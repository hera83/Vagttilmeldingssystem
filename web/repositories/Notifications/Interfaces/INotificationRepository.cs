using web.Repositories.Notifications.Dtos;

namespace web.Repositories.Notifications.Interfaces;

public interface INotificationRepository
{
    /// <summary>Lægger beskederne i køen. Beskeder med en DedupKey, der allerede findes, springes over. Returnerer antal nye.</summary>
    Task<int> CreateManyAsync(IReadOnlyCollection<CreateNotificationDto> items, CancellationToken cancellationToken = default);

    /// <summary>Ventende beskeder, hvis tid er kommet, ældste først.</summary>
    Task<List<NotificationDto>> GetDueAsync(DateTime nowUtc, int take, CancellationToken cancellationToken = default);

    Task<NotificationDto?> GetByDedupKeyAsync(string dedupKey, CancellationToken cancellationToken = default);

    Task<bool> MarkSentAsync(Guid id, DateTime sentAtUtc, CancellationToken cancellationToken = default);

    Task<bool> MarkSkippedAsync(Guid id, string reason, CancellationToken cancellationToken = default);

    /// <summary>Tæller et fejlet forsøg. Med nextAttemptAtUtc prøves igen da, ellers opgives beskeden (Failed).</summary>
    Task<bool> MarkAttemptFailedAsync(Guid id, string error, DateTime? nextAttemptAtUtc, CancellationToken cancellationToken = default);
}
