using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using KCS.Server.Database.Models;
using static KCS.Server.Database.Models.Proxy;

namespace KCS.Server;

public class UserValidators
{
    private static readonly Regex loginRegex = new("^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
    private static readonly Regex passwordRegex = new("^[a-zA-Z0-9!@#$%^&*()_-]+$", RegexOptions.Compiled);

    internal static bool ValidateLogin(string login)
    {
        int minLength = 4; // Минимальная длина
        int maxLength = 16; // Максимальная длина

        bool hasValidLength = login.Length >= minLength && login.Length <= maxLength;
        bool matchesPattern = loginRegex.IsMatch(login);

        return hasValidLength && matchesPattern;
    }
    internal static bool ValidatePassword(string password)
    {
        int minLength = 5; // Минимальная длина пароля
        int maxLength = 30; // Максимальная длина пароля

        bool hasValidLength = password.Length >= minLength && password.Length <= maxLength;
        bool matchesPattern = passwordRegex.IsMatch(password);

        return hasValidLength && matchesPattern;
    }

    internal static bool ValidateStreamerUsername(string login)
    {
        int minLength = 4; // Минимальная длина
        int maxLength = 25; // Максимальная длина

        bool hasValidLength = login.Length >= minLength && login.Length <= maxLength;
        bool matchesPattern = loginRegex.IsMatch(login);

        return hasValidLength && matchesPattern;
    }
}

/// <summary>
/// Чекер токенов твича
/// </summary>
public class TokenCheck
{
    private static readonly Dictionary<string, string> headers = new()
            {
                {"User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/118.0"},
                {"Accept", "*/*"},
                //{"Authorization", $"OAuth {token}"},
                {"Client-Id", "kimne78kx3ncx6brgo4mv6wki5h1ko"},
                //{"Accept-Encoding", "gzip, deflate, br"},
                {"Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3"},
                {"Host", "gql.twitch.tv" },
                {"Origin", "https://www.twitch.tv/" },
                {"Referer", "https://www.twitch.tv/" },
                {"Connection", "keep-alive"},
                {"Sec-Fetch-Dest", "empty"},
                {"Sec-Fetch-Mode", "cors"},
                {"Sec-Fetch-Site", "same-site"},
            };
    internal static int Threads;

    internal static async Task<Dictionary<string, string>> Check(IEnumerable<string> tokens)
    {
        ConcurrentDictionary<string, string> result = new();
        var content = new StringContent("[{\"operationName\":\"Core_Services_Spade_CurrentUser\",\"variables\":{},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"482be6fdcd0ff8e6a55192210e2ec6db8a67392f206021e81abe0347fc727ebe\"}}}]", Encoding.UTF8, "text/plain"); ;
        using var client = new HttpClient();
        foreach (var header in headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
        await Parallel.ForEachAsync(tokens, new ParallelOptions() { MaxDegreeOfParallelism = Threads }, async (token, e) =>
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://gql.twitch.tv/gql");
                request.Headers.Add("Authorization", $"OAuth {token}");
                request.Content = content;
                var response = await client.SendAsync(request);
                if (response is null)
                    return;
                var json = await response.Content.ReadFromJsonAsync<CoreServicesJson[]>();
                result.TryAdd(token, json[0].data.currentUser.login);
                response.Dispose();

            }
            catch { }
        });
        return UniqueValues(new Dictionary<string, string>(result));
    }
    private static Dictionary<string, string> UniqueValues(Dictionary<string, string> dictionary)
    {
        var result = new Dictionary<string, string>();
        foreach (var item in dictionary)
        {
            if (!result.ContainsValue(item.Value))
                result.Add(item.Key, item.Value);
        }
        return result;
    }
    internal static async Task<Dictionary<TokenItem, List<Tag>>> GetAllTags(IEnumerable<TokenItem> tokens, string streamerUsername)
    {
        ConcurrentDictionary<TokenItem, List<Tag>> result = new();
        foreach (var token in tokens)
        {
            result.TryAdd(token, []);
        }

        var content = new StringContent("[{\"operationName\": \"ChatRestrictions\", \"variables\": {\"channelLogin\": \"" + streamerUsername + "\"}, \"extensions\": {\"persistedQuery\": {\"version\": 1, \"sha256Hash\": \"7514aeb3d2c203087b83e920f8d36eb18a5ca1bfa96a554ed431255ecbbbc089\"}}}]", Encoding.UTF8, "application/json");
        using var client = new HttpClient();
        foreach (var header in headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
        await Parallel.ForEachAsync(tokens, new ParallelOptions() { MaxDegreeOfParallelism = Threads }, async (token, e) =>
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://gql.twitch.tv/gql");
                request.Headers.Add("Authorization", $"OAuth {token.Token}");
                request.Content = content;
                var response = await client.SendAsync(request);
                if (response is null)
                    return;
                var json = await response.Content.ReadFromJsonAsync<ChatRestrictionsJson[]>();
                token.Tags.Clear();
                if (json[0].data.channel is null)
                    return;
                if (json[0].data.channel.self.banStatus is not null)
                {
                    result[token].Add(Tag.Ban);
                }
                if (json[0].data.channel.self.isModerator)
                {
                    result[token].Add(Tag.Moderator);
                }
                if (json[0].data.channel.self.subscriptionBenefit is not null)
                {
                    result[token].Add(Tag.Subscriber);
                }
                if (json[0].data.channel.self.isVIP)
                {
                    result[token].Add(Tag.VIP);
                }
                response.Dispose();
            }
            catch { }
        });
        return new Dictionary<TokenItem, List<Tag>>(result);

    }
    public class CoreServicesJson
    {
        public class CurrentUser
        {
            public string login { get; set; }
        }
        public class Data
        {
            public CurrentUser currentUser { get; set; }
        }

        public Data data { get; set; }
    }
    public class ChatRestrictionsJson
    {
        public class Channel
        {
            public Self self { get; set; }
        }

        public class Data
        {
            public Channel channel { get; set; }
        }
        public class Self
        {
            public object banStatus { get; set; }
            public object subscriptionBenefit { get; set; }
            public bool isVIP { get; set; }
            public bool isModerator { get; set; }
        }
        public Data data { get; set; }
    }
}
public class ProxyCheck
{
    private static readonly HashSet<string> Types = new() { "http", "socks5" };
    internal static List<Proxy> Parse(IEnumerable<string> proxies)
    {
        List<Proxy> result = [];

        foreach (var proxy in proxies)
        {
            try
            {
                var split = proxy.Split(':');

                if (split.Length != 3 && split.Length != 5)
                    continue;

                var type = split[0].ToLower();

                if (!Types.Contains(type))
                    continue;


                result.Add(new Proxy
                {
                    Type = type,
                    Host = split[1],
                    Port = split[2],
                    Credentials = split.Length == 5 ? new UnSafeCredentials(split[3], split[4]) : null
                });
            }
            catch { }
        }

        return result;
    }
    public static string ProxyToString(Proxy proxy)
    {
        return proxy.Credentials is null
            ? $"{proxy.Type}:{proxy.Host}:{proxy.Port}"
            : $"{proxy.Type}:{proxy.Host}:{proxy.Port}:{proxy.Credentials.Value.Username}:{proxy.Credentials.Value.Password}";
    }
    public static string[] ProxyToString(IEnumerable<Proxy> proxies)
    {
        return proxies.Select(ProxyToString).ToArray();
    }
}
