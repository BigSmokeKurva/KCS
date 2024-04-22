namespace KCS.Server.Controllers.Models;

public struct SendMessageModel
{
    public string BotName { get; set; }
    public string Message { get; set; }
    public Metadata? Metadata { get; set; }
}

public struct Metadata
{
    public OriginalMessage OriginalMessage { get; set; }
    public OriginalSender OriginalSender { get; set; }
}

public struct OriginalMessage
{
    public string Id { get; set; }
    public string Content { get; set; }
}

public struct OriginalSender
{
    public int Id { get; set; }
    public string Username { get; set; }
}