namespace MonopolyAdminPanel.Models;

public class PlayerTableItem
{
    public string Name { get; set; }

    public int Budget { get; set; }

    public int Purchases { get; set; }

    public int Fines { get; set; }

    public int Cells { get; set; }

    public PlayerTableItem(
        string name,
        int budget,
        int purchases,
        int fines,
        int cells)
    {
        Name = name;
        Budget = budget;
        Purchases = purchases;
        Fines = fines;
        Cells = cells;
    }
}