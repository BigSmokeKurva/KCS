namespace KCS.Server.Database.Models;

public class BotInfo
{
    public string Username { get; set; }
    public List<string> Followed { get; set; } = [];
}