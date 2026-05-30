using System.Collections.ObjectModel;
using MonopolyAdminPanel.Models;

namespace MonopolyAdminPanel.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<PlayerTableItem> Players { get; } = new()
    {
        new("Malahit1", 0, 0, 0, 0),
        new("Malahit2", 0, 0, 0, 0),
        new("Malahit3", 0, 0, 0, 0),
        new("Malahit4", 0, 0, 0, 0),
    };
}