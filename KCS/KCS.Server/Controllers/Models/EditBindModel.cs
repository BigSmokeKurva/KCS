namespace KCS.Server.Controllers.Models
{
    public class EditBindModel
    {
        public string Name { get; set; }
        public string[] Messages { get; set; }
        public string[]? HotKeys { get; set; } = null;
        public string OldName { get; set; }
    }
}
