using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonopolyAdminPanel.Services;

public class NetworkService
{
    private const int AdminSenderId = 65535;

    private readonly NetworkMessageHandler _messageHandler = new();

    private TcpClient? _client;
    private NetworkStream? _stream;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private CancellationTokenSource? _cts;

    public bool IsConnected => _client?.Connected == true;

    public event Action<bool>? ConnectionChanged;
    public event Action<string>? ErrorReceived;

    public NetworkService()
    {
        _messageHandler.ServerError += message => ErrorReceived?.Invoke(message);
        _messageHandler.ServerDisconnected += Disconnect;
    }

    public async Task ConnectAsync(string ip, int port)
    {
        Disconnect();

        _client = new TcpClient();
        await _client.ConnectAsync(ip, port);

        _stream = _client.GetStream();
        _reader = new StreamReader(_stream, Encoding.UTF8);
        _writer = new StreamWriter(_stream, Encoding.UTF8)
        {
            AutoFlush = true
        };

        _cts = new CancellationTokenSource();

        ConnectionChanged?.Invoke(true);
    }

    public async Task<bool> SendConnectRequestAndWaitAnswerAsync()
    {
        await SendConnectRequestAsync();

        string? response = await ReadLineAsync();

        if (response == null)
        {
            ErrorReceived?.Invoke("Сервер закрыл соединение во время подключения");
            return false;
        }

        bool accepted = false;

        void OnAccepted()
        {
            accepted = true;
        }

        void OnRejected(string reason)
        {
            ErrorReceived?.Invoke(reason);
        }

        _messageHandler.ConnectAccepted += OnAccepted;
        _messageHandler.ConnectRejected += OnRejected;

        _messageHandler.HandleRawMessage(response);

        _messageHandler.ConnectAccepted -= OnAccepted;
        _messageHandler.ConnectRejected -= OnRejected;

        if (accepted)
            StartReceiveLoop();

        return accepted;
    }

    public async Task SendConnectRequestAsync()
    {
        string json = $"{{\"type\":\"connect_request\",\"senderId\":{AdminSenderId},\"payload\":{{\"role\":\"admin\"}}}}";

        await SendAsync(json);
    }

    public async Task SendAsync(string json)
    {
        if (_writer == null)
            throw new InvalidOperationException("Нет подключения к серверу");

        await _writer.WriteLineAsync(json);
    }

    private void StartReceiveLoop()
    {
        if (_cts == null)
            return;

        _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                string? line = await ReadLineAsync();

                if (line == null)
                    break;

                _messageHandler.HandleRawMessage(line);
            }
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke($"Ошибка чтения сообщения от сервера: {ex.Message}");
        }

        Disconnect();
    }

    private async Task<string?> ReadLineAsync()
    {
        if (_reader == null)
            return null;

        return await _reader.ReadLineAsync();
    }

    public void Disconnect()
    {
        _cts?.Cancel();

        _reader?.Dispose();
        _writer?.Dispose();
        _stream?.Dispose();
        _client?.Close();

        _reader = null;
        _writer = null;
        _stream = null;
        _client = null;
        _cts = null;

        ConnectionChanged?.Invoke(false);
    }
}