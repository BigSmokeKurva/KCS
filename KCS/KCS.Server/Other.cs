using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using KCS.Server.Database.Models;
using KCS.Server.Services;

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
                        $"kick_session={tokensCouple.Item1}; {tokensCouple.Item2}={tokensCouple.Item3}; cf_clearance={CloudflareBackgroundSolverService.CfClearance}");
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
                        $"kick_session={token.Token1}; {token.Token2}={token.Token3}; cf_clearance={CloudflareBackgroundSolverService.CfClearance}");
                    foreach (var header in Headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }

                    var response = await client.SendAsync(request, e);
                    var json = await response.Content.ReadFromJsonAsync<MeJson>(cancellationToken: e);
                    token.Tags.Clear();

                    if (json.Banned is not null)
                    {
                        result[token].Add(Tag.Ban);
                    }

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

public static class Kasada
{
    public static string ApiKey = null!;
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private static async Task<SalamoonderResultTaskResponse> Solve(HttpClient client)
    {
        var data = new
        {
            api_key = ApiKey,
            task = new
            {
                type = "KasadaCaptchaSolver",
                pjs =
                    "https://kick.com/149e9513-01fa-4fb0-aad4-566afd725d1b/2d206a39-8ed7-437e-a3be-862e0f06eea3/p.js",
                cdOnly = "false"
            }
        };
        var response = await client.PostAsJsonAsync("https://salamoonder.com/api/createTask", data);
        var task = await response.Content.ReadFromJsonAsync<SalamoonderCreateTaskResponse>();
        if (task.ErrorCode != 0) throw new Exception(task.ErrorDescription);
        for (var i = 0; i < 30; i++)
        {
            await Task.Delay(1000);
            response = await client.PostAsJsonAsync("https://salamoonder.com/api/getTaskResult", new
            {
                taskId = task.TaskId
            });
            var result = await response.Content.ReadFromJsonAsync<SalamoonderResultTaskResponse>();

            if (result.Status != "ready") continue;
            if (result.Solution.Error is null) return result;
            if (result.Solution.Error is not null &&
                result.Solution.Error == "No solution created. (Refunded task automatically) XXX")
            {
                return await Solve(client);
            }

            throw new Exception(result.Solution.Error);
        }


        throw new Exception("Timeout");
    }

    public static async Task<SalamoonderResultTaskResponse> Solve(object? nothing = null)
    {
        await using var scope = ServiceProviderAccessor.ServiceProvider.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<HttpClient>();
        Exception? exception = null;
        SalamoonderResultTaskResponse result = default;
        await Semaphore.WaitAsync();
        try
        {
            result = await Solve(client);
            await Task.Delay(1000);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        CloudflareBackgroundSolverService.UserAgent = result.Solution.UserAgent!;
        Semaphore.Release();
        if (exception is not null) throw exception;
        return result;
    }

    private struct SalamoonderCreateTaskResponse
    {
        [JsonPropertyName("taskId")] public string TaskId { get; set; }
        [JsonPropertyName("error_code")] public int ErrorCode { get; set; }

        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; }
    }

    public struct SalamoonderResultTaskResponse
    {
        [JsonPropertyName("errorId")] public int ErrorId { get; set; }

        [JsonPropertyName("solution")] public Solution Solution { get; set; }

        [JsonPropertyName("status")] public string Status { get; set; }
    }

    public struct Solution
    {
        [JsonPropertyName("user-agent")] public string? UserAgent { get; set; }

        [JsonPropertyName("x-kpsdk-cd")] public string? XKpsdkCd { get; set; }

        [JsonPropertyName("x-kpsdk-cr")] public string? XKpsdkCr { get; set; }

        [JsonPropertyName("x-kpsdk-ct")] public string? XKpsdkCt { get; set; }

        [JsonPropertyName("x-kpsdk-r")] public string? XKpsdkR { get; set; }

        [JsonPropertyName("x-kpsdk-st")] public string? XKpsdkSt { get; set; }
        [JsonPropertyName("error")] public string? Error { get; set; }
    }
}