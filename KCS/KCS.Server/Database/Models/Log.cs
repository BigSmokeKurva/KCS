namespace KCS.Server.Database.Models
{
    public class Log
    {
        public virtual int LogId { get; set; }
        public int Id { get; set; }

        public string Message { get; set; }

        public DateTime Time { get; set; }

        public LogType Type { get; set; }
    }

    public enum LogType
    {
        Chat,
        Action
    }
}
