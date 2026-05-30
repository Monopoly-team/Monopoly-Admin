using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using MonopolyAdminPanel.Services;

namespace MonopolyAdminPanel.Views;

public partial class AdminPanelView : UserControl
{
    private const string ConnectedText = "● Подключено";
    private const string DisconnectedText = "● Не подключено";

    private NetworkService? _networkService;

    public AdminPanelView()
    {
        InitializeComponent();
    }

    public AdminPanelView(NetworkService networkService, string serverIp)
        : this()
    {
        _networkService = networkService;

        ServerIpText.Text = serverIp;

        _networkService.ConnectionChanged += OnConnectionChanged;

        UpdateConnectionStatus(_networkService.IsConnected);
    }

    private void DisconnectButton_Click(object? sender, RoutedEventArgs e)
    {
        _networkService?.Disconnect();
    }

    private void OnConnectionChanged(bool isConnected)
    {
        Dispatcher.UIThread.Post(() => UpdateConnectionStatus(isConnected));
    }

    private void UpdateConnectionStatus(bool isConnected)
    {
        if (ConnectionStatusText == null)
            return;

        ConnectionStatusText.Text =
            isConnected ? ConnectedText : DisconnectedText;

        ConnectionStatusText.Foreground =
            isConnected ? Brushes.LimeGreen : Brushes.Red;
    }
}