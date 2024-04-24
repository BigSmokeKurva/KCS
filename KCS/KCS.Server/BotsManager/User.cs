using System.Collections.Concurrent;
using KCS.Server.Controllers.Models;
using KCS.Server.Database.Models;

namespace KCS.Server.BotsManager;

public class User(int id, StreamerInfo streamerInfo)
{
    private static readonly Random Rnd = new();
    private readonly List<Task> _spamTasks = [];
    public readonly Dictionary<string, Bot> Bots = [];
    private CancellationTokenSource? _spamCancellationToken;
    public readonly ConcurrentQueue<Bot> FollowBotQueue = [];
    public int Id { get; set; } = id;

    internal void ConnectBot(string botName, Configuration configuration)
    {
        if (Bots.ContainsKey(botName)) return;

        var token = configuration.Tokens.FirstOrDefault(x => x?.Username == botName, null);
        _ = token ?? throw new Exception("Токен не найден");

        var bot = new Bot(token, streamerInfo);
        Bots.Add(botName, bot);
    }

    internal void DisconnectBot(string botName)
    {
        try
        {
            Bots[botName].Dispose();
        }
        catch
        {
            // ignored
        }

        Bots.Remove(botName);
    }

    internal void ConnectAllBots(Configuration configuration)
    {
        var bots = configuration.Tokens;
        if (bots.Count == Bots.Count)
            return;
        foreach (var bot in (bots ?? throw new InvalidOperationException()).Where(bot =>
                     !Bots.ContainsKey(bot.Username)))
            Bots.Add(bot.Username, new Bot(bot, streamerInfo));
    }

    internal void DisconnectAllBots()
    {
        if (Bots.Count == 0)
            return;

        foreach (var bot in Bots)
            try
            {
                bot.Value.Dispose();
            }
            catch
            {
                // ignored
            }

        Bots.Clear();
    }

    internal async Task Send(SendMessageModel model)
    {
        if (!Bots.TryGetValue(model.BotName, out var bot))
            return;

        await bot.Send(model);
    }

    internal async Task Send(string botName, string message)
    {
        if (!Bots.TryGetValue(botName, out var bot))
            return;

        await bot.Send(message);
    }

    internal bool SpamStarted()
    {
        return _spamTasks.Count != 0;
    }

    internal async Task StopSpam()
    {
        await _spamCancellationToken!.CancelAsync();
        // ждем пока все потоки остановятся
        while (_spamTasks.Any(x => x.Status == TaskStatus.Running)) await Task.Delay(200);

        _spamCancellationToken?.Dispose();
        _spamTasks.Clear();
        _spamCancellationToken = null;
    }

    internal void StartSpam(int threads, int delay, string[] messages, SpamMode mode)
    {
        _spamCancellationToken = new CancellationTokenSource();
        if (mode == SpamMode.Random)
            for (var i = 0; i < threads; i++)
                _spamTasks.Add(SpamThread(delay, messages));
        else
            _spamTasks.Add(SpamThreadModeList(Bots.Values.Take(threads).ToArray(), delay,
                [.. messages]));
    }

    private async Task SpamThread(int delay, IReadOnlyList<string> messages)
    {
        delay *= 1000;
        while (_spamCancellationToken is not null && !_spamCancellationToken.IsCancellationRequested)
        {
            if (Bots.Count == 0) await Task.Delay(delay, _spamCancellationToken.Token);

            try
            {
                var bot = Bots.Values.ElementAt(Rnd.Next(0, Bots.Count));
                await bot.Send(messages[Rnd.Next(0, messages.Count)], _spamCancellationToken.Token);
                await Task.Delay(delay, _spamCancellationToken.Token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            catch
            {
                // ignored
            }
        }
    }

    private async Task SpamThreadModeList(Bot[] bots, int delay, IList<string> messages)
    {
        delay *= 1000;
        while (_spamCancellationToken is not null && !_spamCancellationToken.IsCancellationRequested &&
               messages.Count > 0)
            foreach (var bot in bots)
            {
                try
                {
                    await bot.Send(messages.First(), _spamCancellationToken.Token);
                    messages.RemoveAt(0);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
                catch
                {
                    // ignored
                }

                await Task.Delay(delay, _spamCancellationToken.Token);
            }

        _spamCancellationToken?.Dispose();
        _spamTasks.Clear();
        _spamCancellationToken = null;
    }

    internal async Task ChangeStreamerUsername(StreamerInfo newStreamerInfo)
    {
        if (SpamStarted()) await StopSpam();

        if (Bots.Count > 0) DisconnectAllBots();

        Bots.Clear();
        streamerInfo = newStreamerInfo;
    }
}

public enum FollowMode
{
    Follow,
    UnFollow
}