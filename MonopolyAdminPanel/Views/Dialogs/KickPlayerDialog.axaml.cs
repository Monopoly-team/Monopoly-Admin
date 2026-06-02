using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MonopolyAdminPanel.Models;

namespace MonopolyAdminPanel.Views.Dialogs;

public partial class KickPlayerDialog : Window
{
    private ComboBox? _playersComboBox;
    private TextBox? _reasonTextBox;

    public Player? SelectedPlayer => _playersComboBox?.SelectedItem as Player;

    public string Reason => _reasonTextBox?.Text ?? string.Empty;

    public KickPlayerDialog()
    {
        InitializeComponent();

        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _playersComboBox = this.FindControl<ComboBox>("PlayersComboBox");
        _reasonTextBox = this.FindControl<TextBox>("ReasonTextBox");
    }

    public KickPlayerDialog(IReadOnlyList<Player> players)
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

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
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