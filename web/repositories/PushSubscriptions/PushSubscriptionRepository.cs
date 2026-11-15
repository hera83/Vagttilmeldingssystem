using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Repositories.PushSubscriptions.Dtos;
using web.Repositories.PushSubscriptions.Interfaces;
using web.Repositories.PushSubscriptions.Mappers;

namespace web.Repositories.PushSubscriptions;

public class PushSubscriptionRepository(WebDbContext dbContext) : IPushSubscriptionRepository
{
    public async Task<PushSubscriptionDto> SaveAsync(SavePushSubscriptionDto dto, CancellationToken cancellationToken = default)
    {
        // BEGIN IMMEDIATE: to samtidige tilmeldinger fra samme enhed kan ikke begge oprette en række.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var entity = await dbContext.PushSubscriptions.FirstOrDefaultAsync(x => x.Endpoint == dto.Endpoint, cancellationToken);
        if (entity is null)
        {
            entity = dto.ToEntity();
            dbContext.PushSubscriptions.Add(entity);
        }
        else
        {
            dto.ApplyTo(entity);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<bool> DeleteForUserAsync(Guid userId, string endpoint, CancellationToken cancellationToken = default) =>
        await dbContext.PushSubscriptions.Where(x => x.UserId == userId && x.Endpoint == endpoint).ExecuteDeleteAsync(cancellationToken) > 0;

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.PushSubscriptions.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;

    public Task<List<PushSubscriptionDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.PushSubscriptions.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.CreatedAt)
            .Select(PushSubscriptionMapper.ToDtoExpression)
            .ToListAsync(cancellationToken);

    public Task<List<Guid>> GetActiveSubscriberIdsAsync(CancellationToken cancellationToken = default) =>
        (from s in dbContext.PushSubscriptions.AsNoTracking()
         join u in dbContext.Users on s.UserId equals u.Id
         where u.IsActive
         select s.UserId).Distinct().ToListAsync(cancellationToken);

    public async Task<bool> MarkSuccessAsync(Guid id, DateTime atUtc, CancellationToken cancellationToken = default) =>
        await dbContext.PushSubscriptions.Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.LastSuccessAt, atUtc)
                .SetProperty(x => x.UpdatedAt, atUtc), cancellationToken) > 0;
}
