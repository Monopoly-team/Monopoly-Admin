using System;
using System.Collections.Generic;
using System.Text.Json;
using MonopolyAdminPanel.Models;
using System.Diagnostics;

namespace MonopolyAdminPanel.Services;

public class NetworkMessageHandler
{
    private const string TypeConnectAccept = "connect_accept";
    private const string TypeConnectReject = "connect_reject";
    private const string TypeError = "error";
    private const string TypeServerDisconnect = "server_disconnect";
    private const string TypePlayersListLobby = "players_list_lobby";
    private const string TypePlayersListGame = "players_list_game";
    private const string TypeGameState = "game_state";
    private const string TypeGameStarted = "game_started";
    private const string TypeGameEvent = "game_event";
    private const string TypeGamePaused = "game_paused";
    private const string TypeGameResumed = "game_resumed";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public event Action? ConnectAccepted;
    public event Action<string>? ConnectRejected;
    public event Action<string>? ServerError;
    public event Action? ServerDisconnected;
    public event Action<int, int>? DiceRolled;

    public event Action<IReadOnlyList<Player>>? PlayersListReceived;
    public event Action<NetworkMessage>? GameStateReceived;

    public event Action? GameStarted;
    public event Action<string>? GameEventReceived;
    public event Action<string>? GamePaused;
    public event Action? GameResumed;

    public void HandleRawMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            NetworkMessage? message = JsonSerializer.Deserialize<NetworkMessage>(json, JsonOptions);
            Debug.WriteLine($"[MessageHandler] RAW TYPE = {message?.Type}");

            if (message == null)
            {
                ServerError?.Invoke("Сервер отправил пустое сообщение");
                return;
            }

            HandleMessage(message);
        }
        catch (JsonException ex)
        {
            ServerError?.Invoke($"Ошибка парсинга JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            ServerError?.Invoke($"Ошибка обработки сообщения: {ex.Message}");
        }
    }

    private void HandleMessage(NetworkMessage message)
    {
        Debug.WriteLine($"[MessageHandler] HandleMessage: {message.Type}");

        switch (message.Type)
        {
            case TypeConnectAccept:
                HandleConnectAccept();
                break;

            case TypeConnectReject:
                HandleConnectReject(message);
                break;

            case TypeError:
                HandleError(message);
                break;

            case TypeServerDisconnect:
                HandleServerDisconnect();
                break;

            case TypePlayersListLobby:
                HandlePlayersList(message);
                break;

            case TypePlayersListGame:
                HandlePlayersList(message);
                break;

            case TypeGameState:
                HandleGameState(message);
                break;

            case TypeGameStarted:
                HandleGameStarted();
                break;

            case TypeGameEvent:
                HandleGameEvent(message);
                break;

            case TypeGamePaused:
                HandleGamePaused(message);
                break;

            case TypeGameResumed:
                HandleGameResumed();
                break;

            default:
                ServerError?.Invoke($"Неизвестный тип сообщения: {message.Type}");
                break;
        }
    }

    private void HandleConnectAccept()
    {
        ConnectAccepted?.Invoke();
    }

    private void HandleConnectReject(NetworkMessage message)
    {
        string reason = GetPayloadString(message, "reason", "Подключение отклонено сервером");
        ConnectRejected?.Invoke(reason);
    }

    private void HandleError(NetworkMessage message)
    {
        string error = GetPayloadString(message, "message", "Ошибка от сервера");
        ServerError?.Invoke(error);
    }

    private void HandleServerDisconnect()
    {
        ServerDisconnected?.Invoke();
    }

    private void HandlePlayersList(NetworkMessage message)
    {
        Debug.WriteLine("[MessageHandler] HandlePlayersList entered");

        if (message.Payload.ValueKind != JsonValueKind.Object)
        {
            ServerError?.Invoke("Некорректный payload у players_list");
            return;
        }

        if (!message.Payload.TryGetProperty("players", out JsonElement playersElement))
        {
            ServerError?.Invoke("В players_list отсутствует payload.players");
            return;
        }

        Debug.WriteLine("[MessageHandler] payload.players found");
        Debug.WriteLine(playersElement.GetRawText());

        List<Player>? players = JsonSerializer.Deserialize<List<Player>>(
            playersElement.GetRawText(),
            JsonOptions);

        Debug.WriteLine($"[MessageHandler] Deserialized players count = {players?.Count ?? 0}");

        if (players == null)
        {
            ServerError?.Invoke("Не удалось прочитать список игроков");
            return;
        }

        foreach (Player player in players)
        {
            Debug.WriteLine(
                $"[MessageHandler] Player: Id={player.Id} Name={player.Name} Connected={player.IsConnected}");
        }

        PlayersListReceived?.Invoke(players);
    }

    private void HandleGameState(NetworkMessage message)
    {
        if (message.Payload.ValueKind == JsonValueKind.Object &&
            message.Payload.TryGetProperty("lastDiceFirst", out JsonElement firstElement) &&
            message.Payload.TryGetProperty("lastDiceSecond", out JsonElement secondElement))
        {
            int first = firstElement.GetInt32();
            int second = secondElement.GetInt32();

            DiceRolled?.Invoke(first, second);
        }

        GameStateReceived?.Invoke(message);
    }

    private void HandleGameStarted()
    {
        GameStarted?.Invoke();
    }

    private static string GetPayloadString(
        NetworkMessage message,
        string propertyName,
        string defaultValue)
    {
        if (message.Payload.ValueKind != JsonValueKind.Object)
            return defaultValue;

        if (!message.Payload.TryGetProperty(propertyName, out JsonElement value))
            return defaultValue;

        return value.GetString() ?? defaultValue;
    }

    private void HandleGameEvent(NetworkMessage message)
    {
        string text = GetPayloadString(message, "text", "");

        if (string.IsNullOrWhiteSpace(text))
        {
            ServerError?.Invoke("В game_event отсутствует payload.text");
            return;
        }

        GameEventReceived?.Invoke(text);
    }
    private void HandleGamePaused(NetworkMessage message)
    {
        string reason = GetPayloadString(message, "reason", "Игра поставлена на паузу");
        GamePaused?.Invoke(reason);
    }

    private void HandleGameResumed()
    {
        GameResumed?.Invoke();
    }
}