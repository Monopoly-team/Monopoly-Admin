using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MonopolyAdminPanel.Models;

namespace MonopolyAdminPanel.Views.Dialogs;

public partial class ChangeBalanceDialog : Window
{
    private ComboBox? _playersComboBox;
    private TextBox? _balanceTextBox;
    private TextBox? _reasonTextBox;

    public Player? SelectedPlayer => _playersComboBox?.SelectedItem as Player;

    public int? Balance
    {
        get
        {
            string text = _balanceTextBox?.Text?.Trim() ?? "";

            if (int.TryParse(text, out int balance))
                return balance;

            return null;
        }
    }

    public string Reason => _reasonTextBox?.Text?.Trim() ?? string.Empty;

    public ChangeBalanceDialog()
    {
        InitializeComponent();

        _playersComboBox = this.FindControl<ComboBox>("PlayersComboBox");
        _balanceTextBox = this.FindControl<TextBox>("BalanceTextBox");
        _reasonTextBox = this.FindControl<TextBox>("ReasonTextBox");
    }

    public ChangeBalanceDialog(IReadOnlyList<Player> players)
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