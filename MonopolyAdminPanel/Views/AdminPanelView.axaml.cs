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

    private TextBlock? _totalPlayersText;
    private TextBlock? _bankBalanceText;
    private TextBlock? _totalPurchasesText;
    private TextBlock? _totalFinesText;
    private TextBlock? _totalBonusesText;
    private TextBlock? _totalEventsText;
    private TextBlock? _totalTurnsText;

    private bool _isGameStarted;
    private bool _isGameEnded;

    private int _totalFines;
    private int _totalBonuses;
    private int _totalEvents;

    private IReadOnlyList<Player> _lastPlayers = [];

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
            _lastPlayers = players;

            UpdatePlayersTable(players);
            UpdateOnlinePlayers(players);
            UpdateGameInfo(players);
        });
    }

    private void OnGameStarted()
    {
        Debug.WriteLine("[AdminPanelView] OnGameStarted");

        Dispatcher.UIThread.Post(() =>
        {
            _isGameStarted = true;
            RegisterGameEvent("[СОБЫТИЕ] Игра началась");

            UpdateGameStatus();
            UpdateOnlinePlayers(_lastPlayers);
        });
    }
    private void OnGameEventReceived(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            RegisterGameEvent(text);
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

        if (_isGameStarted)
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
            AddCell(player.Balance.ToString(), row, 1, Brushes.White);
            AddCell(player.Purchases.ToString(), row, 2, Brushes.White);
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
        AddCell("бюджет", 0, 1, Brushes.Gray);
        AddCell("покупки", 0, 2, Brushes.Gray);
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

        IBrush statusBrush;

        if (_isGameEnded)
        {
            statusBrush = Brushes.Red;
        }
        else
        {
            statusBrush = player.IsConnected
                ? Brushes.LimeGreen
                : Brushes.Red;
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
            ColumnDefinitions = new ColumnDefinitions("18,*,70")
        };

        var dot = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = statusBrush,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var infoPanel = new StackPanel();

        infoPanel.Children.Add(new TextBlock
        {
            Text = player.Name,
            Foreground = Brushes.White,
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
            _bankBalanceText.Text = "0";

        if (_totalPurchasesText != null)
            _totalPurchasesText.Text = "0";

        if (_totalTurnsText != null)
            _totalTurnsText.Text = "0";

        UpdateLocalStatistics();
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
        _totalFines++;
        UpdateLocalStatistics();
        if (string.IsNullOrWhiteSpace(reason))
        {
            RegisterGameEvent($"[ADMIN] Выдан штраф игроку {selectedPlayer.Name} на {amount.Value}");
        }
        else
        {
            RegisterGameEvent($"[ADMIN] Выдан штраф игроку {selectedPlayer.Name} на {amount.Value}. Причина: {reason}");
        }
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
        _totalBonuses++;
        UpdateLocalStatistics();
        if (string.IsNullOrWhiteSpace(reason))
        {
            RegisterGameEvent($"[ADMIN] Выдан бонус игроку {selectedPlayer.Name} на {amount.Value}");
        }
        else
        {
            RegisterGameEvent($"[ADMIN] Выдан бонус игроку {selectedPlayer.Name} на {amount.Value}. Причина: {reason}");
        }
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
}