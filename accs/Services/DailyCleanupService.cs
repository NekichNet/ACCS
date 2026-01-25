using accs.Services.Interfaces;

namespace accs.Services
{
    public class DailyCleanupService : BackgroundService
    {
        private readonly IServiceProvider _services;

        public DailyCleanupService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                DateTime now = DateTime.Now; 
                DateTime nextMidnight = now.Date.AddDays(1);

                TimeSpan delay = nextMidnight - now;

                await Task.Delay(delay, stoppingToken);

                using (var scope = _services.CreateScope()) 
                {
                    var cleanupService = scope.ServiceProvider.GetRequiredService<IUsersCleanUpService>(); 
                    await cleanupService.CleanupAsync();
                }
            }
        }
    }
}
