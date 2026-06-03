using MonopolyAdminPanel.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private IReadOnlyList<Player> _lastPlayers = [];
    private bool _isGameStarted;
    private bool _isGamePaused;

    public bool IsConnected => _client?.Connected == true;
    public IReadOnlyList<Player> LastPlayers => _lastPlayers;
    public bool IsGameStarted => _isGameStarted;
    public bool IsGamePaused => _isGamePaused;

    public event Action<bool>? ConnectionChanged;
    public event Action<string>? ErrorReceived;
    public event Action<IReadOnlyList<Player>>? PlayersListReceived;
    public event Action<string>? GameEventReceived;

    public event Action? GameStarted;
    public event Action<string>? GamePaused;
    public event Action? GameResumed;
    public event Action<int, int>? DiceRolled;

    public NetworkService()
    {
        _messageHandler.ServerError += message =>
        {
            Debug.WriteLine($"[NetworkService] ServerError: {message}");
            ErrorReceived?.Invoke(message);
        };

        _messageHandler.ServerDisconnected += () =>
        {
            Debug.WriteLine("[NetworkService] ServerDisconnected event received");
            Disconnect();
        };

        _messageHandler.PlayersListReceived += players =>
        {
            Debug.WriteLine($"[NetworkService] PlayersListReceived: {players.Count}");

            _lastPlayers = players;

            PlayersListReceived?.Invoke(players);
        };

        _messageHandler.GameStarted += () =>
        {
            Debug.WriteLine("[NetworkService] GameStarted event received");

            _isGameStarted = true;

            GameStarted?.Invoke();
        };

        _messageHandler.GameEventReceived += text =>
        {
            Debug.WriteLine($"[NetworkService] GameEventReceived: {text}");
            GameEventReceived?.Invoke(text);
        };

        _messageHandler.GamePaused += reason =>
        {
            Debug.WriteLine($"[NetworkService] GamePaused: {reason}");

            _isGamePaused = true;

            GamePaused?.Invoke(reason);
        };

        _messageHandler.GameResumed += () =>
        {
            Debug.WriteLine("[NetworkService] GameResumed");

            _isGamePaused = false;

            GameResumed?.Invoke();
        };

        _messageHandler.DiceRolled += (first, second) =>
        {
            Debug.WriteLine($"[NetworkService] DiceRolled: {first}, {second}");
            DiceRolled?.Invoke(first, second);
        };
    }

    public async Task ConnectAsync(string ip, int port)
    {
        Debug.WriteLine($"[NetworkService] Connecting to {ip}:{port}");

        Disconnect();

        _client = new TcpClient();
        await _client.ConnectAsync(ip, port);

        Debug.WriteLine("[NetworkService] Connected");

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
        Debug.WriteLine("[NetworkService] Sending connect_request");

        await SendConnectRequestAsync();

        string? response = await ReadLineAsync();

        Debug.WriteLine($"[NetworkService] Connect response raw: {response}");

        if (response == null)
        {
            ErrorReceived?.Invoke("Сервер закрыл соединение во время подключения");
            return false;
        }

        bool accepted = false;

        void OnAccepted()
        {
            Debug.WriteLine("[NetworkService] connect_accept received");
            accepted = true;
        }

        void OnRejected(string reason)
        {
            Debug.WriteLine($"[NetworkService] connect_reject received: {reason}");
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
        string json =
            $"{{\"type\":\"connect_request\",\"senderId\":{AdminSenderId},\"payload\":{{\"role\":\"admin\"}}}}";

        await SendAsync(json);
    }

    public async Task SendAsync(string json)
    {
        if (_writer == null)
            throw new InvalidOperationException("Нет подключения к серверу");

        Debug.WriteLine($"[NetworkService] SEND: {json}");

        await _writer.WriteLineAsync(json);
    }

    private void StartReceiveLoop()
    {
        if (_cts == null)
            return;

        Debug.WriteLine("[NetworkService] Receive loop started");

        _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
    }
    public void ClearCachedState()
    {
        _lastPlayers = [];
        _isGameStarted = false;
        _isGamePaused = false;
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                string? line = await ReadLineAsync();

                if (line == null)
                {
                    Debug.WriteLine("[NetworkService] ReadLine returned null. Server closed connection.");
                    break;
                }

                Debug.WriteLine($"[NetworkService] RECEIVE: {line}");

                _messageHandler.HandleRawMessage(line);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NetworkService] ReceiveLoop exception: {ex.Message}");
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
        Debug.WriteLine("[NetworkService] Disconnect");

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