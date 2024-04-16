namespace KCS.Server.Controllers.Models
{
    public class CreateInviteCodeModel
    {
        public string Code { get; set; }
        public string Mode { get; set; }
        public int? Hours { get; set; }
    }
}
