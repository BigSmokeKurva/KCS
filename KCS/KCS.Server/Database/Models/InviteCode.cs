namespace KCS.Server.Database.Models
{
    public class InviteCode
    {
        public string Code { get; set; }
        public InviteCodeStatus Status { get; set; }
        public DateTime? Expires { get; set; } = null;
        public int? UserId { get; set; } = null;
        public DateTime? ActivationDate { get; set; } = null;
        public InviteCodeMode Mode { get; set; }
    }
    public enum InviteCodeStatus
    {
        Active,
        Used,
        Expired
    }
    public enum InviteCodeMode
    {
        Time,
        Unlimited
    }
}
