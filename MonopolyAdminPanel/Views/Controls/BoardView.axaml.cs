using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MonopolyAdminPanel.Views.Controls;

public partial class BoardView : UserControl
{
    private readonly Dictionary<int, TextBlock> _priceTexts = new();
    private readonly Dictionary<int, Canvas> _playerTokenCanvases = new();

    public BoardView()
    {
        InitializeComponent();

        CreatePriceLabels();
        CreatePlayerTokenLayers();
    }

    public void UpdateCells(JsonElement cellsElement, JsonElement playersElement)
    {
        if (cellsElement.ValueKind != JsonValueKind.Array)
            return;

        Dictionary<int, IBrush> playerBrushes = CreatePlayerBrushes(playersElement);

        foreach (JsonElement cellElement in cellsElement.EnumerateArray())
        {
            if (!cellElement.TryGetProperty("id", out JsonElement idElement))
                continue;

            int id = idElement.GetInt32();

            if (cellElement.TryGetProperty("price", out JsonElement priceElement))
            {
                int price = priceElement.GetInt32();
                SetCellPrice(id, price);
            }

            int ownerId = 0;

            if (cellElement.TryGetProperty("ownerId", out JsonElement ownerIdElement))
                ownerId = ownerIdElement.GetInt32();

            SetCellOwnerColor(id, ownerId, playerBrushes);
        }
        UpdatePlayerTokens(playersElement);
    }

    private static Dictionary<int, IBrush> CreatePlayerBrushes(JsonElement playersElement)
    {
        Dictionary<int, IBrush> result = new();

        if (playersElement.ValueKind != JsonValueKind.Array)
            return result;

        foreach (JsonElement playerElement in playersElement.EnumerateArray())
        {
            if (!playerElement.TryGetProperty("id", out JsonElement idElement))
                continue;

            if (!playerElement.TryGetProperty("color", out JsonElement colorElement))
                continue;

            int id = idElement.GetInt32();
            string? color = colorElement.GetString();

            if (string.IsNullOrWhiteSpace(color))
                continue;

            result[id] = new SolidColorBrush(Color.Parse(color));
        }

        return result;
    }

    private void SetCellOwnerColor(
    int cellId,
    int ownerId,
    Dictionary<int, IBrush> playerBrushes)
    {
        Grid? cellArea = this.FindControl<Grid>($"Cell{cellId}OwnerArea");

        if (cellArea == null)
            return;

        if (ownerId <= 0)
        {
            cellArea.Background = Brushes.White;
            return;
        }

        if (!playerBrushes.TryGetValue(ownerId, out IBrush? ownerBrush))
            return;

        Color color = ((SolidColorBrush)ownerBrush).Color;

        cellArea.Background = new SolidColorBrush(
            Color.FromArgb(
                120,
                color.R,
                color.G,
                color.B));
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

    private void CreatePlayerTokenLayers()
    {
        for (int id = 0; id <= 39; id++)
        {
            if (!TryGetCellPositionIncludingCorners(id, out int row, out int column))
                continue;

            var canvas = new Canvas
            {
                IsHitTestVisible = false
            };

            Grid.SetRow(canvas, row);
            Grid.SetColumn(canvas, column);

            BoardGrid.Children.Add(canvas);

            _playerTokenCanvases[id] = canvas;
        }
    }

    private void UpdatePlayerTokens(JsonElement playersElement)
    {
        foreach (Canvas canvas in _playerTokenCanvases.Values)
            canvas.Children.Clear();

        if (playersElement.ValueKind != JsonValueKind.Array)
            return;

        Dictionary<int, List<(int PlayerId, string Color)>> playersByPosition = new();

        foreach (JsonElement playerElement in playersElement.EnumerateArray())
        {
            if (!playerElement.TryGetProperty("id", out JsonElement idElement))
                continue;

            if (!playerElement.TryGetProperty("position", out JsonElement positionElement))
                continue;

            if (!playerElement.TryGetProperty("color", out JsonElement colorElement))
                continue;

            int playerId = idElement.GetInt32();
            int position = positionElement.GetInt32();
            string color = colorElement.GetString() ?? "#FFFFFF";

            if (!playersByPosition.ContainsKey(position))
                playersByPosition[position] = new List<(int, string)>();

            playersByPosition[position].Add((playerId, color));
        }

        foreach (var pair in playersByPosition)
        {
            int position = pair.Key;
            List<(int PlayerId, string Color)> players = pair.Value;

            if (!_playerTokenCanvases.TryGetValue(position, out Canvas? canvas))
                continue;

            DrawTokensOnCell(canvas, players);
        }
    }

    private static void DrawTokensOnCell(Canvas canvas, List<(int PlayerId, string Color)> players)
    {
        const double tokenSize = 16;
        const double gap = 4;

        int count = players.Count;

        int columns = count switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            4 => 2,
            _ => 3
        };

        int rows = (int)Math.Ceiling(count / (double)columns);

        double totalWidth = columns * tokenSize + (columns - 1) * gap;
        double totalHeight = rows * tokenSize + (rows - 1) * gap;

        double cellWidth = canvas.Bounds.Width;
        double cellHeight = canvas.Bounds.Height;

        if (cellWidth <= 0)
            cellWidth = 53;

        if (cellHeight <= 0)
            cellHeight = 53;

        double startX = cellWidth / 2 - totalWidth / 2;
        double startY = cellHeight / 2 - totalHeight / 2;

        for (int i = 0; i < count; i++)
        {
            var player = players[i];

            int row = i / columns;
            int column = i % columns;

            var token = new Avalonia.Controls.Shapes.Ellipse
            {
                Width = tokenSize,
                Height = tokenSize,
                Fill = new SolidColorBrush(Color.Parse(player.Color)),
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            Canvas.SetLeft(token, startX + column * (tokenSize + gap));
            Canvas.SetTop(token, startY + row * (tokenSize + gap));

            canvas.Children.Add(token);
        }
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

    private static bool TryGetCellPositionIncludingCorners(int id, out int row, out int column)
    {
        row = 0;
        column = 0;

        if (id == 0)
        {
            row = 10;
            column = 10;
            return true;
        }

        if (id == 10)
        {
            row = 10;
            column = 0;
            return true;
        }

        if (id == 20)
        {
            row = 0;
            column = 0;
            return true;
        }

        if (id == 30)
        {
            row = 0;
            column = 10;
            return true;
        }

        if (TryGetCellPosition(id, out row, out column, out _))
            return true;

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