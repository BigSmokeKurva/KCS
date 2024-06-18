using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using KCS.Server.Controllers.Models;
using KCS.Server.Database.Models;
using KCS.Server.Services;

namespace KCS.Server.BotsManager;

public class Bot : IDisposable
{
    private string Token1 { get; set; }
    private string Token2 { get; set; }
    private string Token3 { get; set; }
    private string Token4 { get; set; }
    public string Username { get; set; }
    private Proxy Proxy { get; set; }
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
        Proxy = proxy;

        _client = new HttpClient(new HttpClientHandler()
        {
            Proxy = (WebProxy)proxy,
            Credentials = proxy.Credentials,
            SslProtocols = SslProtocols.Tls13
        });
    }

    private async Task Send(HttpContent content, string message, CancellationToken? cancellationToken = null)
    {
        for (int i = 0; i < 8; i++)
        {
            using var connection = new ClientWebSocket();
            await connection.ConnectAsync(
                new Uri(
                    "wss://ws-us2.pusher.com/app/eb1d5f283081a78b932c?protocol=7&client=js&version=7.6.0&flash=false"),
                cancellationToken ?? CancellationToken.None);
            await connection.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(
                    "{\"event\":\"pusher:subscribe\",\"data\":{\"auth\":\"\",\"channel\":\"chatrooms." +
                    StreamerInfo.ChatroomId.Value + ".v2\"}}")),
                WebSocketMessageType.Text, true, cancellationToken ?? CancellationToken.None);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post,
                $"https://kick.com/api/v2/messages/send/{StreamerInfo.ChatroomId}")
            {
                Content = content,
                Headers =
                {
                    { "x-xsrf-token", HttpUtility.UrlDecode(Token4) },
                    {
                        "cookie",
                        $"kick_session={Token1}; {Token2}={Token3}"
                    },
                    { "referer", "https://kick.com" },
                    { "User-Agent", CloudflareBackgroundSolverService.UserAgent }
                }
            };
            var response = await _client.SendAsync(requestMessage,
                cancellationToken: cancellationToken ?? CancellationToken.None);
            var sentMessage = await response.Content.ReadFromJsonAsync<Message>();
            if (sentMessage.Status.Error)
            {
                throw new Exception(sentMessage.Status.Message);
            }


            var buffer = new byte[2048];
            var cancellationTokenSource = new CancellationTokenSource(3000);
            try
            {
                while ((!cancellationToken?.IsCancellationRequested) ?? true)
                {
                    var received =
                        await connection.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationTokenSource.Token);
                    if (received.MessageType == WebSocketMessageType.Close)
                        throw new Exception("Connection is not open");
                    var data = Encoding.UTF8.GetString(buffer, 0, received.Count);
                    if (data.Contains(sentMessage.Data.Id))
                    {
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
            }
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
        await Send(content, model.Message);
    }

    public async Task Send(string message, CancellationToken? cancellationToken = null)
    {
        var content = new StringContent(JsonSerializer.Serialize(new
        {
            content = message,
            type = "message"
        }), Encoding.UTF8, "application/json");
        await Send(content, message, cancellationToken);
    }

    public async Task<bool> UnFollow()
    {
        try
        {
            if (!await IsFollowed())
            {
                return true;
            }

            for (int i = 0; i < 3; i++)
            {
                var kasada = await Kasada.Solve();
                string cfClearance =
                    await CloudflareBackgroundSolverService.SolveCfClearance(CancellationToken.None, Proxy);
                var requestMessage = new HttpRequestMessage(
                    HttpMethod.Delete,
                    $"https://kick.com/api/v2/channels/{StreamerInfo.Username}/follow"
                )
                {
                    Headers =
                    {
                        { "x-xsrf-token", HttpUtility.UrlDecode(Token4) },
                        { "Accept", "application/json, text/plain, */*" },
                        {
                            "cookie",
                            $"kick_session={Token1}; {Token2}={Token3}; cf_clearance={cfClearance}"
                        },
                        { "User-Agent", kasada.Solution.UserAgent },
                        { "x-kpsdk-cd", kasada.Solution.XKpsdkCd },
                        { "x-kpsdk-ct", kasada.Solution.XKpsdkCt },
                        { "x-kpsdk-v", "j-0.0.0" },
                        { "referer", "https://kick.com" }
                    }
                };
                var response = await _client.SendAsync(requestMessage);
                var content = await response.Content.ReadAsStringAsync();
                if (content.Contains("Just a moment"))
                {
                    await Task.Delay(1000);
                    continue;
                }

                if (!await IsFollowed())
                    return true;
                await Task.Delay(1000);
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    public async Task<bool> Follow()
    {
        try
        {
            if (await IsFollowed())
            {
                return true;
            }

            for (var i = 0; i < 3; i++)
            {
                var kasada = await Kasada.Solve(_client);
                string cfClearance =
                    await CloudflareBackgroundSolverService.SolveCfClearance(CancellationToken.None, Proxy);
                var requestMessage = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"https://kick.com/api/v2/channels/{StreamerInfo.Username}/follow"
                )
                {
                    Headers =
                    {
                        { "x-xsrf-token", HttpUtility.UrlDecode(Token4) },
                        { "Accept", "application/json, text/plain, */*" },
                        {
                            "cookie",
                            $"kick_session={Token1}; {Token2}={Token3}; cf_clearance={cfClearance}"
                        },
                        { "User-Agent", kasada.Solution.UserAgent },
                        { "x-kpsdk-cd", kasada.Solution.XKpsdkCd },
                        { "x-kpsdk-ct", kasada.Solution.XKpsdkCt },
                        { "x-kpsdk-v", "j-0.0.0" },
                        { "referer", "https://kick.com" }
                    }
                };
                var r = await _client.SendAsync(requestMessage);
                var content = await r.Content.ReadAsStringAsync();
                if (content.Contains("Just a moment"))
                {
                    await Task.Delay(1000);
                    continue;
                }

                if (await IsFollowed()) return true;
                await Task.Delay(1000);
            }
        }
        catch (Exception ex)
        {
            return false;
        }

        return false;
    }

    private async Task<bool> IsFollowed()
    {
        for (int i = 0; i < 3; i++)
        {
            string cfClearance =
                await CloudflareBackgroundSolverService.SolveCfClearance(CancellationToken.None, Proxy);
            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://kick.com/api/v2/channels/{StreamerInfo.Username}/me")
            {
                Headers =
                {
                    {
                        "cookie",
                        $"kick_session={Token1}; {Token2}={Token3}; cf_clearance={cfClearance}"
                    },
                    { "referer", "https://kick.com" },
                    { "Accept", "application/json, text/plain, */*" },
                    { "x-xsrf-token", HttpUtility.UrlDecode(Token4) },
                    { "User-Agent", CloudflareBackgroundSolverService.UserAgent }
                }
            };
            var response = await _client.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();
            if (content.Contains("Just a moment"))
            {
                await Task.Delay(1000);
                continue;
            }

            var json = await response.Content.ReadFromJsonAsync<GetMe>();
            return json.IsFollowing;
        }

        return false;
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

public class Messages
{
    public class Badge
    {
        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("text")] public string Text { get; set; }

        [JsonPropertyName("active")] public bool Active { get; set; }
    }

    public class _Data
    {
        [JsonPropertyName("messages")] public List<Message> Messages { get; set; }

        [JsonPropertyName("cursor")] public string Cursor { get; set; }

        [JsonPropertyName("pinned_message")] public object PinnedMessage { get; set; }
    }

    public class Identity
    {
        [JsonPropertyName("color")] public string Color { get; set; }

        [JsonPropertyName("badges")] public List<Badge> Badges { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("id")] public string Id { get; set; }

        [JsonPropertyName("chat_id")] public int ChatId { get; set; }

        [JsonPropertyName("user_id")] public int UserId { get; set; }

        [JsonPropertyName("content")] public string Content { get; set; }

        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("metadata")] public object Metadata { get; set; }

        [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }

        [JsonPropertyName("sender")] public Sender Sender { get; set; }
    }

    public class Sender
    {
        [JsonPropertyName("id")] public int Id { get; set; }

        [JsonPropertyName("slug")] public string Slug { get; set; }

        [JsonPropertyName("username")] public string Username { get; set; }

        [JsonPropertyName("identity")] public Identity Identity { get; set; }
    }

    public class _Status
    {
        [JsonPropertyName("error")] public bool Error { get; set; }

        [JsonPropertyName("code")] public int Code { get; set; }

        [JsonPropertyName("message")] public string Message { get; set; }
    }

    [JsonPropertyName("status")] public _Status Status { get; set; }

    [JsonPropertyName("data")] public _Data Data { get; set; }
}

public class Message
{
    public class Sender
    {
        [JsonPropertyName("id")] public int Id { get; set; }

        [JsonPropertyName("username")] public string Username { get; set; }

        [JsonPropertyName("slug")] public string Slug { get; set; }

        [JsonPropertyName("identity")] public Identity Identity { get; set; }
    }

    public class _Status
    {
        [JsonPropertyName("error")] public bool Error { get; set; }

        [JsonPropertyName("code")] public int Code { get; set; }

        [JsonPropertyName("message")] public string Message { get; set; }
    }

    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class _Data
    {
        [JsonPropertyName("id")] public string Id { get; set; }

        [JsonPropertyName("chatroom_id")] public int ChatroomId { get; set; }

        [JsonPropertyName("content")] public string Content { get; set; }

        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }

        [JsonPropertyName("sender")] public Sender Sender { get; set; }
    }

    public class Identity
    {
        [JsonPropertyName("color")] public string Color { get; set; }

        [JsonPropertyName("badges")] public List<object> Badges { get; set; }
    }

    [JsonPropertyName("status")] public _Status Status { get; set; }

    [JsonPropertyName("data")] public _Data Data { get; set; }
}