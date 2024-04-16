using Microsoft.EntityFrameworkCore;
using KCS.Server.Database;
using KCS.Server.Database.Models;

namespace KCS.Server.BotsManager
{
    public class Manager
    {
        public static Dictionary<int, User> users = new();
        public static bool IsConnected(int id, string botUsername)
        {
            if (!users.TryGetValue(id, out var user))
                return false;

            return user.bots.ContainsKey(botUsername);
        }
        public async static Task ConnectBot(int id, string botUsername, DatabaseContext db)
        {
            if (!users.TryGetValue(id, out var user))
            {

                var streamerUsername = await db.Configurations.Where(x => x.Id == id).Select(x => x.StreamerUsername).FirstAsync();
                users.Add(id, new User(id, streamerUsername));
                await users[id].ConnectBot(botUsername, db);
                return;
            }

            await user.ConnectBot(botUsername, db);
        }
        public async static Task DisconnectBot(int id, string botUsername, DatabaseContext db)
        {
            if (!users.TryGetValue(id, out var user))
            {

                var streamerUsername = await db.Configurations.Where(x => x.Id == id).Select(x => x.StreamerUsername).FirstAsync();
                users.Add(id, new User(id, streamerUsername));
                return;
            }

            await user.DisconnectBot(botUsername);
        }
        public async static Task ConnectAllBots(int id, DatabaseContext db)
        {
            if (!users.TryGetValue(id, out var user))
            {

                var streamerUsername = await db.Configurations.Where(x => x.Id == id).Select(x => x.StreamerUsername).FirstAsync();
                users.Add(id, new User(id, streamerUsername));
                await users[id].ConnectAllBots(db);
                return;
            }

            await user.ConnectAllBots(db);
        }
        public async static Task DisconnectAllBots(int id, DatabaseContext db)
        {
            if (!users.TryGetValue(id, out var user))
            {

                var streamerUsername = await db.Configurations.Where(x => x.Id == id).Select(x => x.StreamerUsername).FirstAsync();
                users.Add(id, new User(id, streamerUsername));
                return;
            }

            await user.DisconnectAllBots();
        }
        public async static Task<bool> Send(int id, string botUsername, string message, string replyTo, DatabaseContext db)
        {
            if (!users.TryGetValue(id, out var user))
            {

                var streamerUsername = await db.Configurations.Where(x => x.Id == id).Select(x => x.StreamerUsername).FirstAsync();
                users.Add(id, new User(id, streamerUsername));
                return false;
            }

            try
            {
                await user.Send(botUsername, message, replyTo);
                return true;
            }
            catch { }
            return false;
        }
        public static async Task<bool> SpamStarted(int id, DatabaseContext db)
        {
            if (!users.TryGetValue(id, out var user))
            {

                var streamerUsername = await db.Configurations.Where(x => x.Id == id).Select(x => x.StreamerUsername).FirstAsync();
                users.Add(id, new User(id, streamerUsername));
                return false;
            }
            return user.SpamStarted();
        }
        public static async Task StopSpam(int id, DatabaseContext db)
        {
            if (!users.TryGetValue(id, out var user))
            {

                var streamerUsername = await db.Configurations.Where(x => x.Id == id).Select(x => x.StreamerUsername).FirstAsync();
                users.Add(id, new User(id, streamerUsername));
                return;
            }
            await user.StopSpam();
        }
        public static async Task StartSpam(int id, int threads, int delay, string[] messages, SpamMode mode, DatabaseContext db)
        {
            if (!users.TryGetValue(id, out var user))
            {

                var streamerUsername = await db.Configurations.Where(x => x.Id == id).Select(x => x.StreamerUsername).FirstAsync();
                users.Add(id, new User(id, streamerUsername));
                user = users[id];
                await user.StartSpam(threads, delay, messages, mode);
                return;
            }
            await user.StartSpam(threads, delay, messages, mode);
        }
        public static async Task ChangeStreamerUsername(int id, string streamerUsername)
        {
            if (!users.TryGetValue(id, out var user))
            {
                users.Add(id, new User(id, streamerUsername));
                return;
            }
            await user.ChangeStreamerUsername(streamerUsername);
        }
        public static async Task Remove(int id, DatabaseContext db)
        {
            if (!users.TryGetValue(id, out var user))
            {
                return;
            }
            await user.Remove(db);

        }
    }
}
