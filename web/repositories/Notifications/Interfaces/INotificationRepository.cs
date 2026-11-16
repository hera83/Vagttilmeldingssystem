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
    /// <summary>Tømmer tekst og link (fx efter afsendelse af en mail med et engangslink), så de ikke ligger i databasen.</summary>
    Task<bool> ClearContentAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> MarkAttemptFailedAsync(Guid id, string error, DateTime? nextAttemptAtUtc, CancellationToken cancellationToken = default);

    /// <summary>Køens status til dashboardet: sendt og opgivet siden sinceUtc, ventende nu og den seneste opgivne besked.</summary>
    Task<NotificationStatsDto> GetStatsAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);

    /// <summary>Brugerens beskeder, nyeste først (dataudtræk).</summary>
    Task<List<NotificationDto>> GetForUserAsync(Guid userId, int take, CancellationToken cancellationToken = default);

    /// <summary>Sletter brugerens ventende beskeder (anonymisering). Returnerer antal.</summary>
    Task<int> DeletePendingForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Sletter færdigbehandlede beskeder (Sent, Skipped, Failed), der er ældre end cutoffUtc. Returnerer antal.</summary>
    Task<int> DeleteFinishedBeforeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}
