using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Repositories.Events.Dtos;
using web.Repositories.Events.Interfaces;
using web.Repositories.Events.Mappers;

namespace web.Repositories.Events;

public class EventRepository(WebDbContext dbContext) : IEventRepository
{
    public async Task<List<EventDto>> SearchAsync(EventSearchDto search, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Events.AsNoTracking().Where(x => x.IsArchived == search.Archived);

        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            var pattern = "%" + EscapeLike(search.Text.Trim()) + "%";
            query = query.Where(x => EF.Functions.Like(x.Name, pattern, "\\") || EF.Functions.Like(x.Location, pattern, "\\"));
        }

        // Aktive: de næste først. Arkiverede: de nyeste først.
        query = search.Archived
            ? query.OrderByDescending(x => x.StartDate).ThenBy(x => x.Name)
            : query.OrderBy(x => x.StartDate).ThenBy(x => x.Name);

        return await query.Select(EventMapper.ToDtoExpression).ToListAsync(cancellationToken);
    }

    public async Task<EventCountsDto> GetCountsAsync(CancellationToken cancellationToken = default)
    {
        var counts = await dbContext.Events.AsNoTracking()
            .GroupBy(x => x.IsArchived)
            .Select(g => new { Archived = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new EventCountsDto
        {
            Active = counts.FirstOrDefault(x => !x.Archived)?.Count ?? 0,
            Archived = counts.FirstOrDefault(x => x.Archived)?.Count ?? 0
        };
    }

    public Task<EventDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Events.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(EventMapper.ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<EventDto> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default)
    {
        var entity = dto.ToEntity();
        dbContext.Events.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<EventDto?> UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Events.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        dto.ApplyTo(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<bool> SetArchivedAsync(Guid id, bool archived, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Events.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsArchived = archived;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Events.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
