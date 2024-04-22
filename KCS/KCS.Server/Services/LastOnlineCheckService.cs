using KCS.Server.BotsManager;
using KCS.Server.Database;
using KCS.Server.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace KCS.Server.Services;

public class LastOnlineCheckService(IServiceProvider serviceProvider) : IHostedService, IDisposable
{
    private Timer? _timer;

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(CheckLastOnline, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async void CheckLastOnline(object? state)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var users = Manager.Users.Where(x => x.Value.Bots.Any());
        var now = TimeHelper.GetUnspecifiedUtc();
        foreach (var (id, user) in users)
        {
            if (!(now - await context.Users.Where(x => x.Id == id).Select(x => x.LastOnline).FirstAsync() >
                  TimeSpan.FromMinutes(10)))
                continue;
            if (user.SpamStarted())
            {
                await user.StopSpam();
                await context.AddLog(id, "Остановил спам. (Бездействие)", LogType.Action);
            }

            if (user.Bots.Count > 0)
            {
                user.DisconnectAllBots();
                await context.AddLog(id, "Отключил всех ботов. (Бездействие)", LogType.Action);
            }

            Manager.Users.Remove(id);
        }

        await context.SaveChangesAsync();
    }
}