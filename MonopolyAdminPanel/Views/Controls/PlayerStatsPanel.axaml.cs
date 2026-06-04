using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MonopolyAdminPanel.Models;
using System.Collections.Generic;

namespace MonopolyAdminPanel.Views.Controls;

public partial class PlayerStatsPanel : UserControl
{
    private Grid? _playersTableGrid;

    public PlayerStatsPanel()
    {
        InitializeComponent();

        _playersTableGrid = this.FindControl<Grid>("PlayersTableGrid");

        AddTableHeader();
    }

    public void UpdatePlayers(IReadOnlyList<Player> players)
    {
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
}