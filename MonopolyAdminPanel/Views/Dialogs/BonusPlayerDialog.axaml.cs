using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MonopolyAdminPanel.Models;

namespace MonopolyAdminPanel.Views.Dialogs;

public partial class BonusPlayerDialog : Window
{
    private ComboBox? _playersComboBox;
    private TextBox? _amountTextBox;
    private TextBox? _reasonTextBox;

    public Player? SelectedPlayer => _playersComboBox?.SelectedItem as Player;

    public int? Amount
    {
        get
        {
            string text = _amountTextBox?.Text?.Trim() ?? string.Empty;

            if (int.TryParse(text, out int amount))
                return amount;

            return null;
        }
    }

    public string Reason => _reasonTextBox?.Text?.Trim() ?? string.Empty;

    public BonusPlayerDialog()
    {
        InitializeComponent();

        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _playersComboBox = this.FindControl<ComboBox>("PlayersComboBox");
        _amountTextBox = this.FindControl<TextBox>("AmountTextBox");
        _reasonTextBox = this.FindControl<TextBox>("ReasonTextBox");
    }

    public BonusPlayerDialog(IReadOnlyList<Player> players)
        : this()
    {
        if (_playersComboBox == null)
            return;

        _playersComboBox.ItemsSource = players;

        if (players.Count > 0)
            _playersComboBox.SelectedIndex = 0;
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}