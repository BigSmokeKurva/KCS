
using Microsoft.EntityFrameworkCore;
using KCS.Server.BotsManager;
using KCS.Server.Database;

namespace KCS.Server.Services
{
    public class LastOnlineCheckService(IServiceProvider _serviceProvider) : IHostedService, IDisposable
    {
        private Timer _timer = null;
        private readonly IServiceProvider _serviceProvider = _serviceProvider;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(CheckLastOnline, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        private async void CheckLastOnline(object state)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var users = Manager.users.Where(x => x.Value.bots.Any());
            var now = TimeHelper.GetUnspecifiedUtc();
            foreach (var user in users)
            {
                if (!((now - await context.Users.Where(x => x.Id == user.Key).Select(x => x.LastOnline).FirstAsync()) > TimeSpan.FromMinutes(10)))
                    continue;
                if (user.Value.spamTasks.Any())
                {
                    await user.Value.StopSpam();
                    await context.AddLog(user.Key, "Остановил спам. (Бездействие)", Database.Models.LogType.Action);
                }
                if (user.Value.bots.Any())
                {
                    await user.Value.DisconnectAllBots();
                    await context.AddLog(user.Key, "Отключил всех ботов. (Бездействие)", Database.Models.LogType.Action);
                }
                Manager.users.Remove(user.Key);
            }
            await context.SaveChangesAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
