using System.Text.Json.Serialization;

namespace KCS.Server.Controllers.Models;

public class AddSpamTemplateModel
{
    [JsonPropertyName("title")] public string Title { get; set; }
}