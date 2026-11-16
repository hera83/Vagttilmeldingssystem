using web.Managers.Maintenance.Interfaces;

namespace web.BgWorkers;

/// <summary>Oprydning af gamle beskeder og links (IMaintenanceManager) ved opstart og derefter én gang i døgnet (fast interval).</summary>
public class DataCleanupWorker(IServiceScopeFactory scopeFactory, ILogger<DataCleanupWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        try
        {
            do
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var result = await scope.ServiceProvider.GetRequiredService<IMaintenanceManager>().CleanupAsync(stoppingToken);
                    if (result.DeletedNotifications + result.DeletedPasswordTokens > 0)
                    {
                        logger.LogInformation("Cleanup deleted {Notifications} old notifications and {Tokens} expired password links",
                            result.DeletedNotifications, result.DeletedPasswordTokens);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Forventet ved nedlukning.
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Worker} failed", nameof(DataCleanupWorker));
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // WaitForNextTickAsync kaster, når stoppingToken annulleres.
        }
    }
}
