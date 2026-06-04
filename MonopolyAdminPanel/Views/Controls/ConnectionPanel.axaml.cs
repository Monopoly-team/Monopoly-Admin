using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;

namespace MonopolyAdminPanel.Views.Controls;

public partial class ConnectionPanel : UserControl
{
    private const string ConnectedText = "● Подключено";
    private const string DisconnectedText = "● Не подключено";

    private TextBlock? _connectionStatusText;
    private TextBlock? _serverIpText;

    public event Action? DisconnectRequested;

    public ConnectionPanel()
    {
        InitializeComponent();

        _connectionStatusText = this.FindControl<TextBlock>("ConnectionStatusText");
        _serverIpText = this.FindControl<TextBlock>("ServerIpText");
    }

    public void SetServerIp(string serverIp)
    {
        if (_serverIpText != null)
            _serverIpText.Text = serverIp;
    }

    public void SetConnectionStatus(bool isConnected)
    {
        if (_connectionStatusText == null)
            return;

        _connectionStatusText.Text = isConnected ? ConnectedText : DisconnectedText;
        _connectionStatusText.Foreground = isConnected ? Brushes.LimeGreen : Brushes.Red;
    }

    private void DisconnectButton_Click(object? sender, RoutedEventArgs e)
    {
        DisconnectRequested?.Invoke();
    }
}