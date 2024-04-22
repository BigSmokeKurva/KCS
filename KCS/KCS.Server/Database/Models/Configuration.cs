using System.Net;

namespace KCS.Server.Database.Models
{
    public class Configuration
    {
        public int Id { get; set; }
        public List<TokenItem> Tokens { get; set; } = [];
        public StreamerInfo StreamerInfo { get; set; } = new();
        public List<SpamTemplate> SpamTemplates { get; set; } = [];
        public List<Bind> Binds { get; set; } = [];
    }

    public class StreamerInfo
    {
        public string Username { get; set; } = string.Empty;
        public int? ChatroomId { get; set; } = null;
    }

    public class SpamTemplate
    {
        public int Delay { get; set; } = 1;
        public int Threads { get; set; } = 1;
        public required string Title { get; set; }
        public List<string> Messages { get; set; } = [];
        public SpamMode Mode { get; set; } = SpamMode.Random;
    }

    public enum SpamMode
    {
        Random,
        List
    }

    public class Bind
    {
        public required string Title { get; set; }
        public List<string>? HotKeys { get; set; } = null;
        public List<string> Messages { get; set; } = [];
    }

    public struct Proxy
    {
        public struct UnSafeCredentials(string username, string password) : ICredentials
        {
            public string Username { get; set; } = username;
            public string Password { get; set; } = password;

            public readonly NetworkCredential GetCredential(Uri uri, string authType)
            {
                return new NetworkCredential(Username, Password);
            }
        }

        public string Type { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        public UnSafeCredentials? Credentials { get; set; }

        public static implicit operator WebProxy(Proxy v)
        {
            return new WebProxy($"{v.Type}://{v.Host}", int.Parse(v.Port))
            {
                Credentials = v.Credentials
            };
        }
    }

    public class TokenItem
    {
        public string Username { get; set; }
        public string Token1 { get; set; }
        public string Token2 { get; set; }
        public string Token3 { get; set; }
        public string Token4 { get; set; }
        public Proxy Proxy { get; set; }
        public List<Tag> Tags { get; set; } = [];
    }

    public enum Tag
    {
        Broadcaster,
        Moderator,
        Subscriber,
        VIP,
        Ban
    }
}