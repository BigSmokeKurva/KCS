using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;
using KCS.Server.Controllers.Models;
using KCS.Server.Database.Models;

namespace KCS.Server.BotsManager
{
    public class Bot : IDisposable
    {
        private string Token1 { get; set; }
        private string Token2 { get; set; }
        private string Token3 { get; set; }
        private string Token4 { get; set; }
        private int ChatroomId { get; set; }
        private readonly HttpClient _client;

        public Bot(TokenItem token, int chatroomId) : this(token.Token1, token.Token2, token.Token3, token.Token4,
            chatroomId,
            token.Proxy)
        {
        }

        private Bot(string token1, string token2, string token3, string token4, int chatroomId, Proxy proxy)
        {
            Token1 = token1;
            Token2 = token2;
            Token3 = token3;
            Token4 = token4;
            ChatroomId = chatroomId;

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
        }

        private async Task Send(HttpContent content, CancellationToken? cancellationToken = null)
        {
            var response = await _client.PostAsync($"https://kick.com/api/v2/messages/send/{ChatroomId}", content,
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
}