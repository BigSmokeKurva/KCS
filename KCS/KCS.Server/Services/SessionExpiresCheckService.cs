using KCS.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace KCS.Server.Services;

public class SessionExpiresCheckService(IServiceProvider serviceProvider) : IHostedService, IDisposable
{
    private Timer? _timer;

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(CheckSessions, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async void CheckSessions(object? state)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        await context.Sessions.Where(x => x.Expires < TimeHelper.GetUnspecifiedUtc()).ExecuteDeleteAsync();
        await context.SaveChangesAsync();
    }
}