using System.Text.Json.Serialization;

namespace KCS.Server.Controllers.Models;

public struct ChatroomObject
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("channel_id")] public int ChannelId { get; set; }
}

public struct StreamerInfoResponse
{
    [JsonPropertyName("chatroom")] public ChatroomObject? Chatroom { get; set; }
    [JsonPropertyName("id")] public int Id { get; set; }
}