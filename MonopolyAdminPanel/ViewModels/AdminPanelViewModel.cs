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

    public AdminPanelViewModel()
    {
        GameState = new GameState();

        EventLogs.Add("22:20:11   Malahit1 бросил кубик - 4");
        EventLogs.Add("22:18:45   Malahit2 заплатил за аренду Malahit1");
        EventLogs.Add("22:16:03   Malahit2 взял кредит 5000");
    }
}