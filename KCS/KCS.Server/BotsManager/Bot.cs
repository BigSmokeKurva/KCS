using System.Net;
using System.Net.WebSockets;
using System.Text;
using KCS.Server.Database.Models;
using Timer = System.Timers.Timer;

namespace KCS.Server.BotsManager
{
    public class Bot(string username, string token, string streamerUsername, Proxy proxy)
    {

        public string username { get; set; } = username;
        public string token { get; set; } = token;
        public string streamerUsername { get; set; } = streamerUsername;
        public ClientWebSocket connection { get; set; } = null;
        public Proxy proxy = proxy;
        public Timer pingTimer = null;

        public async Task Connect()
        {
            await Disconnect();
            connection = new();
            connection.Options.Proxy = new WebProxy()
            {
                Address = new Uri($"{proxy.Type}://{proxy.Host}:{proxy.Port}"),
                Credentials = proxy.Credentials
            };
            try
            {
                await connection.ConnectAsync(new Uri($"wss://irc-ws.chat.twitch.tv:443"), new CancellationTokenSource(5000).Token);
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("CAP REQ :twitch.tv/tags twitch.tv/commands")), WebSocketMessageType.Text, true, new CancellationTokenSource(5000).Token);
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("PASS oauth:" + token)), WebSocketMessageType.Text, true, new CancellationTokenSource(5000).Token);
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"NICK {username}")), WebSocketMessageType.Text, true, new CancellationTokenSource(5000).Token);
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"USER {username} 8 *:{username}")), WebSocketMessageType.Text, true, new CancellationTokenSource(5000).Token);
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"JOIN #{streamerUsername}")), WebSocketMessageType.Text, true, new CancellationTokenSource(5000).Token);
                // запуск таймера в фоне
                if (pingTimer is null)
                {
                    pingTimer = new Timer(15000);
                    pingTimer.Elapsed += async (sender, args) =>
                    {
                        try
                        {
                            await PingPong(new CancellationTokenSource(6000).Token);
                        }
                        catch (Exception ex)
                        {
                            if (pingTimer is null)
                                return;
                            await Connect();
                        }
                    };
                    pingTimer.Start();
                }
            }
            catch
            {
                await Disconnect();
            }
        }
        public async Task Disconnect()
        {
            if (pingTimer != null)
            {
                try
                {
                    pingTimer?.Stop();
                }
                catch { }
                try
                {
                    pingTimer?.Dispose();
                }
                catch { }
                try
                {
                    pingTimer = null;
                }
                catch { }
            }

            if (connection != null)
            {
                try
                {
                    await connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                catch { }
                try
                {
                    connection.Dispose();
                }
                catch { }
                try
                {
                    connection = null;
                }
                catch { }
            }
        }

        private async Task PingPong(CancellationToken cancellationToken)
        {
            if (connection.State != WebSocketState.Open)
            {
                throw new Exception("Connection is not open");
            }
            await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("PING")), WebSocketMessageType.Text, true, cancellationToken);
            var buffer = new byte[1024];
            while (!cancellationToken.IsCancellationRequested)
            {
                var received = await connection.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (received.MessageType == WebSocketMessageType.Close)
                {
                    throw new Exception("Connection is not open");
                }
                var message = Encoding.UTF8.GetString(buffer, 0, received.Count);
                if (message.Contains("PING"))
                {
                    await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("PONG")), WebSocketMessageType.Text, true, cancellationToken);
                    break;
                }
                else if (message.Contains("PONG"))
                {
                    break;
                }
            }
        }
        public async Task Send(string message, string replyTo, CancellationTokenSource? ctoken = null)
        {
            ctoken ??= new CancellationTokenSource(5000);
            try
            {
                if (connection is null || connection.State != WebSocketState.Open)
                {
                    throw new Exception("Connection is not open");
                }
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"{(replyTo is not null ? $"@reply-parent-msg-id={replyTo} " : string.Empty)}PRIVMSG #{streamerUsername} :{message}")), WebSocketMessageType.Text, true, ctoken.Token);
            }
            catch (Exception ex)
            {
                await Connect();
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"{(replyTo is not null ? $"@reply-parent-msg-id={replyTo} " : string.Empty)}PRIVMSG #{streamerUsername} :{message}")), WebSocketMessageType.Text, true, ctoken.Token);
            }
        }
    }

}
