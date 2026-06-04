using System.Collections.ObjectModel;
using MonopolyAdminPanel.Models;

namespace MonopolyAdminPanel.ViewModels;

public class AdminPanelViewModel : ViewModelBase
{
    public GameState GameState { get; }

    public ObservableCollection<Player> Players => GameState.Players;

    public ObservableCollection<string> EventLogs { get; } = new();

    public Player? SelectedPlayer { get; set; }

    public string ServerIp { get; set; } = string.Empty;

    public bool IsConnected { get; set; }

}