using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using MonopolyAdminPanel.Models;
using System.Collections.Generic;
using System.Diagnostics;

namespace MonopolyAdminPanel.Views.Controls;

public partial class OnlinePlayersPanel : UserControl
{
    private StackPanel? _onlinePlayersPanel;

    public OnlinePlayersPanel()
    {
        InitializeComponent();

        _onlinePlayersPanel = this.FindControl<StackPanel>("OnlinePlayersStackPanel");
    }

    public void UpdatePlayers(
        IReadOnlyList<Player> players,
        bool isGameStarted,
        bool isGameEnded)
    {
        Debug.WriteLine($"[OnlinePlayersPanel] UpdatePlayers: {players.Count}");

        if (_onlinePlayersPanel == null)
            return;

        _onlinePlayersPanel.Children.Clear();

        foreach (Player player in players)
        {
            _onlinePlayersPanel.Children.Add(
                CreateOnlinePlayerCard(player, isGameStarted, isGameEnded));
        }
    }

    private static Control CreateOnlinePlayerCard(
        Player player,
        bool isGameStarted,
        bool isGameEnded)
    {
        IBrush playerBrush = player.IsConnected && !isGameEnded
            ? GetPlayerBrush(player)
            : Brushes.Gray;

        IBrush statusBrush;

        if (isGameEnded)
        {
            statusBrush = Brushes.Red;
        }
        else
        {
            statusBrush = player.IsConnected
                ? Brushes.LimeGreen
                : Brushes.Gray;
        }

        string statusText = GetPlayerStatusText(player, isGameStarted, isGameEnded);

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

    private static string GetPlayerStatusText(
        Player player,
        bool isGameStarted,
        bool isGameEnded)
    {
        if (isGameEnded)
            return "Отключен";

        if (!player.IsConnected)
            return "Вышел";

        return isGameStarted ? "В игре" : "В лобби";
    }
}