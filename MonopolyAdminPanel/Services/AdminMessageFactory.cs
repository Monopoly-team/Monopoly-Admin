using System.Collections.Generic;
using System.Text.Json;

namespace MonopolyAdminPanel.Services;

public static class AdminMessageFactory
{
    private const int AdminSenderId = 65535;

    public static string CreateAdminAction(Dictionary<string, object> payload)
    {
        return JsonSerializer.Serialize(new
        {
            type = "admin_action",
            senderId = AdminSenderId,
            payload
        });
    }

    public static string CreateChatMessage(string text)
    {
        return JsonSerializer.Serialize(new
        {
            type = "chat_message",
            senderId = AdminSenderId,
            payload = new
            {
                text
            }
        });
    }
}