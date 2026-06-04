using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using MonopolyAdminPanel.Models;
using MonopolyAdminPanel.Services;
using MonopolyAdminPanel.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using MonopolyAdminPanel.Views.Controls;
using System.Threading.Tasks;

namespace MonopolyAdminPanel.Views;


public partial class AdminPanelView : UserControl
{
    private NetworkService? _networkService;

    private EventHistoryPanel? _eventHistoryPanel;
    private ChatPanel? _adminChatPanel;
    private DicePanel? _dicePanel;
    private ConnectionPanel? _connectionPanel;
    private GameInfoPanel? _gameInfoPanel;
    private GameStatePanel? _gameStatePanel;
    private PlayerStatsPanel? _playerStatsPanel;
    private OnlinePlayersPanel? _onlinePlayersPanel;
    private AdminActionsPanel? _adminActionsPanel;

    private Action? _returnToLogin;
    private BoardView? _gameBoardView;

    private Border? _pauseOverlay;
    
    private bool _isGamePaused;
    private bool _isGameStarted;
    private bool _isGameEnded;

    private int _totalFines;
    private int _totalBonuses;
    private int _totalEvents;

    private IReadOnlyList<Player> _lastPlayers = [];
    private readonly Dictionary<int, int> _purchaseCountsByPlayerId = new();
    private readonly Dictionary<int, int> _lastOwnedCellsByPlayerId = new();
    private DispatcherTimer? _gameSessionTimer;
    private DateTime? _gameSessionStartTime;
    private int? _lastCurrentPlayerId;
    private int _totalTurns;

    public AdminPanelView()
    {
        InitializeComponent();

        FindControls();
    }

    public AdminPanelView(NetworkService networkService, string serverIp, Action returnToLogin): this()
    {
        _networkService = networkService;
        _returnToLogin = returnToLogin;

        _connectionPanel?.SetServerIp(serverIp);

        _networkService.ConnectionChanged += OnConnectionChanged;
        _networkService.PlayersListReceived += OnPlayersListReceived;
        _networkService.GameStarted += OnGameStarted;
        _networkService.GameEventReceived += OnGameEventReceived;
        _networkService.ChatMessageReceived += OnChatMessageReceived;
        _networkService.GamePaused += OnGamePaused;
        _networkService.GameResumed += OnGameResumed;
        _networkService.DiceRolled += OnDiceRolled;
        _networkService.GameStateReceived += OnGameStateReceived;

        UpdateConnectionStatus(_networkService.IsConnected);
        UpdateGameStatus();

        if (_networkService.LastPlayers.Count > 0)
        {
            OnPlayersListReceived(_networkService.LastPlayers);
        }

        if (_networkService.IsGameStarted)
        {
            OnGameStarted();
        }
        if (_networkService.IsGamePaused)
        {
            OnGamePaused("Игра на паузе");
        }
    }

    private void FindControls()
    {
        _eventHistoryPanel = this.FindControl<EventHistoryPanel>("EventHistoryPanel");
        _dicePanel = this.FindControl<DicePanel>("DicePanel");
        _connectionPanel = this.FindControl<ConnectionPanel>("ConnectionPanel");
        _gameInfoPanel = this.FindControl<GameInfoPanel>("GameInfoPanel");
        _gameStatePanel = this.FindControl<GameStatePanel>("GameStatePanel");
        _playerStatsPanel = this.FindControl<PlayerStatsPanel>("PlayerStatsPanel");
        _onlinePlayersPanel = this.FindControl<OnlinePlayersPanel>("OnlinePlayersPanel");

        _pauseOverlay = this.FindControl<Border>("PauseOverlay");
        _gameBoardView = this.FindControl<BoardView>("GameBoardView");
        _adminActionsPanel = this.FindControl<AdminActionsPanel>("AdminActionsPanel");

        if (_adminActionsPanel != null)
        {
            _adminActionsPanel.ChangeBalanceRequested += OnChangeBalanceRequested;
            _adminActionsPanel.FinePlayerRequested += OnFinePlayerRequested;
            _adminActionsPanel.BonusPlayerRequested += OnBonusPlayerRequested;
            _adminActionsPanel.KickPlayerRequested += OnKickPlayerRequested;
            _adminActionsPanel.StartEventRequested += OnStartEventRequested;
            _adminActionsPanel.PauseGameRequested += OnPauseGameRequested;
        }

        if (_connectionPanel != null)
            _connectionPanel.DisconnectRequested += OnDisconnectRequested;

        _adminChatPanel = this.FindControl<ChatPanel>("AdminChatPanel");

        if (_adminChatPanel != null)
            _adminChatPanel.SendRequested += OnChatSendRequested;
    }

    private void OnDisconnectRequested()
    {
        StopGameSessionTimer();
        _returnToLogin?.Invoke();
    }

    private void OnConnectionChanged(bool isConnected)
    {
        Dispatcher.UIThread.Post(() => UpdateConnectionStatus(isConnected));
    }

    private void OnPlayersListReceived(IReadOnlyList<Player> players)
    {
        Debug.WriteLine($"[AdminPanelView] OnPlayersListReceived: {players.Count} players");

        Dispatcher.UIThread.Post(() =>
        {
            IReadOnlyList<Player> updatedPlayers = KeepKnownPlayersDuringGame(players);

            _lastPlayers = updatedPlayers;

            UpdatePlayersTable(updatedPlayers);
            UpdateOnlinePlayers(updatedPlayers);
            UpdateGameInfo(updatedPlayers);
        });
    }

    private void OnGameStarted()
    {
        Debug.WriteLine("[AdminPanelView] OnGameStarted");

        Dispatcher.UIThread.Post(() =>
        {
            _isGameStarted = true;

            UpdateGameStatus();
            UpdateOnlinePlayers(_lastPlayers);
        });
    }
    private void OnGamePaused(string reason)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _isGamePaused = true;
            UpdatePauseState();
        });
    }

    private void OnGameResumed()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _isGamePaused = false;
            UpdatePauseState();
        });
    }

    private void OnGameStateReceived(NetworkMessage message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (message.Payload.ValueKind != JsonValueKind.Object)
                return;

            if (!message.Payload.TryGetProperty("cells", out JsonElement cellsElement))
                return;

            if (!message.Payload.TryGetProperty("players", out JsonElement playersElement))
                return;

            List<Player> players = ReadPlayersFromGameState(playersElement);

            UpdatePurchaseCounters(players);

            _lastPlayers = players;

            StartGameSessionTimerIfNeeded();
            UpdateTurnCounterAndCurrentPlayer(message.Payload, players);

            UpdatePlayersTable(players);
            UpdateOnlinePlayers(players);
            UpdateGameInfo(players);

            _gameBoardView?.UpdateCells(cellsElement, playersElement);
        });
    }

    private void StartGameSessionTimerIfNeeded()
    {
        if (_gameSessionStartTime != null)
            return;

        _gameSessionStartTime = DateTime.Now;

        UpdateGameSessionTime();

        _gameSessionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _gameSessionTimer.Tick += (_, _) => UpdateGameSessionTime();
        _gameSessionTimer.Start();
    }

    private void UpdateGameSessionTime()
    {
        if (_gameSessionStartTime == null)
            return;

        TimeSpan elapsed = DateTime.Now - _gameSessionStartTime.Value;

        string timeText =
            $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";

        _gameStatePanel?.SetTime(timeText);
    }

    private void UpdateTurnCounterAndCurrentPlayer(JsonElement payload, IReadOnlyList<Player> players)
    {
        if (!payload.TryGetProperty("currentPlayerId", out JsonElement currentPlayerElement))
            return;

        if (!TryGetIntValue(currentPlayerElement, out int currentPlayerId))
            return;

        if (_lastCurrentPlayerId == null)
        {
            _lastCurrentPlayerId = currentPlayerId;
        }
        else if (_lastCurrentPlayerId.Value != currentPlayerId)
        {
            _totalTurns++;
            _lastCurrentPlayerId = currentPlayerId;
        }

        Player? currentPlayer = FindPlayerById(players, currentPlayerId);

        _gameStatePanel?.SetCurrentPlayer(currentPlayer?.Name ?? currentPlayerId.ToString());
    }

    private static bool TryGetIntValue(JsonElement value, out int result)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out result))
            return true;

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out result))
            return true;

        result = 0;
        return false;
    }

    private void StopGameSessionTimer()
    {
        _gameSessionTimer?.Stop();
        _gameSessionTimer = null;
    }

    private IReadOnlyList<Player> KeepKnownPlayersDuringGame(IReadOnlyList<Player> players)
    {
        if (!_isGameStarted)
            return players;

        var result = new List<Player>(players);

        foreach (Player oldPlayer in _lastPlayers)
        {
            bool alreadyExists = false;

            foreach (Player player in result)
            {
                if (player.Id == oldPlayer.Id)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (alreadyExists)
                continue;

            oldPlayer.IsConnected = false;
            result.Add(oldPlayer);
        }

        return result;
    }

    private List<Player> ReadPlayersFromGameState(JsonElement playersElement)
    {
        var players = new List<Player>();

        if (playersElement.ValueKind != JsonValueKind.Array)
            return players;

        foreach (JsonElement playerElement in playersElement.EnumerateArray())
        {
            int id = GetIntProperty(playerElement, "id", 0);

            Player? previousPlayer = FindPlayerById(_lastPlayers, id);

            int ownedCellsCount = GetArrayCountProperty(playerElement, "ownedProperties");

            if (ownedCellsCount == 0)
                ownedCellsCount = GetArrayCountProperty(playerElement, "ownedCells");

            var player = new Player
            {
                Id = id,
                Name = GetStringProperty(
                    playerElement,
                    "nickname",
                    GetStringProperty(playerElement, "name", previousPlayer?.Name ?? $"Игрок {id}")),

                Balance = GetIntProperty(playerElement, "balance", previousPlayer?.Balance ?? 0),

                Color = GetStringProperty(playerElement, "color", previousPlayer?.Color ?? "#FFFFFF"),

                IsConnected = GetBoolProperty(
                    playerElement,
                    "active",
                    GetBoolProperty(playerElement, "isConnected", previousPlayer?.IsConnected ?? true)),

                OwnedCellsCount = ownedCellsCount,

                Purchases = previousPlayer?.Purchases ?? 0,
                Fines = previousPlayer?.Fines ?? 0,
                Bonuses = previousPlayer?.Bonuses ?? 0
            };

            players.Add(player);
        }

        return players;
    }

    private void UpdatePurchaseCounters(IReadOnlyList<Player> players)
    {
        foreach (Player player in players)
        {
            int currentOwnedCells = player.OwnedCellsCount;

            if (!_purchaseCountsByPlayerId.ContainsKey(player.Id))
            {
                _purchaseCountsByPlayerId[player.Id] = currentOwnedCells;
            }

            if (_lastOwnedCellsByPlayerId.TryGetValue(player.Id, out int previousOwnedCells))
            {
                if (currentOwnedCells > previousOwnedCells)
                {
                    _purchaseCountsByPlayerId[player.Id] += currentOwnedCells - previousOwnedCells;
                }
            }

            _lastOwnedCellsByPlayerId[player.Id] = currentOwnedCells;
            player.Purchases = _purchaseCountsByPlayerId[player.Id];
        }
    }

    private static Player? FindPlayerById(IReadOnlyList<Player> players, int id)
    {
        foreach (Player player in players)
        {
            if (player.Id == id)
                return player;
        }

        return null;
    }

    private static string GetStringProperty(JsonElement element, string propertyName, string defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
            return defaultValue;

        if (value.ValueKind != JsonValueKind.String)
            return defaultValue;

        return value.GetString() ?? defaultValue;
    }

    private static int GetIntProperty(JsonElement element, string propertyName, int defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
            return defaultValue;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int result))
            return result;

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out result))
            return result;

        return defaultValue;
    }

    private static bool GetBoolProperty(JsonElement element, string propertyName, bool defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
            return defaultValue;

        if (value.ValueKind == JsonValueKind.True)
            return true;

        if (value.ValueKind == JsonValueKind.False)
            return false;

        if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out bool result))
            return result;

        return defaultValue;
    }

    private static int GetArrayCountProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
            return 0;

        if (value.ValueKind != JsonValueKind.Array)
            return 0;

        return value.GetArrayLength();
    }

    private void UpdatePauseState()
    {
        if (_pauseOverlay != null)
            _pauseOverlay.IsVisible = _isGamePaused;

        _adminActionsPanel?.SetPauseButtonText(_isGamePaused);

        UpdateGameStatus();
    }

    private void OnGameEventReceived(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            RegisterGameEvent($"[СОБЫТИЕ] {text}");
        });
    }

    private void OnChatMessageReceived(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            AddChatMessage(text);
        });
    }

    private void AddChatMessage(string text)
    {
        _adminChatPanel?.AddMessage(text);
    }

    private async void OnChatSendRequested()
    {
        await SendChatMessageAsync();
    }

    private async Task SendChatMessageAsync()
    {
        if (_networkService == null)
            return;

        if (_adminChatPanel == null)
            return;

        string text = _adminChatPanel.MessageText;

        if (string.IsNullOrWhiteSpace(text))
            return;

        string json = CreateChatMessageJson(text);

        await _networkService.SendAsync(json);

        _adminChatPanel.ClearInput();
    }

    private static string CreateChatMessageJson(string text)
    {
        return JsonSerializer.Serialize(new
        {
            type = "chat_message",
            senderId = 65535,
            payload = new
            {
                text
            }
        });
    }

    private void AddEventLog(string text)
    {
        _eventHistoryPanel?.AddEvent(text);
    }

    private void UpdateConnectionStatus(bool isConnected)
    {
        _connectionPanel?.SetConnectionStatus(isConnected);
    }

    private void UpdateGameStatus()
    {
        if (_isGameEnded)
        {
            _gameStatePanel?.SetStatus("ИГРА ЗАКОНЧЕНА", Brushes.Red);
            return;
        }

        if (_isGamePaused)
        {
            _gameStatePanel?.SetStatus("ИГРА НА ПАУЗЕ", Brushes.Orange);
            return;
        }

        if (_isGameStarted)
        {
            _gameStatePanel?.SetStatus("ИГРА АКТИВНА", Brushes.DeepSkyBlue);
            return;
        }

        _gameStatePanel?.SetStatus("ЛОББИ", Brushes.LimeGreen);
    }

    private void UpdatePlayersTable(IReadOnlyList<Player> players)
    {
        Debug.WriteLine($"[AdminPanelView] UpdatePlayersTable: {players.Count}");

        _playerStatsPanel?.UpdatePlayers(players);
    }

    private void UpdateOnlinePlayers(IReadOnlyList<Player> players)
    {
        Debug.WriteLine($"[AdminPanelView] UpdateOnlinePlayers: {players.Count}");

        _onlinePlayersPanel?.UpdatePlayers(
            players,
            isGameStarted: _isGameStarted,
            isGameEnded: _isGameEnded);
    }

    private void UpdateGameInfo(IReadOnlyList<Player> players)
    {
        int totalPurchases = 0;

        foreach (Player player in players)
        {
            totalPurchases += player.Purchases;
        }

        _gameInfoPanel?.UpdateInfo(
            totalPlayers: players.Count,
            totalPurchases: totalPurchases,
            totalFines: _totalFines,
            totalBonuses: _totalBonuses,
            totalEvents: _totalEvents,
            totalTurns: _totalTurns);
    }
    private void OnDiceRolled(int first, int second)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _dicePanel?.SetDice(first, second);
        });
    }

    private void RegisterGameEvent(string text)
    {
        AddEventLog(text);
    }

    private void UpdateLocalStatistics()
    {
        UpdateGameInfo(_lastPlayers);
    }

    private static string CreateAdminActionJson(Dictionary<string, object> payload)
    {
        return JsonSerializer.Serialize(new
        {
            type = "admin_action",
            senderId = 65535,
            payload
        });
    }

    private async void OnChangeBalanceRequested()
    {
        if (_networkService == null)
            return;

        if (_lastPlayers.Count == 0)
            return;

        var dialog = new ChangeBalanceDialog(_lastPlayers);

        var parentWindow = TopLevel.GetTopLevel(this) as Window;

        if (parentWindow == null)
            return;

        bool confirmed = await dialog.ShowDialog<bool>(parentWindow);

        if (!confirmed)
            return;

        Player? selectedPlayer = dialog.SelectedPlayer;

        if (selectedPlayer == null)
            return;

        int? balance = dialog.Balance;

        if (balance == null)
            return;

        string reason = dialog.Reason.Trim();

        var payload = new Dictionary<string, object>
        {
            ["action"] = "set_balance",
            ["playerId"] = selectedPlayer.Id,
            ["balance"] = balance.Value
        };

        if (!string.IsNullOrWhiteSpace(reason))
            payload["reason"] = reason;

        string json = CreateAdminActionJson(payload);

        await _networkService.SendAsync(json);
    }

    private async void OnFinePlayerRequested()
    {
        if (_networkService == null)
            return;

        if (_lastPlayers.Count == 0)
            return;

        var dialog = new FinePlayerDialog(_lastPlayers);

        var parentWindow = TopLevel.GetTopLevel(this) as Window;

        if (parentWindow == null)
            return;

        bool confirmed = await dialog.ShowDialog<bool>(parentWindow);

        if (!confirmed)
            return;

        Player? selectedPlayer = dialog.SelectedPlayer;

        if (selectedPlayer == null)
            return;

        int? amount = dialog.Amount;

        if (amount == null)
            return;

        string reason = dialog.Reason.Trim();

        var payload = new Dictionary<string, object>
        {
            ["action"] = "fine",
            ["playerId"] = selectedPlayer.Id,
            ["amount"] = amount.Value
        };

        if (!string.IsNullOrWhiteSpace(reason))
            payload["reason"] = reason;

        string json = CreateAdminActionJson(payload);

        await _networkService.SendAsync(json);

        selectedPlayer.Fines++;
        _totalFines++;

        UpdatePlayersTable(_lastPlayers);
        UpdateLocalStatistics();
    }

    private async void OnBonusPlayerRequested()
    {
        if (_networkService == null)
            return;

        if (_lastPlayers.Count == 0)
            return;

        var dialog = new BonusPlayerDialog(_lastPlayers);

        var parentWindow = TopLevel.GetTopLevel(this) as Window;

        if (parentWindow == null)
            return;

        bool confirmed = await dialog.ShowDialog<bool>(parentWindow);

        if (!confirmed)
            return;

        Player? selectedPlayer = dialog.SelectedPlayer;

        if (selectedPlayer == null)
            return;

        int? amount = dialog.Amount;

        if (amount == null)
            return;

        string reason = dialog.Reason.Trim();

        var payload = new Dictionary<string, object>
        {
            ["action"] = "bonus",
            ["playerId"] = selectedPlayer.Id,
            ["amount"] = amount.Value
        };

        if (!string.IsNullOrWhiteSpace(reason))
            payload["reason"] = reason;

        string json = CreateAdminActionJson(payload);

        await _networkService.SendAsync(json);

        selectedPlayer.Bonuses++;
        _totalBonuses++;

        UpdatePlayersTable(_lastPlayers);
        UpdateLocalStatistics();
    }

    private async void OnKickPlayerRequested()
    {
        if (_networkService == null)
            return;

        if (_lastPlayers.Count == 0)
            return;

        var dialog = new KickPlayerDialog(_lastPlayers);

        var parentWindow = TopLevel.GetTopLevel(this) as Window;

        if (parentWindow == null)
            return;

        bool confirmed = await dialog.ShowDialog<bool>(parentWindow);

        if (!confirmed)
            return;

        Player? selectedPlayer = dialog.SelectedPlayer;

        if (selectedPlayer == null)
            return;

        string reason = dialog.Reason.Trim();

        var payload = new Dictionary<string, object>
        {
            ["action"] = "kick",
            ["playerId"] = selectedPlayer.Id
        };

        if (!string.IsNullOrWhiteSpace(reason))
            payload["reason"] = reason;

        string json = CreateAdminActionJson(payload);

        await _networkService.SendAsync(json);

        if (selectedPlayer.Id == 1)
        {
            _isGameStarted = false;
            _isGameEnded = true;

            UpdateGameStatus();

            UpdateOnlinePlayers(_lastPlayers);
        }
    }

    private async void OnStartEventRequested()
    {
        if (_networkService == null)
            return;

        var dialog = new StartEventDialog();

        var parentWindow = TopLevel.GetTopLevel(this) as Window;

        if (parentWindow == null)
            return;

        bool confirmed = await dialog.ShowDialog<bool>(parentWindow);

        if (!confirmed)
            return;

        int? turns = dialog.Turns;

        if (turns == null)
            return;

        var payload = new Dictionary<string, object>
        {
            ["action"] = "start_event",
            ["eventId"] = dialog.EventId,
            ["title"] = dialog.EventTitle,
            ["description"] = dialog.EventDescription,
            ["turns"] = turns.Value
        };

        string json = CreateAdminActionJson(payload);

        await _networkService.SendAsync(json);

        _totalEvents++;
        UpdateLocalStatistics();
    }
    private async void OnPauseGameRequested()
    {
        if (_networkService == null)
            return;

        string action = _isGamePaused ? "resume_game" : "pause_game";

        var payload = new Dictionary<string, object>
        {
            ["action"] = action
        };

        string json = CreateAdminActionJson(payload);

        await _networkService.SendAsync(json);
    }
}