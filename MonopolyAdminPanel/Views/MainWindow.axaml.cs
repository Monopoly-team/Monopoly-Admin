using Avalonia.Controls;
using MonopolyAdminPanel.Views;

namespace MonopolyAdminPanel.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void LoginButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Content = new AdminPanelView();
    }
}