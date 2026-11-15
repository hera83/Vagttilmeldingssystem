using web.Data.Models;
using web.Extensions;
using web.Infrastructure.Notifications;
using web.Managers.Reminders.Dtos;
using web.Managers.Reminders.Interfaces;
using web.Repositories.Assignments.Dtos;
using web.Repositories.Assignments.Interfaces;
using web.Repositories.Notifications.Dtos;
using web.Repositories.Notifications.Interfaces;
using web.Repositories.Profiles.Interfaces;
using web.Repositories.PushSubscriptions.Interfaces;
using web.Services.Mail.Interfaces;
using web.Services.Push.Interfaces;

namespace web.Managers.Reminders;

/// <summary>
/// Påmindelser 48 og 24 timer før vagtstart (krav K19, K20). Kaldes af ReminderWorker. Beskederne lægges i køen
/// (Notifications) med en DedupKey pr. påmindelse, tildeling, starttid og kanal, så gentagne scanninger aldrig giver dubletter.
/// </summary>
public class ReminderManager(
    IAssignmentRepository assignmentRepository,
    INotificationRepository notificationRepository,
    IPushSubscriptionRepository pushSubscriptionRepository,
    IProfileRepository profileRepository,
    IPushService pushService,
    IMailService mailService,
    AppLinks appLinks,
    NotificationSignal signal,
    TimeProvider timeProvider) : IReminderManager
{
    public static readonly TimeSpan FirstReminder = TimeSpan.FromHours(48);
    public static readonly TimeSpan SecondReminder = TimeSpan.FromHours(24);

    public async Task<ReminderScanResultDto> QueueDueAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var upcoming = await assignmentRepository.GetActiveStartingBetweenAsync(now, now + FirstReminder, cancellationToken);
        var result = new ReminderScanResultDto { ScannedAtUtc = now };
        if (upcoming.Count == 0)
        {
            return result;
        }

        var pushUsers = await pushService.GetPublicKeyAsync(cancellationToken) is not null
            ? (await pushSubscriptionRepository.GetActiveSubscriberIdsAsync(cancellationToken)).ToHashSet()
            : [];
        var mailEnabled = await mailService.IsConfiguredAsync(cancellationToken);
        var emailOptOut = mailEnabled ? await profileRepository.GetEmailOptOutUserIdsAsync(cancellationToken) : [];

        var items = new List<CreateNotificationDto>();
        foreach (var assignment in upcoming)
        {
            var kind = DueKind(assignment, now);
            if (kind is null)
            {
                continue;
            }

            result.DueCount++;
            var shift = new MailShiftLine(assignment.Title, assignment.EventName,
                $"{assignment.StartUtc.ToDanishDateText()} kl. {DateTimeExtensions.ToDanishTimeRangeText(assignment.StartUtc, assignment.EndUtc)}",
                assignment.EventLocation);
            var path = $"/MyShifts/Details/{assignment.ShiftId}";
            var key = $"reminder:{(kind == NotificationKinds.Reminder48 ? 48 : 24)}:{assignment.AssignmentId:N}:{assignment.StartUtc.Ticks}";

            if (pushUsers.Contains(assignment.UserId))
            {
                var push = MailTemplates.ReminderPush(kind, shift, assignment.StartUtc, now);
                items.Add(Create(assignment, kind, push, path, $"{key}:push", NotificationChannel.Push, now));
            }

            if (mailEnabled && !emailOptOut.Contains(assignment.UserId))
            {
                var mail = MailTemplates.Reminder(kind, shift, assignment.StartUtc, now, appLinks.TryAbsolute(path));
                items.Add(Create(assignment, kind, mail, appLinks.TryAbsolute(path) ?? string.Empty, $"{key}:email", NotificationChannel.Email, now));
            }
        }

        result.QueuedCount = await notificationRepository.CreateManyAsync(items, cancellationToken);
        if (result.QueuedCount > 0)
        {
            signal.Notify();
        }

        return result;
    }

    /// <summary>
    /// Hvilken påmindelse, tildelingen skal have nu, eller null.
    /// 48 t: fra start − 48 t til start − 24 t, og kun hvis tildelingen fandtes ved start − 48 t (ellers ville "om 2 dage" komme
    /// lige efter tilmeldingen). 24 t: fra start − 24 t til start, også for tildelinger oprettet senere. Startet: ingen.
    /// </summary>
    public static string? DueKind(UpcomingAssignmentDto assignment, DateTime nowUtc)
    {
        if (nowUtc >= assignment.StartUtc)
        {
            return null;
        }

        if (nowUtc >= assignment.StartUtc - SecondReminder)
        {
            return NotificationKinds.Reminder24;
        }

        if (nowUtc >= assignment.StartUtc - FirstReminder && assignment.AssignedAt <= assignment.StartUtc - FirstReminder)
        {
            return NotificationKinds.Reminder48;
        }

        return null;
    }

    private static CreateNotificationDto Create(UpcomingAssignmentDto assignment, string kind, MailText text, string url, string dedupKey,
        NotificationChannel channel, DateTime now) => new()
    {
        UserId = assignment.UserId,
        Channel = channel,
        Kind = kind,
        Title = text.Subject,
        Body = text.Body,
        Url = url,
        DedupKey = dedupKey,
        AssignmentId = assignment.AssignmentId,
        NotBeforeUtc = now
    };
}
