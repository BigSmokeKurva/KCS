
using Microsoft.EntityFrameworkCore;
using KCS.Server.Database;

namespace KCS.Server.Services
{
    public class SessionExpiresCheckService(IServiceProvider _serviceProvider) : IHostedService, IDisposable
    {
        private Timer _timer = null;
        private readonly IServiceProvider _serviceProvider = _serviceProvider;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(CheckSessions, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        }

        public async void CheckSessions(object state)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            await context.Sessions.Where(x => x.Expires < TimeHelper.GetUnspecifiedUtc()).ExecuteDeleteAsync();
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
