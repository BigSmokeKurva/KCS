namespace KCS.Server.Database.Models
{
    public class Session
    {
        public Guid AuthToken { get; set; }

        public int Id { get; set; }

        public DateTime Expires { get; set; }

        //public virtual User User { get; set; }
    }
}
