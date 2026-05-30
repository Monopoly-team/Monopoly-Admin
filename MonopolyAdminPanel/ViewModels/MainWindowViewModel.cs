using System.Collections.ObjectModel;

namespace MonopolyAdminPanel.ViewModels

{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<PlayerTableItem> Players { get; } = new()
        {
            new PlayerTableItem("Malahit1", 0, 0, 0, 0),
            new PlayerTableItem("Malahit2", 0, 0, 0, 0),
            new PlayerTableItem("Malahit3", 0, 0, 0, 0),
            new PlayerTableItem("Malahit4", 0, 0, 0, 0),
        };
    }

    public class PlayerTableItem
    {
        public string Name { get; set; }
        public int Budget { get; set; }
        public int Purchases { get; set; }
        public int Fines { get; set; }
        public int Cells { get; set; }

        public PlayerTableItem(string name, int budget, int purchases, int fines, int cells)
        {
            Name = name;
            Budget = budget;
            Purchases = purchases;
            Fines = fines;
            Cells = cells;
        }
    }
}
