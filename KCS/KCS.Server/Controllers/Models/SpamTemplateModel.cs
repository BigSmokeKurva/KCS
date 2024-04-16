using KCS.Server.Database.Models;

namespace KCS.Server.Controllers.Models
{
    public class SpamTemplateModel
    {
        public string Title { get; set; }
        public string OldTitle { get; set; }
        public int Threads { get; set; }
        public int Delay { get; set; }
        public string[] Messages { get; set; }
        public SpamMode Mode { get; set; }
    }
}
