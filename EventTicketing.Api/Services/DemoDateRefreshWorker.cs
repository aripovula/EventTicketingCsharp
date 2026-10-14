namespace EventTicketing.Api.Services;

/// <summary>Runs the <see cref="DemoDateRefresher"/> when the API starts and once a day after that.</summary>
public class DemoDateRefreshWorker(IServiceScopeFactory scopeFactory, ILogger<DemoDateRefreshWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<DemoDateRefresher>()
                    .RefreshAsync(DateOnly.FromDateTime(DateTime.UtcNow), stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Stale demo dates are harmless; try again tomorrow.
                logger.LogError(ex, "Refreshing the demo event dates failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
