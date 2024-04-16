namespace KCS.Server.Database.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public bool Admin { get; set; } = false;
        public bool Paused { get; set; } = false;
        public DateTime LastOnline { get; set; } = TimeHelper.GetUnspecifiedUtc();
        public virtual Configuration Configuration { get; set; } = new();
        public virtual List<Session> Sessions { get; set; } = [];
        public virtual List<Log> Logs { get; set; } = [];
    }
}
