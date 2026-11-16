using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Repositories.Profiles.Dtos;
using web.Repositories.Profiles.Interfaces;
using web.Repositories.Profiles.Mappers;

namespace web.Repositories.Profiles;

public class ProfileRepository(WebDbContext dbContext) : IProfileRepository
{
    public Task<ProfileDto?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.Profiles.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(ProfileMapper.ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> IsCompleteAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.Profiles.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(ProfileMapper.IsCompleteExpression)
            .AnyAsync(cancellationToken);

    public async Task<HashSet<Guid>> GetCompleteUserIdsAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.Profiles.AsNoTracking()
            .Where(ProfileMapper.IsCompleteExpression)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken)).ToHashSet();

    public async Task<ProfileDto> SaveAsync(Guid userId, SaveProfileDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Profiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (entity is null)
        {
            entity = dto.ToEntity(userId);
            dbContext.Profiles.Add(entity);
        }
        else
        {
            dto.ApplyTo(entity);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task SetEmailNotificationsAsync(Guid userId, bool enabled, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Profiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (entity is null)
        {
            entity = new Data.Models.Profile { UserId = userId };
            dbContext.Profiles.Add(entity);
        }

        entity.EmailNotifications = enabled;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> GetEmailNotificationsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.Profiles.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (bool?)x.EmailNotifications)
            .FirstOrDefaultAsync(cancellationToken) ?? true;

    public async Task<HashSet<Guid>> GetEmailOptOutUserIdsAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.Profiles.AsNoTracking()
            .Where(x => !x.EmailNotifications)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken)).ToHashSet();

    public async Task<Dictionary<Guid, ProfileDto>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        return await dbContext.Profiles.AsNoTracking()
            .Where(x => ids.Contains(x.UserId))
            .Select(ProfileMapper.ToDtoExpression)
            .ToDictionaryAsync(x => x.UserId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.Profiles.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken) > 0;
}
