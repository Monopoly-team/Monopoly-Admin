using System.Collections.ObjectModel;

namespace MonopolyAdminPanel.Models;

public class GameState
{
    public ObservableCollection<Player> Players { get; } = new();

    public int CurrentPlayerId { get; set; }

    public bool IsGameStarted { get; set; }

    public int BankBalance { get; set; }

    public int TotalPurchases { get; set; }

    public int TotalFines { get; set; }

    public int TotalBonuses { get; set; }

    public int TotalEvents { get; set; }

    public int TotalTurns { get; set; }
}