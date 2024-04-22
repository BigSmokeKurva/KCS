using KCS.Server.Database;
using KCS.Server.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace KCS.Server.Services;

public class InviteCodeExpiresCheckService(IServiceProvider serviceProvider) : IHostedService, IDisposable
{
    private Timer? _timer;

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(CheckCodes!, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async void CheckCodes(object state)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var now = TimeHelper.GetUnspecifiedUtc();
        await context.InviteCodes
            .Where(x => x.Status == InviteCodeStatus.Active && x.Mode == InviteCodeMode.Time && x.Expires < now)
            .ExecuteUpdateAsync(x => x.SetProperty(c => c.Status, c => InviteCodeStatus.Expired));
        await context.SaveChangesAsync();
    }
}