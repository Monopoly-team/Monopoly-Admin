using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
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

namespace MonopolyAdminPanel.Views;

public partial class AdminPanelView : UserControl
{
    private const string ConnectedText = "● Подключено";
    private const string DisconnectedText = "● Не подключено";

    private NetworkService? _networkService;

    private Grid? _playersTableGrid;
    private StackPanel? _onlinePlayersPanel;
    private StackPanel? _eventsPanel;
    private ScrollViewer? _eventsScrollViewer;
    private Action? _returnToLogin;
    private BoardView? _gameBoardView;

    private TextBlock? _totalPlayersText;
    private TextBlock? _bankBalanceText;
    private TextBlock? _totalPurchasesText;
    private TextBlock? _totalFinesText;
    private TextBlock? _totalBonusesText;
    private TextBlock? _totalEventsText;
    private TextBlock? _totalTurnsText;
    private TextBlock? _currentPlayerText;

    private Border? _pauseOverlay;
    private Button? _pauseGameButton;
    
    private bool _isGamePaused;
    private bool _isGameStarted;
    private bool _isGameEnded;

    private int _totalFines;
    private int _totalBonuses;
    private int _totalEvents;

    private Control? _firstDiceSvg;
    private Control? _secondDiceSvg;
    private TextBlock? _lastDiceRollText;

    private IReadOnlyList<Player> _lastPlayers = [];
    private readonly Dictionary<int, int> _purchaseCountsByPlayerId = new();
    private readonly Dictionary<int, int> _lastOwnedCellsByPlayerId = new();

    public AdminPanelView()
    {
        InitializeComponent();

        FindControls();

        AddTableHeader();
    }

    public AdminPanelView(NetworkService networkService, string serverIp, Action returnToLogin): this()
    {
        _networkService = networkService;
        _returnToLogin = returnToLogin;

        ServerIpText.Text = serverIp;

        _networkService.ConnectionChanged += OnConnectionChanged;
        _networkService.PlayersListReceived += OnPlayersListReceived;
        _networkService.GameStarted += OnGameStarted;
        _networkService.GameEventReceived += OnGameEventReceived;
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
        _playersTableGrid = this.FindControl<Grid>("PlayersTableGrid");
        _onlinePlayersPanel = this.FindControl<StackPanel>("OnlinePlayersPanel");
        _eventsPanel = this.FindControl<StackPanel>("EventsPanel");
        _eventsScrollViewer = this.FindControl<ScrollViewer>("EventsScrollViewer");

        _totalPlayersText = this.FindControl<TextBlock>("TotalPlayersText");
        _bankBalanceText = this.FindControl<TextBlock>("BankBalanceText");
        _totalPurchasesText = this.FindControl<TextBlock>("TotalPurchasesText");
        _totalFinesText = this.FindControl<TextBlock>("TotalFinesText");
        _totalBonusesText = this.FindControl<TextBlock>("TotalBonusesText");
        _totalEventsText = this.FindControl<TextBlock>("TotalEventsText");
        _totalTurnsText = this.FindControl<TextBlock>("TotalTurnsText");
        _pauseOverlay = this.FindControl<Border>("PauseOverlay");
        _pauseGameButton = this.FindControl<Button>("PauseGameButton");
        _firstDiceSvg = this.FindControl<Control>("FirstDiceSvg");
        _secondDiceSvg = this.FindControl<Control>("SecondDiceSvg");
        _lastDiceRollText = this.FindControl<TextBlock>("LastDiceRollText");
        _gameBoardView = this.FindControl<BoardView>("GameBoardView");
        _currentPlayerText = this.FindControl<TextBlock>("CurrentPlayerText");
    }

    private void DisconnectButton_Click(object? sender, RoutedEventArgs e)
    {
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

            UpdatePlayersTable(players);
            UpdateOnlinePlayers(players);
            UpdateGameInfo(players);

            if (message.Payload.TryGetProperty("currentPlayerId", out JsonElement currentPlayerElement))
            {
                int currentPlayerId = currentPlayerElement.GetInt32();

                Player? currentPlayer = FindPlayerById(players, currentPlayerId);

                if (_currentPlayerText != null)
                    _currentPlayerText.Text = currentPlayer?.Name ?? currentPlayerId.ToString();
            }

            _gameBoardView?.UpdateCells(cellsElement, playersElement);
        });
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

        if (_pauseGameButton != null)
            _pauseGameButton.Content = _isGamePaused ? "Запустить игру" : "Остановить игру";

        UpdateGameStatus();
    }
    private void OnGameEventReceived(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            RegisterGameEvent($"[СОБЫТИЕ] {text}");
        });
    }

    private void AddEventLog(string text)
    {
        if (_eventsPanel == null)
            return;

        var textBlock = new TextBlock
        {
            Text = text,
            Foreground = Brushes.LightGray,
            FontSize = 14
        };

        bool shouldAutoScroll = true;

        if (_eventsScrollViewer != null)
        {
            double offset = _eventsScrollViewer.Offset.Y;
            double viewportHeight = _eventsScrollViewer.Viewport.Height;
            double extentHeight = _eventsScrollViewer.Extent.Height;

            shouldAutoScroll = offset + viewportHeight >= extentHeight - 40;
        }

        _eventsPanel.Children.Add(textBlock);

        if (shouldAutoScroll)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _eventsScrollViewer?.ScrollToEnd();
            });
        }
    }

    private void UpdateConnectionStatus(bool isConnected)
    {
        if (ConnectionStatusText == null)
            return;

        ConnectionStatusText.Text = isConnected ? ConnectedText : DisconnectedText;
        ConnectionStatusText.Foreground = isConnected ? Brushes.LimeGreen : Brushes.Red;
    }

    private void UpdateGameStatus()
    {
        if (GameStatusText == null)
            return;

        if (_isGamePaused)
        {
            GameStatusText.Text = "ИГРА НА ПАУЗЕ";
            GameStatusText.Foreground = Brushes.Orange;
        }
        else if (_isGameStarted)
        {
            GameStatusText.Text = "ИГРА АКТИВНА";
            GameStatusText.Foreground = Brushes.DeepSkyBlue;
        }
        else
        {
            GameStatusText.Text = "ЛОББИ";
            GameStatusText.Foreground = Brushes.LimeGreen;
        }
    }

    private void UpdatePlayersTable(IReadOnlyList<Player> players)
    {
        Debug.WriteLine($"[AdminPanelView] UpdatePlayersTable: {players.Count}");

        if (_playersTableGrid == null)
            return;

        _playersTableGrid.Children.Clear();
        _playersTableGrid.RowDefinitions.Clear();

        AddTableHeader();

        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            int row = i + 1;

            _playersTableGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            AddCell(player.Name, row, 0, Brushes.White);
            AddCell(player.Purchases.ToString(), row, 1, Brushes.White);
            AddCell(player.Bonuses.ToString(), row, 2, Brushes.White);
            AddCell(player.Fines.ToString(), row, 3, Brushes.White);
            AddCell(player.OwnedCellsCount.ToString(), row, 4, Brushes.White);
        }
    }

    private void AddTableHeader()
    {
        if (_playersTableGrid == null)
            return;

        _playersTableGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        AddCell("игрок", 0, 0, Brushes.Gray);
        AddCell("покупки", 0, 1, Brushes.Gray);
        AddCell("бонусы", 0, 2, Brushes.Gray);
        AddCell("штрафы", 0, 3, Brushes.Gray);
        AddCell("клетки", 0, 4, Brushes.Gray);
    }

    private void AddCell(string text, int row, int column, IBrush foreground)
    {
        if (_playersTableGrid == null)
            return;

        var textBlock = new TextBlock
        {
            Text = text,
            Foreground = foreground,
            FontWeight = row == 0 ? FontWeight.SemiBold : FontWeight.Normal,
            Margin = new Thickness(0, 7, 0, 7)
        };

        Grid.SetRow(textBlock, row);
        Grid.SetColumn(textBlock, column);

        _playersTableGrid.Children.Add(textBlock);
    }

    private void UpdateOnlinePlayers(IReadOnlyList<Player> players)
    {
        Debug.WriteLine($"[AdminPanelView] UpdateOnlinePlayers: {players.Count}");

        if (_onlinePlayersPanel == null)
            return;

        _onlinePlayersPanel.Children.Clear();

        foreach (Player player in players)
        {
            _onlinePlayersPanel.Children.Add(CreateOnlinePlayerCard(player));
        }
    }

    private Control CreateOnlinePlayerCard(Player player)
    {
        Debug.WriteLine($"[AdminPanelView] CreateOnlinePlayerCard: {player.Name}");

        IBrush playerBrush = player.IsConnected && !_isGameEnded
            ? GetPlayerBrush(player)
            : Brushes.Gray;

        IBrush statusBrush;

        if (_isGameEnded)
        {
            statusBrush = Brushes.Red;
        }
        else
        {
            statusBrush = player.IsConnected
                ? Brushes.LimeGreen
                : Brushes.Gray;
        }

        string statusText = GetPlayerStatusText(player);

        var card = new Border
        {
            Height = 58,
            Background = new SolidColorBrush(Color.Parse("#26262D")),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("18,*,80")
        };

        var dot = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = playerBrush,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var infoPanel = new StackPanel();

        infoPanel.Children.Add(new TextBlock
        {
            Text = player.Name,
            Foreground = playerBrush,
            FontWeight = FontWeight.SemiBold
        });

        infoPanel.Children.Add(new TextBlock
        {
            Text = $"{player.Balance}$",
            Foreground = Brushes.LightGray,
            FontSize = 12
        });

        var statusBlock = new TextBlock
        {
            Text = statusText,
            Foreground = statusBrush,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            FontWeight = FontWeight.SemiBold
        };

        Grid.SetColumn(dot, 0);
        Grid.SetColumn(infoPanel, 1);
        Grid.SetColumn(statusBlock, 2);

        grid.Children.Add(dot);
        grid.Children.Add(infoPanel);
        grid.Children.Add(statusBlock);

        card.Child = grid;

        return card;
    }

    private static IBrush GetPlayerBrush(Player player)
    {
        if (string.IsNullOrWhiteSpace(player.Color))
            return Brushes.White;

        try
        {
            return new SolidColorBrush(Color.Parse(player.Color));
        }
        catch
        {
            return Brushes.White;
        }
    }

    private string GetPlayerStatusText(Player player)
    {
        if (_isGameEnded)
            return "Отключен";

        if (!player.IsConnected)
            return "Вышел";

        return _isGameStarted ? "В игре" : "В лобби";
    }

    private void UpdateGameInfo(IReadOnlyList<Player> players)
    {
        if (_totalPlayersText != null)
            _totalPlayersText.Text = players.Count.ToString();

        if (_bankBalanceText != null)
            _bankBalanceText.Text = "∞";

        int totalPurchases = 0;

        foreach (Player player in players)
        {
            totalPurchases += player.Purchases;
        }

        if (_totalPurchasesText != null)
            _totalPurchasesText.Text = totalPurchases.ToString();

        if (_totalTurnsText != null)
            _totalTurnsText.Text = "0";

        if (_bankBalanceText != null)
            _bankBalanceText.Text = "∞";

        UpdateLocalStatistics();
    }
    private void OnDiceRolled(int first, int second)
    {
        Dispatcher.UIThread.Post(() =>
        {
            SetSvgPath(_firstDiceSvg, $"/Assets/dice{first}.svg");
            SetSvgPath(_secondDiceSvg, $"/Assets/dice{second}.svg");

            if (_lastDiceRollText != null)
                _lastDiceRollText.Text = $"Последний бросок: {first + second} ({first} + {second})";
        });
    }

    private void SetSvgPath(Control? svg, string path)
    {
        if (svg == null)
            return;

        var property = svg.GetType().GetProperty("Path");

        property?.SetValue(svg, path);
    }

    private void RegisterGameEvent(string text)
    {
        AddEventLog(text);
    }

    private void UpdateLocalStatistics()
    {
        if (_totalFinesText != null)
            _totalFinesText.Text = _totalFines.ToString();

        if (_totalBonusesText != null)
            _totalBonusesText.Text = _totalBonuses.ToString();

        if (_totalEventsText != null)
            _totalEventsText.Text = _totalEvents.ToString();
    }

    private async void ChangeBalanceButton_Click(object? sender, RoutedEventArgs e)
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

        string reason = dialog.Reason;

        string json;

        if (string.IsNullOrWhiteSpace(reason))
        {
            json =
                $"{{\"type\":\"admin_action\",\"senderId\":65535,\"payload\":{{\"action\":\"set_balance\",\"playerId\":{selectedPlayer.Id},\"balance\":{balance.Value}}}}}";
        }
        else
        {
            json =
                $"{{\"type\":\"admin_action\",\"senderId\":65535,\"payload\":{{\"action\":\"set_balance\",\"playerId\":{selectedPlayer.Id},\"balance\":{balance.Value},\"reason\":\"{reason}\"}}}}";
        }

        await _networkService.SendAsync(json);
    }

    private async void FinePlayerButton_Click(object? sender, RoutedEventArgs e)
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

        string reason = dialog.Reason;

        string json;

        if (string.IsNullOrWhiteSpace(reason))
        {
            json =
                $"{{\"type\":\"admin_action\",\"senderId\":65535,\"payload\":{{\"action\":\"fine\",\"playerId\":{selectedPlayer.Id},\"amount\":{amount.Value}}}}}";
        }
        else
        {
            json =
                $"{{\"type\":\"admin_action\",\"senderId\":65535,\"payload\":{{\"action\":\"fine\",\"playerId\":{selectedPlayer.Id},\"amount\":{amount.Value},\"reason\":\"{reason}\"}}}}";
        }

        await _networkService.SendAsync(json);

        selectedPlayer.Fines++;
        _totalFines++;

        UpdatePlayersTable(_lastPlayers);
        UpdateLocalStatistics();
    }

    private async void BonusPlayerButton_Click(object? sender, RoutedEventArgs e)
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

        string reason = dialog.Reason;

        string json;

        if (string.IsNullOrWhiteSpace(reason))
        {
            json =
                $"{{\"type\":\"admin_action\",\"senderId\":65535,\"payload\":{{\"action\":\"bonus\",\"playerId\":{selectedPlayer.Id},\"amount\":{amount.Value}}}}}";
        }
        else
        {
            json =
                $"{{\"type\":\"admin_action\",\"senderId\":65535,\"payload\":{{\"action\":\"bonus\",\"playerId\":{selectedPlayer.Id},\"amount\":{amount.Value},\"reason\":\"{reason}\"}}}}";
        }

        await _networkService.SendAsync(json);

        selectedPlayer.Bonuses++;
        _totalBonuses++;

        UpdatePlayersTable(_lastPlayers);
        UpdateLocalStatistics();
    }

    private async void KickPlayerButton_Click(object? sender, RoutedEventArgs e)
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

        string json;

        if (string.IsNullOrWhiteSpace(reason))
        {
            json =
                $"{{\"type\":\"admin_action\",\"senderId\":65535,\"payload\":{{\"action\":\"kick\",\"playerId\":{selectedPlayer.Id}}}}}";
        }
        else
        {
            json =
                $"{{\"type\":\"admin_action\",\"senderId\":65535,\"payload\":{{\"action\":\"kick\",\"playerId\":{selectedPlayer.Id},\"reason\":\"{reason}\"}}}}";
        }

        await _networkService.SendAsync(json);

        if (selectedPlayer.Id == 1)
        {
            _isGameStarted = false;
            _isGameEnded = true;

            GameStatusText.Text = "ИГРА ЗАКОНЧЕНА";
            GameStatusText.Foreground = Brushes.Red;

            UpdateOnlinePlayers(_lastPlayers);
        }
    }
    
    private async void StartEventButton_Click(object? sender, RoutedEventArgs e)
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

        string json =
            $"{{\"type\":\"admin_action\",\"senderId\":65535,\"payload\":{{\"action\":\"start_event\",\"eventId\":{dialog.EventId},\"title\":\"{dialog.EventTitle}\",\"description\":\"{dialog.EventDescription}\",\"turns\":{turns.Value}}}}}";

        await _networkService.SendAsync(json);

        _totalEvents++;
        UpdateLocalStatistics();
    }
    private async void PauseGameButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_networkService == null)
            return;

        string json;

        if (_isGamePaused)
        {
            json =
                "{\"type\":\"admin_action\",\"senderId\":65535,\"payload\":{\"action\":\"resume_game\"}}";
        }
        else
        {
            json =
                "{\"type\":\"admin_action\",\"senderId\":65535,\"payload\":{\"action\":\"pause_game\"}}";
        }

        await _networkService.SendAsync(json);
    }
}