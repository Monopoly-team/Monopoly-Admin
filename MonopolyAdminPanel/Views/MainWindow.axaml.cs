using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MonopolyAdminPanel.Services;

namespace MonopolyAdminPanel.Views;

public partial class MainWindow : Window
{
    private const int ServerPort = 7777;

    private readonly NetworkService _networkService = new();

    private Control? _loginContent;

    public MainWindow()
    {
        InitializeComponent();

        _loginContent = Content as Control;

        _networkService.ErrorReceived += OnNetworkErrorReceived;
    }

    private async void LoginButton_Click(object? sender, RoutedEventArgs e)
    {
        string ip = GetServerIp();

        if (string.IsNullOrWhiteSpace(ip))
        {
            Debug.WriteLine("IP сервера не введён");
            return;
        }

        try
        {
            await _networkService.ConnectAsync(ip, ServerPort);

            bool accepted = await _networkService.SendConnectRequestAndWaitAnswerAsync();

            if (!accepted)
            {
                _networkService.Disconnect();
                return;
            }

            OpenAdminPanel(ip);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка подключения к серверу: {ex.Message}");
            _networkService.Disconnect();
        }
    }

    private string GetServerIp()
    {
        var ipTextBox = this.FindControl<TextBox>("IpTextBox");
        return ipTextBox?.Text?.Trim() ?? string.Empty;
    }

    private void OpenAdminPanel(string ip)
    {
        Content = new AdminPanelView(
            _networkService,
            ip,
            ReturnToLogin);
    }

    private void ReturnToLogin()
    {
        _networkService.Disconnect();
        _networkService.ClearCachedState();

        if (_loginContent != null)
            Content = _loginContent;
    }

    private void OnNetworkErrorReceived(string message)
    {
        Debug.WriteLine($"Ошибка сервера: {message}");
    }
}