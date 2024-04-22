using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using KCS.Server.Database.Models;

namespace KCS.Server;

public static class UserValidators
{
    private static readonly Regex LoginRegex = new("^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
    private static readonly Regex PasswordRegex = new("^[a-zA-Z0-9!@#$%^&*()_-]+$", RegexOptions.Compiled);

    internal static bool ValidateLogin(string login)
    {
        const int minLength = 4; // Минимальная длина
        const int maxLength = 16; // Максимальная длина

        var hasValidLength = login.Length is >= minLength and <= maxLength;
        var matchesPattern = LoginRegex.IsMatch(login);

        return hasValidLength && matchesPattern;
    }

    internal static bool ValidatePassword(string? password)
    {
        const int minLength = 5; // Минимальная длина пароля
        const int maxLength = 30; // Максимальная длина пароля

        var hasValidLength = password is { Length: >= minLength and <= maxLength };
        var matchesPattern = password != null && PasswordRegex.IsMatch(password);

        return hasValidLength && matchesPattern;
    }

    internal static bool ValidateStreamerUsername(string login)
    {
        const int minLength = 4; // Минимальная длина
        const int maxLength = 25; // Максимальная длина

        var hasValidLength = login.Length is >= minLength and <= maxLength;
        var matchesPattern = LoginRegex.IsMatch(login);

        return hasValidLength && matchesPattern;
    }
}

/// <summary>
/// Чекер токенов твича
/// </summary>
public static class TokenCheck
{
    private static readonly Dictionary<string, string> Headers = new()
    {
        { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/118.0" },
        { "Accept", "*/*" },
        { "Referer", "https://kick.com/" },
    };

    internal static int Threads;

    internal static async Task<Dictionary<string, string>> Check(IEnumerable<(string, string, string)> tokens,
        HttpClient client)
    {
        ConcurrentDictionary<string, string> result = new();

        await Parallel.ForEachAsync(tokens, new ParallelOptions() { MaxDegreeOfParallelism = Threads },
            async (tokensCouple, e) =>
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, "https://kick.com/api/v1/user");
                    request.Headers.Add("Cookie",
                        $"kick_session={tokensCouple.Item1}; {tokensCouple.Item2}={tokensCouple.Item3}");
                    foreach (var header in Headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }

                    var response = await client.SendAsync(request, e);
                    var json = await response.Content.ReadFromJsonAsync<UserJson>(cancellationToken: e);
                    result.TryAdd(tokensCouple.Item1, json.Username ?? string.Empty);
                    response.Dispose();
                }
                catch
                {
                    // ignored
                }
            });
        return new Dictionary<string, string>(result).GroupBy(pair => pair.Value)
            .Select(group => group.First())
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    internal static async Task<Dictionary<TokenItem, List<Tag>>> GetAllTags(IEnumerable<TokenItem> tokens,
        string streamerUsername, HttpClient client)
    {
        ConcurrentDictionary<TokenItem, List<Tag>> result = new();
        foreach (var token in tokens)
        {
            result.TryAdd(token, []);
        }

        await Parallel.ForEachAsync(tokens, new ParallelOptions() { MaxDegreeOfParallelism = Threads },
            async (token, e) =>
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get,
                        $"https://kick.com/api/v2/channels/{streamerUsername}/me");
                    request.Headers.Add("Cookie",
                        $"kick_session={token.Token1}; {token.Token2}={token.Token3}");
                    foreach (var header in Headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }

                    var response = await client.SendAsync(request, e);
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                    var json = await response.Content.ReadFromJsonAsync<MeJson>(cancellationToken: e);
                    token.Tags.Clear();

                    if (json.Banned is not null)
                    {
                        result[token].Add(Tag.Ban);
                    }

                    Console.WriteLine(json.IsModerator);
                    if (json.IsModerator)
                    {
                        result[token].Add(Tag.Moderator);
                    }

                    if (json.Subscription is not null)
                    {
                        result[token].Add(Tag.Subscriber);
                    }

                    if (json.IsBroadcaster)
                    {
                        result[token].Add(Tag.Broadcaster);
                    }

                    response.Dispose();
                }
                catch
                {
                    // ignored
                }
            });
        return new Dictionary<TokenItem, List<Tag>>(result);
    }


    private struct UserJson
    {
        public string? Username { get; set; }
    }

    private struct MeJson
    {
        [JsonPropertyName("subscription")] public object? Subscription { get; set; }

        [JsonPropertyName("is_broadcaster")] public bool IsBroadcaster { get; set; }

        [JsonPropertyName("is_moderator")] public bool IsModerator { get; set; }

        [JsonPropertyName("banned")] public object? Banned { get; set; }
    }
}