using web.Infrastructure.Notifications;
using web.Services.Mail.Interfaces;

namespace web.BgWorkers;

/// <summary>
/// Henter antal ulæste i systemets postkasse hvert Mail:Imap:PollIntervalSeconds (standard 300) til MailboxStatus
/// (KPI-kortet på dashboardet). Kører kun, når IMAP er sat op. Én afhængighed (IMailService), så ingen manager.
/// </summary>
public class MailboxPollWorker(
    IServiceScopeFactory scopeFactory,
    MailboxStatus status,
    TimeProvider timeProvider,
    IConfiguration configuration,
    ILogger<MailboxPollWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(30, configuration.GetValue("Mail:Imap:PollIntervalSeconds", 300)));
        using var timer = new PeriodicTimer(interval, timeProvider);
        try
        {
            do
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var mail = scope.ServiceProvider.GetRequiredService<IMailService>();
                    if (!await mail.IsMailboxConfiguredAsync(stoppingToken))
                    {
                        status.Configured(false);
                        continue;
                    }

                    var mailbox = await mail.GetMailboxStatusAsync(stoppingToken);
                    status.Succeeded(mailbox.UnreadCount, timeProvider.GetUtcNow().UtcDateTime);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Forventet ved nedlukning.
                }
                catch (Exception ex)
                {
                    status.Failed(ex.Message, timeProvider.GetUtcNow().UtcDateTime);
                    logger.LogWarning("{Worker} could not check the mailbox: {Error}", nameof(MailboxPollWorker), ex.Message);
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // WaitForNextTickAsync kaster, når stoppingToken annulleres.
        }
    }
}
