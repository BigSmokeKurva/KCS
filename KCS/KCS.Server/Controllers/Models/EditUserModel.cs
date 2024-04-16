using System.Text.Json;

namespace KCS.Server.Controllers.Models
{
    public enum ChangeType
    {
        Username = 0,
        Password = 1,
        Admin = 2,
        Tokens = 3,
        Paused = 4
    }

    public class EditUserModel
    {
        public int Id { get; set; }
        public ChangeType Property { get; set; }
        public JsonElement Value { get; set; }
    }
}
