using System.Text.Json;

namespace MonopolyAdminPanel.Models;

public class NetworkMessage
{
    public string Type { get; set; } = string.Empty;
    public int SenderId { get; set; }
    public JsonElement Payload { get; set; }
}