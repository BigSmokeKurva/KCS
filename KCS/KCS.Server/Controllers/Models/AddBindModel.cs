using System.Text.Json.Serialization;

namespace KCS.Server.Controllers.Models;

public class AddBindModel
{
    [JsonPropertyName("bindname")] public string BindName { get; set; }
}