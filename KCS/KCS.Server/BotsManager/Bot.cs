using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using KCS.Server.Controllers.Models;
using KCS.Server.Database.Models;

namespace KCS.Server.BotsManager;

public class Bot : IDisposable
{
    private string Token1 { get; set; }
    private string Token2 { get; set; }
    private string Token3 { get; set; }
    private string Token4 { get; set; }
    public string Username { get; set; }
    public StreamerInfo StreamerInfo { get; set; }
    private readonly HttpClient _client;

    public Bot(TokenItem token, StreamerInfo streamerInfo) : this(token.Token1, token.Token2,
        token.Token3, token.Token4, token.Username,
        streamerInfo,
        token.Proxy)
    {
    }

    private Bot(string token1, string token2, string token3, string token4, string username, StreamerInfo streamerInfo,
        Proxy proxy)
    {
        Token1 = token1;
        Token2 = token2;
        Token3 = token3;
        Token4 = token4;
        Username = username;
        StreamerInfo = streamerInfo;

        var options = new HttpClientHandler()
        {
            Proxy = (WebProxy)proxy,
        };

        _client = new HttpClient(options)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        _client.DefaultRequestHeaders.Add("x-xsrf-token", HttpUtility.UrlDecode(Token4));
        _client.DefaultRequestHeaders.Add("referer", "https://kick.com/");
        _client.DefaultRequestHeaders.Add("cookie", $"kick_session={Token1}; {Token2}={Token3}");
        _client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/118.0");
    }

    private async Task Send(HttpContent content, CancellationToken? cancellationToken = null)
    {
        var response = await _client.PostAsync($"https://kick.com/api/v2/messages/send/{StreamerInfo.ChatroomId}",
            content,
            cancellationToken ?? CancellationToken.None);
        if (!response.IsSuccessStatusCode ||
            !(await response.Content.ReadAsStringAsync()).Contains("\"error\":false"))
        {
            throw new Exception("Failed to send message");
        }
    }

    public async Task Send(SendMessageModel model)
    {
        var content = model.Metadata is null
            ? new StringContent(JsonSerializer.Serialize(new
            {
                content = model.Message,
                type = "message"
            }))
            : new StringContent(JsonSerializer.Serialize(new
            {
                content = model.Message,
                type = "reply",
                metadata = new
                {
                    original_message = new
                    {
                        id = model.Metadata?.OriginalMessage.Id,
                        content = model.Metadata?.OriginalMessage.Content
                    },
                    original_sender = new
                    {
                        id = model.Metadata?.OriginalSender.Id,
                        username = model.Metadata?.OriginalSender.Username
                    }
                }
            }));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        await Send(content);
    }

    public async Task Send(string message, CancellationToken? cancellationToken = null)
    {
        var content = new StringContent(JsonSerializer.Serialize(new
        {
            content = message,
            type = "message"
        }), Encoding.UTF8, "application/json");
        await Send(content, cancellationToken);
    }

    public async Task<bool> UnFollow()
    {
        try
        {
            if (!await IsFollowed())
            {
                return true;
            }

            var kasada = await Kasada.Solve();
            var requestMessage = new HttpRequestMessage(
                HttpMethod.Delete,
                $"https://kick.com/api/v2/channels/{StreamerInfo.Username}/follow"
            )
            {
                Headers =
                {
                    { "x-xsrf-token", HttpUtility.UrlDecode(Token4) },
                    { "Accept", "application/json, text/plain, */*" },
                    { "cookie", $"kick_session={Token1}; {Token2}={Token3}" },
                    { "User-Agent", kasada.Solution.UserAgent },
                    { "x-kpsdk-cd", kasada.Solution.XKpsdkCd },
                    { "x-kpsdk-ct", kasada.Solution.XKpsdkCt },
                    { "x-kpsdk-v", "j-0.0.0" },
                    { "Origin", "https://kick.com" }
                }
            };
            await _client.SendAsync(requestMessage);
            return !await IsFollowed();
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> Follow()
    {
        try
        {
            if (await IsFollowed())
            {
                return true;
            }

            var kasada = await Kasada.Solve(_client);
            var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://kick.com/api/v2/channels/{StreamerInfo.Username}/follow"
            )
            {
                Headers =
                {
                    { "x-xsrf-token", HttpUtility.UrlDecode(Token4) },
                    { "Accept", "application/json, text/plain, */*" },
                    { "cookie", $"kick_session={Token1}; {Token2}={Token3}" },
                    { "User-Agent", kasada.Solution.UserAgent },
                    { "x-kpsdk-cd", kasada.Solution.XKpsdkCd },
                    { "x-kpsdk-ct", kasada.Solution.XKpsdkCt },
                    { "x-kpsdk-v", "j-0.0.0" },
                    { "Origin", "https://kick.com" }
                }
            };
            var r = await _client.SendAsync(requestMessage);
            return await IsFollowed();
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> IsFollowed()
    {
        Exception? ex = null;
        for (var i = 0; i < 8; i++)
        {
            try
            {
                var response = await _client.GetAsync($"https://kick.com/api/v2/channels/{StreamerInfo.Username}/me");
                var json = await response.Content.ReadFromJsonAsync<GetMe>();
                return json.IsFollowing;
            }
            catch (Exception e)
            {
                ex = e;
            }

            await Task.Delay(1500);
        }

        throw ex!;
    }

    public void Dispose()
    {
        try
        {
            _client.Dispose();
        }
        catch
        {
            // ignored
        }
    }
}

public struct GetMe
{
    [JsonPropertyName("subscription")] public object Subscription { get; set; }

    [JsonPropertyName("is_super_admin")] public bool IsSuperAdmin { get; set; }

    [JsonPropertyName("is_following")] public bool IsFollowing { get; set; }

    [JsonPropertyName("following_since")] public object FollowingSince { get; set; }

    [JsonPropertyName("is_broadcaster")] public bool IsBroadcaster { get; set; }

    [JsonPropertyName("is_moderator")] public bool IsModerator { get; set; }

    [JsonPropertyName("banned")] public object Banned { get; set; }

    [JsonPropertyName("has_notifications")]
    public bool HasNotifications { get; set; }
}