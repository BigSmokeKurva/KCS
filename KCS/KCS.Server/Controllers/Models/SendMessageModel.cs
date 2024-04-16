namespace KCS.Server.Controllers.Models
{
    public class SendMessageModel
    {
        public string BotName { get; set; }
        public string Message { get; set; }
        public string? ReplyTo { get; set; } = null;
    }
}
