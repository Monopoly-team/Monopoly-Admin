using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace MonopolyAdminPanel.Views.Controls;

public partial class AdminActionsPanel : UserControl
{
    private Button? _pauseGameButton;

    public event Action? ChangeBalanceRequested;
    public event Action? FinePlayerRequested;
    public event Action? BonusPlayerRequested;
    public event Action? KickPlayerRequested;
    public event Action? StartEventRequested;
    public event Action? PauseGameRequested;

    public AdminActionsPanel()
    {
        InitializeComponent();

        _pauseGameButton = this.FindControl<Button>("PauseGameButton");
    }

    public void SetPauseButtonText(bool isGamePaused)
    {
        if (_pauseGameButton != null)
            _pauseGameButton.Content = isGamePaused ? "Запустить игру" : "Остановить игру";
    }

    private void ChangeBalanceButton_Click(object? sender, RoutedEventArgs e)
    {
        ChangeBalanceRequested?.Invoke();
    }

    private void FinePlayerButton_Click(object? sender, RoutedEventArgs e)
    {
        FinePlayerRequested?.Invoke();
    }

    private void BonusPlayerButton_Click(object? sender, RoutedEventArgs e)
    {
        BonusPlayerRequested?.Invoke();
    }

    private void KickPlayerButton_Click(object? sender, RoutedEventArgs e)
    {
        KickPlayerRequested?.Invoke();
    }

    private void StartEventButton_Click(object? sender, RoutedEventArgs e)
    {
        StartEventRequested?.Invoke();
    }

    private void PauseGameButton_Click(object? sender, RoutedEventArgs e)
    {
        PauseGameRequested?.Invoke();
    }
}