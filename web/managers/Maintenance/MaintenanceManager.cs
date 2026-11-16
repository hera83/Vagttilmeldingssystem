using web.Managers.Maintenance.Dtos;
using web.Managers.Maintenance.Interfaces;
using web.Repositories.Notifications.Interfaces;
using web.Repositories.PasswordTokens.Interfaces;

namespace web.Managers.Maintenance;

/// <summary>Oprydning af data, der ikke skal gemmes længere end nødvendigt (step 15, dataminimering).</summary>
public class MaintenanceManager(
    INotificationRepository notificationRepository,
    IPasswordTokenRepository passwordTokenRepository,
    IConfiguration configuration,
    TimeProvider timeProvider) : IMaintenanceManager
{
    public const int DefaultNotificationDays = 90;

    public async Task<CleanupResultDto> CleanupAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var days = Math.Max(1, configuration.GetValue("DataRetention:NotificationDays", DefaultNotificationDays));
        return new CleanupResultDto
        {
            DeletedNotifications = await notificationRepository.DeleteFinishedBeforeAsync(now.AddDays(-days), cancellationToken),
            DeletedPasswordTokens = await passwordTokenRepository.DeleteExpiredAsync(now, cancellationToken)
        };
    }
}
