using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Data.Models;
using web.Infrastructure.Notifications;
using web.Repositories.Notifications.Dtos;
using web.Repositories.Notifications.Interfaces;
using web.Repositories.Notifications.Mappers;

namespace web.Repositories.Notifications;

public class NotificationRepository(WebDbContext dbContext) : INotificationRepository
{
    public const int MaxErrorLength = 1000;

    public async Task<int> CreateManyAsync(IReadOnlyCollection<CreateNotificationDto> items, CancellationToken cancellationToken = default)
    {
        var unique = items.GroupBy(x => x.DedupKey).Select(g => g.First()).ToList();
        if (unique.Count == 0)
        {
            return 0;
        }

        // BEGIN IMMEDIATE: tjek og indsæt sker under skrivelåsen, så samme DedupKey aldrig kommer to gange.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var keys = unique.Select(x => x.DedupKey).ToList();
        var existing = await dbContext.Notifications.Where(x => keys.Contains(x.DedupKey)).Select(x => x.DedupKey).ToListAsync(cancellationToken);
        var toAdd = unique.Where(x => !existing.Contains(x.DedupKey)).Select(x => x.ToEntity()).ToList();
        dbContext.Notifications.AddRange(toAdd);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return toAdd.Count;
    }

    public Task<List<NotificationDto>> GetDueAsync(DateTime nowUtc, int take, CancellationToken cancellationToken = default) =>
        dbContext.Notifications.AsNoTracking()
            .Where(x => x.Status == NotificationStatus.Pending && x.NextAttemptAt <= nowUtc)
            .OrderBy(x => x.NextAttemptAt).ThenBy(x => x.Id)
            .Take(take)
            .Select(NotificationMapper.ToDtoExpression)
            .ToListAsync(cancellationToken);

    public Task<NotificationDto?> GetByDedupKeyAsync(string dedupKey, CancellationToken cancellationToken = default) =>
        dbContext.Notifications.AsNoTracking()
            .Where(x => x.DedupKey == dedupKey)
            .Select(NotificationMapper.ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> MarkSentAsync(Guid id, DateTime sentAtUtc, CancellationToken cancellationToken = default) =>
        await dbContext.Notifications.Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, NotificationStatus.Sent)
                .SetProperty(x => x.SentAt, sentAtUtc)
                .SetProperty(x => x.Attempts, x => x.Attempts + 1)
                .SetProperty(x => x.LastError, (string?)null)
                .SetProperty(x => x.UpdatedAt, sentAtUtc), cancellationToken) > 0;

    public async Task<bool> MarkSkippedAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var text = Truncate(reason);
        return await dbContext.Notifications.Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, NotificationStatus.Skipped)
                .SetProperty(x => x.LastError, text)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken) > 0;
    }

    public async Task<bool> MarkAttemptFailedAsync(Guid id, string error, DateTime? nextAttemptAtUtc, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var text = Truncate(error);
        var status = nextAttemptAtUtc is null ? NotificationStatus.Failed : NotificationStatus.Pending;
        var next = nextAttemptAtUtc ?? now;
        return await dbContext.Notifications.Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.Attempts, x => x.Attempts + 1)
                .SetProperty(x => x.NextAttemptAt, next)
                .SetProperty(x => x.LastError, text)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken) > 0;
    }

    public async Task<bool> ClearContentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await dbContext.Notifications.Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Body, string.Empty)
                .SetProperty(x => x.Url, string.Empty)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken) > 0;
    }

    private static string Truncate(string value) => value.Length <= MaxErrorLength ? value : value[..MaxErrorLength];

    public async Task<NotificationStatsDto> GetStatsAsync(DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Notifications.AsNoTracking();
        return new NotificationStatsDto
        {
            SentSince = await query.CountAsync(x => x.Status == NotificationStatus.Sent && x.SentAt >= sinceUtc, cancellationToken),
            RemindersSentSince = await query.CountAsync(x => x.Status == NotificationStatus.Sent && x.SentAt >= sinceUtc &&
                                                            (x.Kind == NotificationKinds.Reminder48 || x.Kind == NotificationKinds.Reminder24), cancellationToken),
            PendingCount = await query.CountAsync(x => x.Status == NotificationStatus.Pending, cancellationToken),
            FailedSince = await query.CountAsync(x => x.Status == NotificationStatus.Failed && x.UpdatedAt >= sinceUtc, cancellationToken),
            LatestFailure = await query.Where(x => x.Status == NotificationStatus.Failed)
                .OrderByDescending(x => x.UpdatedAt)
                .Select(NotificationMapper.ToDtoExpression)
                .FirstOrDefaultAsync(cancellationToken)
        };
    }
}
