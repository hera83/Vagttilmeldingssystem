using web.Infrastructure.Notifications;
using web.Managers.Reminders.Interfaces;

namespace web.BgWorkers;

/// <summary>
/// Scanner efter påmindelser 48 og 24 timer før vagtstart (krav K19–K21): ved opstart og derefter hvert
/// Reminders:WorkerIntervalSeconds (standard 3600 = én gang i timen, som i projektbeskrivelsen). Slås fra med Reminders:Enabled = false.
/// Beskederne sendes af NotificationWorker.
/// </summary>
public class ReminderWorker(
    IServiceScopeFactory scopeFactory,
    ReminderWorkerStatus status,
    TimeProvider timeProvider,
    IConfiguration configuration,
    ILogger<ReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = configuration.GetValue("Reminders:Enabled", true);
        var interval = TimeSpan.FromSeconds(Math.Max(10, configuration.GetValue("Reminders:WorkerIntervalSeconds", 3600)));
        status.Configure(enabled, interval);
        if (!enabled)
        {
            logger.LogWarning("{Worker} is disabled (Reminders:Enabled = false). No reminders are sent.", nameof(ReminderWorker));
            return;
        }

        using var timer = new PeriodicTimer(interval, timeProvider);
        try
        {
            do
            {
                var started = timeProvider.GetUtcNow().UtcDateTime;
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var manager = scope.ServiceProvider.GetRequiredService<IReminderManager>();
                    var result = await manager.QueueDueAsync(stoppingToken);
                    status.Completed(started, timeProvider.GetUtcNow().UtcDateTime, result.DueCount, result.QueuedCount, started + interval);
                    if (result.QueuedCount > 0)
                    {
                        logger.LogInformation("Queued {Count} reminders for {Due} assignments", result.QueuedCount, result.DueCount);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Forventet ved nedlukning.
                }
                catch (Exception ex)
                {
                    status.Failed(started, ex.Message, started + interval);
                    logger.LogError(ex, "{Worker} failed", nameof(ReminderWorker));
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // WaitForNextTickAsync kaster, når stoppingToken annulleres.
        }
    }
}
