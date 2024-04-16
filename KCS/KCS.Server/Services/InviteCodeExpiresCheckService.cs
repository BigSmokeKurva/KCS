using Microsoft.EntityFrameworkCore;
using KCS.Server.Database;
using KCS.Server.Database.Models;

namespace KCS.Server.Services
{
    public class InviteCodeExpiresCheckService(IServiceProvider _serviceProvider) : IHostedService, IDisposable
    {
        private Timer _timer = null;
        private readonly IServiceProvider _serviceProvider = _serviceProvider;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(CheckCodes, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        private async void CheckCodes(object state)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var now = TimeHelper.GetUnspecifiedUtc();
            await context.InviteCodes.Where(x => x.Status == InviteCodeStatus.Active && x.Mode == InviteCodeMode.Time && x.Expires < now).ExecuteUpdateAsync(x => x.SetProperty(c => c.Status, c => InviteCodeStatus.Expired));
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
