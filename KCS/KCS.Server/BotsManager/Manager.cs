using System.Collections.Concurrent;
using KCS.Server.Controllers.Models;
using Microsoft.EntityFrameworkCore;
using KCS.Server.Database;
using KCS.Server.Database.Models;

namespace KCS.Server.BotsManager
{
    public class Manager(DatabaseContext db)
    {
        public static readonly ConcurrentDictionary<int, User> Users = [];

        public bool IsConnected(int id, string botUsername)
        {
            return Users.TryGetValue(id, out var user) && user.Bots.ContainsKey(botUsername);
        }

        public async Task ConnectBot(int id, string botUsername)
        {
            var configuration = await db.Configurations.FindAsync(id);
            if (!Users.TryGetValue(id, out var user))
            {
                Users.TryAdd(id, new User(id, configuration!.StreamerInfo));
                user = Users[id];
            }

            user.ConnectBot(botUsername, configuration!);
        }

        public async Task DisconnectBot(int id, string botUsername)
        {
            if (!Users.TryGetValue(id, out var user))
            {
                var streamerInfo = await db.Configurations.Where(x => x.Id == id)
                    .Select(x => x.StreamerInfo).FirstAsync();
                Users.TryAdd(id, new User(id, streamerInfo));
                return;
            }

            user.DisconnectBot(botUsername);
        }

        public async Task ConnectAllBots(int id)
        {
            var configuration = await db.Configurations.FindAsync(id);
            if (!Users.TryGetValue(id, out var user))
            {
                Users.TryAdd(id, new User(id, configuration!.StreamerInfo));
                user = Users[id];
            }

            user.ConnectAllBots(configuration!);
        }

        public async Task DisconnectAllBots(int id)
        {
            if (!Users.TryGetValue(id, out var user))
            {
                var streamerInfo = await db.Configurations.Where(x => x.Id == id)
                    .Select(x => x.StreamerInfo).FirstAsync();
                Users.TryAdd(id, new User(id, streamerInfo));
                return;
            }

            user.DisconnectAllBots();
        }

        public async Task Send(int id, SendMessageModel model)
        {
            if (!Users.TryGetValue(id, out var user))
            {
                var streamerInfo = await db.Configurations.Where(x => x.Id == id)
                    .Select(x => x.StreamerInfo).FirstAsync();
                Users.TryAdd(id, new User(id, streamerInfo));
                throw new Exception("Ошибка отправки сообщения");
            }

            await user.Send(model);
        }

        public async Task Send(int id, string message, string botName)
        {
            if (!Users.TryGetValue(id, out var user))
            {
                var streamerInfo = await db.Configurations.Where(x => x.Id == id)
                    .Select(x => x.StreamerInfo).FirstAsync();
                Users.TryAdd(id, new User(id, streamerInfo));
                throw new Exception("Ошибка отправки сообщения");
            }

            await user.Send(botName, message);
        }

        public async Task<bool> SpamStarted(int id)
        {
            if (Users.TryGetValue(id, out var user)) return user.SpamStarted();
            var streamerInfo = await db.Configurations.Where(x => x.Id == id).Select(x => x.StreamerInfo)
                .FirstAsync();
            Users.TryAdd(id, new User(id, streamerInfo));
            return false;
        }

        public async Task StopSpam(int id)
        {
            if (!Users.TryGetValue(id, out var user))
            {
                var streamerInfo = await db.Configurations.Where(x => x.Id == id)
                    .Select(x => x.StreamerInfo).FirstAsync();
                Users.TryAdd(id, new User(id, streamerInfo));
                return;
            }

            await user.StopSpam();
        }

        public async Task StartSpam(int id, int threads, int delay, string[] messages, SpamMode mode)
        {
            if (!Users.TryGetValue(id, out var user))
            {
                var streamerInfo = await db.Configurations.Where(x => x.Id == id)
                    .Select(x => x.StreamerInfo).FirstAsync();
                Users.TryAdd(id, new User(id, streamerInfo));
                user = Users[id];
            }

            user.StartSpam(threads, delay, messages, mode);
        }

        public async Task ChangeStreamerUsername(int id, StreamerInfo streamerInfo)
        {
            if (!Users.TryGetValue(id, out var user))
            {
                Users.TryAdd(id, new User(id, streamerInfo));
                return;
            }

            await user.ChangeStreamerUsername(streamerInfo);
        }

        public async Task Remove(int id)
        {
            if (!Users.TryGetValue(id, out var user))
            {
                return;
            }

            if (user.SpamStarted())
            {
                await user.StopSpam();
                await db.AddLog(user.Id, "Остановил спам. (Бездействие)", LogType.Action);
            }


            if (user.Bots.Count != 0)
            {
                user.DisconnectAllBots();
                await db.AddLog(user.Id, "Отключил всех ботов. (Бездействие)", LogType.Action);
            }

            Users.Remove(id, out _);
        }
    }
}