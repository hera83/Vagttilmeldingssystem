using web.Managers.Notifications;
using web.Managers.Notifications.Interfaces;
using web.Services.Mail.Interfaces;
using web.Services.Push.Interfaces;
using web.Infrastructure.Notifications;

namespace web.BgWorkers;

/// <summary>
/// Sender køen af beskeder (Notifications). Vækkes af NotificationSignal (fx efter en frigivelse) eller hvert
/// Notifications:WorkerIntervalSeconds (standard 30). Finder først nye frigivelser og sender derefter ventende beskeder.
/// </summary>
public class NotificationWorker(
    IServiceScopeFactory scopeFactory,
    NotificationSignal signal,
    IConfiguration configuration,
    ILogger<NotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Notifications:WorkerEnabled", true))
        {
            logger.LogWarning("{Worker} is disabled (Notifications:WorkerEnabled = false). No notifications are sent.", nameof(NotificationWorker));
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue("Notifications:WorkerIntervalSeconds", 30)));
        await WarnIfNotConfiguredAsync(stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var manager = scope.ServiceProvider.GetRequiredService<INotificationManager>();
                    await manager.QueueOpenedShiftsAsync(stoppingToken);
                    while (await manager.SendDueAsync(stoppingToken) >= NotificationManager.BatchSize)
                    {
                        // Fuld batch: der kan ligge flere klar.
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Forventet ved nedlukning.
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Worker} failed", nameof(NotificationWorker));
                }

                await signal.WaitAsync(interval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // WaitAsync kaster, når stoppingToken annulleres.
        }
    }

    private async Task WarnIfNotConfiguredAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var push = scope.ServiceProvider.GetRequiredService<IPushService>();
            if (await push.GetPublicKeyAsync(cancellationToken) is null)
            {
                logger.LogWarning("Push is not configured: set Push:PublicKey, Push:PrivateKey and Push:Subject (generate keys with --generate-vapid-keys). Push notifications are not sent.");
            }

            var mail = scope.ServiceProvider.GetRequiredService<IMailService>();
            if (!await mail.IsConfiguredAsync(cancellationToken))
            {
                logger.LogWarning("Mail is not configured: set Mail:FromAddress and Mail:Smtp:* (or Mail:PickupDirectory in development). E-mails are not sent.");
            }
            else if (configuration["Mail:PickupDirectory"] is { Length: > 0 })
            {
                logger.LogWarning("Mail:PickupDirectory is set: e-mails are saved as .eml files and not sent.");
            }

            if (scope.ServiceProvider.GetRequiredService<AppLinks>().BaseUrl is null)
            {
                logger.LogWarning("App:BaseUrl is not set: invitations and password resets cannot be sent, and other e-mails have no links.");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "{Worker} could not check the push configuration", nameof(NotificationWorker));
        }
    }
}
