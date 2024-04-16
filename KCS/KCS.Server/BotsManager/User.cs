using System.Collections.Concurrent;
using KCS.Server.Database;
using KCS.Server.Database.Models;

namespace KCS.Server.BotsManager
{
    public class User(int id, string streamerUsername)
    {
        public int id { get; set; } = id;
        private string streamerUsername = streamerUsername;
        public Dictionary<string, Bot> bots = [];
        public SpamMode spamMode = SpamMode.Random;
        internal List<Task> spamTasks = [];
        private CancellationTokenSource spamCancellationToken = null;
        private static readonly Random rnd = new();
        internal static int ConnectThreads;
        internal static int DisconnectThreads;
        internal async Task ConnectBot(string botname, DatabaseContext db)
        {
            if (bots.ContainsKey(botname))
            {
                return;
            }
            var configuration = await db.Configurations.FindAsync(id);
            if (!configuration.Tokens.Any(x => x.Username == botname))
            {
                throw new Exception("Токен не найден.");
            }
            var token = configuration.Tokens.First(x => x.Username == botname);
            var bot = new Bot(botname, token.Token, streamerUsername, token.Proxy);
            await bot.Connect();
            bots.Add(botname, bot);
        }
        internal async Task DisconnectBot(string botname)
        {
            if (!bots.TryGetValue(botname, out var bot))
                return;
            await bot.Disconnect();
            bots.Remove(botname);
        }
        internal async Task ConnectAllBots(DatabaseContext db)
        {
            var configuration = await db.Configurations.FindAsync(id);
            var bots = configuration.Tokens;
            if (bots.Count == this.bots.Count)
                return;
            ConcurrentDictionary<string, Bot> keyValuePairs = new();
            await Parallel.ForEachAsync(bots, new ParallelOptions() { MaxDegreeOfParallelism = ConnectThreads }, async (bot, e) =>
            {
                try
                {
                    if (this.bots.ContainsKey(bot.Username))
                        return;
                    var _bot = new Bot(bot.Username, bot.Token, streamerUsername, bot.Proxy);
                    await _bot.Connect();
                    keyValuePairs.TryAdd(bot.Username, _bot);
                }
                catch
                {
                }
            });
            foreach (var pair in keyValuePairs)
            {
                this.bots.Add(pair.Key, pair.Value);
            }
        }
        internal async Task DisconnectAllBots()
        {
            if (!bots.Any())
                return;
            await Parallel.ForEachAsync(bots, new ParallelOptions() { MaxDegreeOfParallelism = DisconnectThreads }, async (bot, e) =>
            {
                try
                {
                    await bot.Value.Disconnect();
                }
                catch { }
            });

            bots.Clear();
        }
        internal async Task Send(string botname, string message, string replyTo)
        {
            if (!bots.TryGetValue(botname, out var bot))
                return;
            await bot.Send(message, replyTo);
        }
        internal bool SpamStarted()
        {
            return spamTasks.Any();
        }
        internal async Task StopSpam()
        {
            spamCancellationToken?.Cancel();
            // ждем пока все потоки остановятся
            while (spamTasks.Any(x => x.Status == TaskStatus.Running))
            {
                await Task.Delay(200);
            }
            spamCancellationToken?.Dispose();
            spamTasks.Clear();
            spamCancellationToken = null;
        }
        internal async Task StartSpam(int threads, int delay, string[] messages, SpamMode mode)
        {
            spamCancellationToken = new();
            spamMode = mode;
            if (mode == SpamMode.Random)
            {
                for (int i = 0; i < threads; i++)
                {
                    spamTasks.Add(SpamThread(delay, messages));
                }
            }
            else
            {
                spamTasks.Add(SpamThreadModeList(bots.Values.Take(threads).ToArray(), delay, [.. messages]));
            }
        }
        internal async Task SpamThread(int delay, string[] messages)
        {
            delay *= 1000;
            while (spamCancellationToken is not null && !spamCancellationToken.IsCancellationRequested)
            {
                if (!bots.Any())
                {
                    await Task.Delay(delay, spamCancellationToken.Token);
                }
                try
                {
                    var bot = bots.Values.ElementAt(rnd.Next(0, bots.Count));
                    await bot.Send(messages[rnd.Next(0, messages.Length)], null, spamCancellationToken);
                    await Task.Delay(delay, spamCancellationToken.Token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
                catch { }
            }
        }
        internal async Task SpamThreadModeList(Bot[] bots, int delay, List<string> messages)
        {
            delay *= 1000;
            while (spamCancellationToken is not null && !spamCancellationToken.IsCancellationRequested && messages.Any())
            {
                foreach (var bot in bots)
                {
                    try
                    {
                        await bot.Send(messages.First(), null, spamCancellationToken);
                        messages.RemoveAt(0);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                    catch { }
                    await Task.Delay(delay, spamCancellationToken.Token);
                }
            }
            spamCancellationToken?.Dispose();
            spamTasks.Clear();
            spamCancellationToken = null;
        }
        internal async Task ChangeStreamerUsername(string streamerUsername)
        {
            if (SpamStarted())
            {
                await StopSpam();
            }
            if (bots.Any())
            {
                await DisconnectAllBots();
            }
            bots.Clear();
            this.streamerUsername = streamerUsername;
        }
        internal async Task Remove(DatabaseContext db)
        {

            if (SpamStarted())
            {
                await db.AddLog(id, "Остановил спам. (Бездействие)", Database.Models.LogType.Action);
                await StopSpam();
            }
            if (bots.Any())
            {
                await db.AddLog(id, "Отключил всех ботов. (Бездействие)", Database.Models.LogType.Action);
                await DisconnectAllBots();
            }
            await db.SaveChangesAsync();
            Manager.users.Remove(id);
        }
    }

}
