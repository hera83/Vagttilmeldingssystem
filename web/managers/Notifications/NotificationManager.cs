using web.Data.Models;
using web.Infrastructure.Notifications;
using web.Managers.Notifications.Dtos;
using web.Managers.Notifications.Interfaces;
using web.Repositories.Notifications.Dtos;
using web.Repositories.Notifications.Interfaces;
using web.Repositories.PushSubscriptions.Dtos;
using web.Repositories.Profiles.Interfaces;
using web.Repositories.PushSubscriptions.Interfaces;
using web.Repositories.Shifts.Interfaces;
using web.Repositories.Users.Interfaces;
using web.Services.Mail;
using web.Services.Mail.Dtos.Messages;
using web.Services.Mail.Interfaces;
using web.Services.Push;
using web.Services.Push.Dtos.Messages;
using web.Services.Push.Interfaces;

namespace web.Managers.Notifications;

/// <summary>
/// Køen af udgående beskeder: hvad der lægges i køen (frigivelser, testbeskeder), og hvordan den sendes (push og e-mail).
/// Kaldes af NotificationWorker og PushController.
/// </summary>
public class NotificationManager(
    IShiftRepository shiftRepository,
    INotificationRepository notificationRepository,
    IPushSubscriptionRepository pushSubscriptionRepository,
    IPushService pushService,
    IUserRepository userRepository,
    IProfileRepository profileRepository,
    IMailService mailService,
    AppLinks appLinks,
    NotificationSignal signal,
    TimeProvider timeProvider,
    ILogger<NotificationManager> logger) : INotificationManager
{
    public const int BatchSize = 100;
    public const int MaxAttempts = 3;

    /// <summary>Ventetid før forsøg nr. 2 og 3.</summary>
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)];

    /// <summary>Frigivelser, der er ældre end dette, markeres uden beskeder (fx hvis appen har været nede længe).</summary>
    public static readonly TimeSpan OpenedMaxAge = TimeSpan.FromHours(24);

    public async Task<PushSettingsDto> GetPushSettingsAsync(CancellationToken cancellationToken = default)
    {
        var publicKey = await pushService.GetPublicKeyAsync(cancellationToken);
        return new PushSettingsDto { Enabled = publicKey is not null, PublicKey = publicKey ?? string.Empty };
    }

    public async Task<SubscribePushResult> SubscribeAsync(Guid userId, SubscribePushDto dto, CancellationToken cancellationToken = default)
    {
        if (await pushService.GetPublicKeyAsync(cancellationToken) is null)
        {
            return SubscribePushResult.PushDisabled;
        }

        if (!PushEndpoints.IsAllowed(dto.Endpoint))
        {
            return SubscribePushResult.InvalidEndpoint;
        }

        await pushSubscriptionRepository.SaveAsync(new SavePushSubscriptionDto
        {
            UserId = userId,
            Endpoint = dto.Endpoint,
            P256dh = dto.P256dh,
            Auth = dto.Auth
        }, cancellationToken);
        return SubscribePushResult.Success;
    }

    public Task<bool> UnsubscribeAsync(Guid userId, string endpoint, CancellationToken cancellationToken = default) =>
        pushSubscriptionRepository.DeleteForUserAsync(userId, endpoint, cancellationToken);

    public async Task<QueueTestResult> QueueTestAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (await pushService.GetPublicKeyAsync(cancellationToken) is null)
        {
            return QueueTestResult.PushDisabled;
        }

        if ((await pushSubscriptionRepository.GetByUserIdAsync(userId, cancellationToken)).Count == 0)
        {
            return QueueTestResult.NoDevices;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        await notificationRepository.CreateManyAsync(
        [
            new CreateNotificationDto
            {
                UserId = userId,
                Channel = NotificationChannel.Push,
                Kind = NotificationKinds.Test,
                Title = "Testbesked fra Vagttilmelding",
                Body = "Push-beskeder virker på dine enheder.",
                Url = "/Profile",
                DedupKey = $"test:{userId:N}:{now:yyyyMMddHHmm}",
                NotBeforeUtc = now
            }
        ], cancellationToken);
        signal.Notify();
        return QueueTestResult.Queued;
    }

    public async Task<int> QueueOpenedShiftsAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var batches = await shiftRepository.GetUnnotifiedOpenedAsync(now, cancellationToken);
        if (batches.Count == 0)
        {
            return 0;
        }

        var pushEnabled = await pushService.GetPublicKeyAsync(cancellationToken) is not null;
        var recipients = pushEnabled ? await pushSubscriptionRepository.GetActiveSubscriberIdsAsync(cancellationToken) : [];
        var emailRecipients = await GetEmailRecipientsAsync(cancellationToken);
        var queued = 0;

        foreach (var batch in batches)
        {
            var current = batch.AvailableCount > 0 && now - batch.OpenedAt <= OpenedMaxAge;
            if (current && recipients.Count > 0)
            {
                // Generisk tekst: kun begivenhedens navn og antal åbne vagter. Ingen persondata og aldrig kladder (krav K10).
                var title = "Nye vagter er åbne";
                var body = $"{batch.EventName}: {batch.AvailableCount} {(batch.AvailableCount == 1 ? "vagt er åben" : "vagter er åbne")} for tilmelding. Først til mølle.";
                var url = $"/Signup?eventId={batch.EventId}";
                queued += await notificationRepository.CreateManyAsync(recipients.Select(userId => new CreateNotificationDto
                {
                    UserId = userId,
                    Channel = NotificationChannel.Push,
                    Kind = NotificationKinds.ShiftsOpened,
                    Title = title,
                    Body = body,
                    Url = url,
                    DedupKey = $"opened:{batch.EventId:N}:{batch.OpenedAt.Ticks}:push:{userId:N}",
                    NotBeforeUtc = now
                }).ToList(), cancellationToken);
            }

            if (current && emailRecipients.Count > 0)
            {
                var mail = MailTemplates.ShiftsOpened(batch.EventName, batch.AvailableCount, appLinks.TryAbsolute($"/Signup?eventId={batch.EventId}"));
                queued += await notificationRepository.CreateManyAsync(emailRecipients.Select(userId => new CreateNotificationDto
                {
                    UserId = userId,
                    Channel = NotificationChannel.Email,
                    Kind = NotificationKinds.ShiftsOpened,
                    Title = mail.Subject,
                    Body = mail.Body,
                    Url = appLinks.TryAbsolute($"/Signup?eventId={batch.EventId}") ?? string.Empty,
                    DedupKey = $"opened:{batch.EventId:N}:{batch.OpenedAt.Ticks}:email:{userId:N}",
                    NotBeforeUtc = now
                }).ToList(), cancellationToken);
            }

            // Markeres efter køen: går noget galt imellem, behandles frigivelsen igen, og DedupKey forhindrer dobbelte beskeder.
            await shiftRepository.MarkOpenNotifiedAsync(batch.ShiftIds, now, cancellationToken);
        }

        if (queued > 0)
        {
            logger.LogInformation("Queued {Count} notifications for {Batches} opened shift batches", queued, batches.Count);
        }

        return queued;
    }

    public async Task<int> SendDueAsync(CancellationToken cancellationToken = default)
    {
        var due = await notificationRepository.GetDueAsync(timeProvider.GetUtcNow().UtcDateTime, BatchSize, cancellationToken);
        foreach (var notification in due)
        {
            switch (notification.Channel)
            {
                case NotificationChannel.Push:
                    await SendPushAsync(notification, cancellationToken);
                    break;
                case NotificationChannel.Email:
                    await SendEmailAsync(notification, cancellationToken);
                    break;
                default:
                    await notificationRepository.MarkSkippedAsync(notification.Id, $"Kanalen {notification.Channel} er ikke sat op.", cancellationToken);
                    break;
            }
        }

        return due.Count;
    }

    private async Task SendPushAsync(NotificationDto notification, CancellationToken cancellationToken)
    {
        if (await pushService.GetPublicKeyAsync(cancellationToken) is null)
        {
            await notificationRepository.MarkSkippedAsync(notification.Id, "Push er ikke sat op (Push:PublicKey, Push:PrivateKey og Push:Subject).", cancellationToken);
            return;
        }

        var subscriptions = await pushSubscriptionRepository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (subscriptions.Count == 0)
        {
            await notificationRepository.MarkSkippedAsync(notification.Id, "Brugeren har ingen enheder med push.", cancellationToken);
            return;
        }

        var delivered = 0;
        var errors = new List<string>();
        foreach (var subscription in subscriptions)
        {
            try
            {
                var response = await pushService.SendAsync(new SendPushRequestDto
                {
                    Endpoint = subscription.Endpoint,
                    P256dh = subscription.P256dh,
                    Auth = subscription.Auth,
                    Title = notification.Title,
                    Body = notification.Body,
                    Url = notification.Url,
                    Tag = $"{notification.Kind}:{notification.Url}",
                    Urgency = notification.Kind == NotificationKinds.ShiftsOpened ? PushUrgency.High : PushUrgency.Normal
                }, cancellationToken);

                if (response.Status == PushDeliveryStatus.Delivered)
                {
                    delivered++;
                    await pushSubscriptionRepository.MarkSuccessAsync(subscription.Id, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
                }
                else
                {
                    // Enheden har opsagt abonnementet (fx afinstalleret appen eller fjernet tilladelsen).
                    await pushSubscriptionRepository.DeleteAsync(subscription.Id, cancellationToken);
                }
            }
            catch (PushException ex)
            {
                // Køen skal registrere fejlen pr. besked og prøve igen senere, så fejlen fanges her (bevidst afvigelse, se projektfilen).
                errors.Add(ex.Message);
                logger.LogWarning("Push to {Host} failed for notification {NotificationId}: {Error}",
                    PushEndpoints.HostOf(subscription.Endpoint), notification.Id, ex.Message);
            }
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (delivered > 0)
        {
            await notificationRepository.MarkSentAsync(notification.Id, now, cancellationToken);
        }
        else if (errors.Count == 0)
        {
            await notificationRepository.MarkSkippedAsync(notification.Id, "Alle enhedernes abonnementer var udløbet og er slettet.", cancellationToken);
        }
        else
        {
            await notificationRepository.MarkAttemptFailedAsync(notification.Id, errors[0], NextAttempt(notification, now), cancellationToken);
        }
    }

    private async Task SendEmailAsync(NotificationDto notification, CancellationToken cancellationToken)
    {
        if (!await mailService.IsConfiguredAsync(cancellationToken))
        {
            await FinishAsync(notification, notificationRepository.MarkSkippedAsync(notification.Id,
                "E-mail er ikke sat op (Mail:FromAddress og Mail:Smtp:Host, eller Mail:PickupDirectory).", cancellationToken), cancellationToken);
            return;
        }

        var user = await userRepository.GetByIdAsync(notification.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            await FinishAsync(notification, notificationRepository.MarkSkippedAsync(notification.Id, "Brugeren findes ikke eller er deaktiveret.", cancellationToken), cancellationToken);
            return;
        }

        var text = MailTemplates.Wrap(user.DisplayName, notification.Body);
        var url = string.IsNullOrWhiteSpace(notification.Url) ? null : notification.Url;
        try
        {
            await mailService.SendAsync(new SendMailRequestDto
            {
                ToAddress = user.UserName,
                ToName = user.DisplayName,
                Subject = notification.Title,
                TextBody = text,
                HtmlBody = MailTemplates.ToHtml(notification.Title, text, url, NotificationKinds.ButtonText(notification.Kind))
            }, cancellationToken);
            await FinishAsync(notification, notificationRepository.MarkSentAsync(notification.Id, timeProvider.GetUtcNow().UtcDateTime, cancellationToken), cancellationToken);
        }
        catch (MailException ex)
        {
            // Køen registrerer fejlen og prøver igen (bevidst afvigelse, se projektfilen). En afvist modtager prøves ikke igen.
            logger.LogWarning("Mail for notification {NotificationId} ({Kind}) failed: {Error}", notification.Id, notification.Kind, ex.Message);
            var next = ex.StatusCode == System.Net.HttpStatusCode.BadRequest ? null : NextAttempt(notification, timeProvider.GetUtcNow().UtcDateTime);
            await notificationRepository.MarkAttemptFailedAsync(notification.Id, ex.Message, next, cancellationToken);
            if (next is null && NotificationKinds.ContainsSecretLink(notification.Kind))
            {
                await notificationRepository.ClearContentAsync(notification.Id, cancellationToken);
            }
        }
    }

    /// <summary>Afslutter en besked. Indeholder den et engangslink, tømmes teksten, så linket ikke ligger i databasen.</summary>
    private async Task FinishAsync(NotificationDto notification, Task<bool> mark, CancellationToken cancellationToken)
    {
        await mark;
        if (NotificationKinds.ContainsSecretLink(notification.Kind))
        {
            await notificationRepository.ClearContentAsync(notification.Id, cancellationToken);
        }
    }

    private static DateTime? NextAttempt(NotificationDto notification, DateTime now)
    {
        var attempt = notification.Attempts + 1;
        return attempt < MaxAttempts ? now + RetryDelays[Math.Min(attempt - 1, RetryDelays.Length - 1)] : null;
    }

    /// <summary>Aktive brugere med e-mail-beskeder slået til (standard), når mail er sat op.</summary>
    private async Task<List<Guid>> GetEmailRecipientsAsync(CancellationToken cancellationToken)
    {
        if (!await mailService.IsConfiguredAsync(cancellationToken))
        {
            return [];
        }

        var optedOut = await profileRepository.GetEmailOptOutUserIdsAsync(cancellationToken);
        return (await userRepository.GetAllAsync(cancellationToken))
            .Where(x => x.IsActive && !optedOut.Contains(x.Id))
            .Select(x => x.Id)
            .ToList();
    }
}
