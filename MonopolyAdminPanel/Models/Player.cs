namespace MonopolyAdminPanel.Models;

public class Player
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Balance { get; set; }

    public string Color { get; set; } = "#FFFFFF";

    public bool IsConnected { get; set; }

    public int Purchases { get; set; }

    public int Fines { get; set; }

    public int Bonuses { get; set; }

    public int OwnedCellsCount { get; set; }

    public override string ToString()
    {
        return Name;
    }
}