using System;
using System.Collections.Generic;
using System.Text.Json;
using MonopolyAdminPanel.Models;

namespace MonopolyAdminPanel.Services;

public class NetworkMessageHandler
{
    private const string TypeConnectAccept = "connect_accept";
    private const string TypeConnectReject = "connect_reject";
    private const string TypeError = "error";
    private const string TypeServerDisconnect = "server_disconnect";
    private const string TypePlayersList = "players_list";
    private const string TypeGameState = "game_state";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public event Action? ConnectAccepted;
    public event Action<string>? ConnectRejected;
    public event Action<string>? ServerError;
    public event Action? ServerDisconnected;

    public event Action<IReadOnlyList<Player>>? PlayersListReceived;
    public event Action<NetworkMessage>? GameStateReceived;

    public void HandleRawMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            NetworkMessage? message = JsonSerializer.Deserialize<NetworkMessage>(json, JsonOptions);

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

            case TypePlayersList:
                HandlePlayersList(message);
                break;

            case TypeGameState:
                HandleGameState(message);
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

        List<Player>? players = JsonSerializer.Deserialize<List<Player>>(
            playersElement.GetRawText(),
            JsonOptions);

        if (players == null)
        {
            ServerError?.Invoke("Не удалось прочитать список игроков");
            return;
        }

        PlayersListReceived?.Invoke(players);
    }

    private void HandleGameState(NetworkMessage message)
    {
        GameStateReceived?.Invoke(message);
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
}