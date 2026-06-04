using MonopolyAdminPanel.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace MonopolyAdminPanel.Services;

public static class GameStateParser
{
    public static List<Player> ReadPlayers(
        JsonElement playersElement,
        IReadOnlyList<Player> previousPlayers)
    {
        var players = new List<Player>();

        if (playersElement.ValueKind != JsonValueKind.Array)
            return players;

        foreach (JsonElement playerElement in playersElement.EnumerateArray())
        {
            int id = GetIntProperty(playerElement, "id", 0);

            Player? previousPlayer = FindPlayerById(previousPlayers, id);

            int ownedCellsCount = GetArrayCountProperty(playerElement, "ownedProperties");

            if (ownedCellsCount == 0)
                ownedCellsCount = GetArrayCountProperty(playerElement, "ownedCells");

            var player = new Player
            {
                Id = id,
                Name = GetStringProperty(
                    playerElement,
                    "nickname",
                    GetStringProperty(playerElement, "name", previousPlayer?.Name ?? $"Игрок {id}")),

                Balance = GetIntProperty(playerElement, "balance", previousPlayer?.Balance ?? 0),

                Color = GetStringProperty(playerElement, "color", previousPlayer?.Color ?? "#FFFFFF"),

                IsConnected = GetBoolProperty(
                    playerElement,
                    "active",
                    GetBoolProperty(playerElement, "isConnected", previousPlayer?.IsConnected ?? true)),

                OwnedCellsCount = ownedCellsCount,

                Purchases = previousPlayer?.Purchases ?? 0,
                Fines = previousPlayer?.Fines ?? 0,
                Bonuses = previousPlayer?.Bonuses ?? 0
            };

            players.Add(player);
        }

        return players;
    }

    public static Player? FindPlayerById(IReadOnlyList<Player> players, int id)
    {
        foreach (Player player in players)
        {
            if (player.Id == id)
                return player;
        }

        return null;
    }

    private static string GetStringProperty(JsonElement element, string propertyName, string defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
            return defaultValue;

        if (value.ValueKind != JsonValueKind.String)
            return defaultValue;

        return value.GetString() ?? defaultValue;
    }

    private static int GetIntProperty(JsonElement element, string propertyName, int defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
            return defaultValue;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int result))
            return result;

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out result))
            return result;

        return defaultValue;
    }

    private static bool GetBoolProperty(JsonElement element, string propertyName, bool defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
            return defaultValue;

        if (value.ValueKind == JsonValueKind.True)
            return true;

        if (value.ValueKind == JsonValueKind.False)
            return false;

        if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out bool result))
            return result;

        return defaultValue;
    }

    private static int GetArrayCountProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
            return 0;

        if (value.ValueKind != JsonValueKind.Array)
            return 0;

        return value.GetArrayLength();
    }
}