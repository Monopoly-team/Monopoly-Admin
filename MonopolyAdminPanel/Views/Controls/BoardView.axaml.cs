using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Collections.Generic;
using System.Text.Json;

namespace MonopolyAdminPanel.Views.Controls;

public partial class BoardView : UserControl
{
    private readonly Dictionary<int, TextBlock> _priceTexts = new();

    public BoardView()
    {
        InitializeComponent();

        CreatePriceLabels();
    }

    public void UpdateCells(JsonElement cellsElement)
    {
        if (cellsElement.ValueKind != JsonValueKind.Array)
            return;

        foreach (JsonElement cellElement in cellsElement.EnumerateArray())
        {
            if (!cellElement.TryGetProperty("id", out JsonElement idElement))
                continue;

            if (!cellElement.TryGetProperty("price", out JsonElement priceElement))
                continue;

            int id = idElement.GetInt32();
            int price = priceElement.GetInt32();

            SetCellPrice(id, price);
        }
    }

    private void CreatePriceLabels()
    {
        for (int id = 1; id <= 39; id++)
        {
            if (IsSpecialCell(id))
                continue;

            CreatePriceLabel(id);
        }
    }

    private static bool IsSpecialCell(int id)
    {
        return id == 4 || id == 6 ||
               id == 10 ||
               id == 14 || id == 16 ||
               id == 20 ||
               id == 24 || id == 26 ||
               id == 30 ||
               id == 34 || id == 36;
    }

    private void CreatePriceLabel(int id)
    {
        if (!TryGetCellPosition(id, out int row, out int column, out CellSide side))
            return;

        var textBlock = new TextBlock
        {
            Text = "",
            Foreground = Brushes.White,
            FontSize = 9,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            MinWidth = 45,
            IsHitTestVisible = false
        };

        if (side == CellSide.Left)
        {
            textBlock.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            textBlock.RenderTransform = new RotateTransform(-90);
        }

        if (side == CellSide.Right)
        {
            textBlock.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            textBlock.RenderTransform = new RotateTransform(90);
        }

        var priceBox = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2F3136")),
            Child = textBlock,
            IsHitTestVisible = false
        };

        switch (side)
        {
            case CellSide.Bottom:
                priceBox.Height = 20;
                priceBox.VerticalAlignment = VerticalAlignment.Bottom;
                break;

            case CellSide.Top:
                priceBox.Height = 20;
                priceBox.VerticalAlignment = VerticalAlignment.Top;
                break;

            case CellSide.Left:
                priceBox.Width = 24;
                priceBox.HorizontalAlignment = HorizontalAlignment.Left;
                break;

            case CellSide.Right:
                priceBox.Width = 24;
                priceBox.HorizontalAlignment = HorizontalAlignment.Right;
                break;
        }

        Grid.SetRow(priceBox, row);
        Grid.SetColumn(priceBox, column);

        BoardGrid.Children.Add(priceBox);

        _priceTexts[id] = textBlock;
    }

    private void SetCellPrice(int id, int price)
    {
        if (!_priceTexts.TryGetValue(id, out TextBlock? textBlock))
            return;

        textBlock.Text = price > 0 ? $"${price}" : "";
    }

    private static bool TryGetCellPosition(int id, out int row, out int column, out CellSide side)
    {
        row = 0;
        column = 0;
        side = CellSide.Bottom;

        if (id >= 1 && id <= 9)
        {
            row = 10;
            column = 10 - id;
            side = CellSide.Bottom;
            return true;
        }

        if (id >= 11 && id <= 19)
        {
            row = 20 - id;
            column = 0;
            side = CellSide.Left;
            return true;
        }

        if (id >= 21 && id <= 29)
        {
            row = 0;
            column = id - 20;
            side = CellSide.Top;
            return true;
        }

        if (id >= 31 && id <= 39)
        {
            row = id - 30;
            column = 10;
            side = CellSide.Right;
            return true;
        }

        return false;
    }

    private enum CellSide
    {
        Bottom,
        Top,
        Left,
        Right
    }
}