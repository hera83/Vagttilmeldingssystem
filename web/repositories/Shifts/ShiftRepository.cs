using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Data.Models;
using web.Repositories.Shifts.Dtos;
using web.Repositories.Shifts.Interfaces;
using web.Repositories.Shifts.Mappers;

namespace web.Repositories.Shifts;

public class ShiftRepository(WebDbContext dbContext) : IShiftRepository
{
    public Task<ShiftDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Shifts.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(ShiftMapper.ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<ShiftDto>> GetByEventIdAsync(Guid eventId, ShiftStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Shifts.AsNoTracking().Where(x => x.EventId == eventId);
        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        return query.OrderBy(x => x.StartUtc).ThenBy(x => x.Title)
            .Select(ShiftMapper.ToDtoExpression)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> AnyForEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        dbContext.Shifts.AnyAsync(x => x.EventId == eventId, cancellationToken);

    public async Task<Dictionary<Guid, Guid>> GetEventIdsAsync(IEnumerable<Guid> shiftIds, CancellationToken cancellationToken = default)
    {
        var ids = shiftIds.Distinct().ToList();
        return await dbContext.Shifts.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.EventId, cancellationToken);
    }

    public Task<List<OpenShiftDto>> GetOpenAsync(DateTime nowUtc, CancellationToken cancellationToken = default) =>
        (from s in dbContext.Shifts.AsNoTracking()
         join e in dbContext.Events on s.EventId equals e.Id
         where s.Status == ShiftStatus.Open && !e.IsArchived && s.StartUtc > nowUtc
         orderby s.StartUtc, s.Title
         select new OpenShiftDto
         {
             Id = s.Id,
             Title = s.Title,
             Description = s.Description,
             StartUtc = s.StartUtc,
             EndUtc = s.EndUtc,
             Capacity = s.Capacity,
             EventId = e.Id,
             EventName = e.Name,
             EventLocation = e.Location
         }).ToListAsync(cancellationToken);

    public async Task<List<OpenedBatchDto>> GetUnnotifiedOpenedAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var rows = await (from s in dbContext.Shifts.AsNoTracking()
                          join e in dbContext.Events on s.EventId equals e.Id
                          where s.Status == ShiftStatus.Open && s.OpenNotifiedAt == null && s.OpenedAt != null
                          select new { s.Id, s.EventId, EventName = e.Name, OpenedAt = s.OpenedAt!.Value, Available = !e.IsArchived && s.StartUtc > nowUtc })
            .ToListAsync(cancellationToken);

        // Frigivelsen sætter samme OpenedAt på alle vagter i samme handling (OpenAsync).
        return rows
            .GroupBy(x => new { x.EventId, x.OpenedAt })
            .Select(g => new OpenedBatchDto
            {
                EventId = g.Key.EventId,
                EventName = g.First().EventName,
                OpenedAt = g.Key.OpenedAt,
                ShiftIds = g.Select(x => x.Id).ToList(),
                AvailableCount = g.Count(x => x.Available)
            })
            .OrderBy(x => x.OpenedAt)
            .ToList();
    }

    public Task<int> MarkOpenNotifiedAsync(IReadOnlyCollection<Guid> shiftIds, DateTime atUtc, CancellationToken cancellationToken = default)
    {
        var ids = shiftIds.Distinct().ToList();
        return dbContext.Shifts
            .Where(x => ids.Contains(x.Id) && x.OpenNotifiedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.OpenNotifiedAt, atUtc)
                .SetProperty(x => x.UpdatedAt, atUtc), cancellationToken);
    }

    public Task<List<EventShiftCountsDto>> GetStatusCountsAsync(IEnumerable<Guid> eventIds, CancellationToken cancellationToken = default)
    {
        var ids = eventIds.Distinct().ToList();
        return dbContext.Shifts.AsNoTracking()
            .Where(x => ids.Contains(x.EventId))
            .GroupBy(x => x.EventId)
            .Select(g => new EventShiftCountsDto
            {
                EventId = g.Key,
                DraftCount = g.Count(x => x.Status == ShiftStatus.Draft),
                OpenCount = g.Count(x => x.Status == ShiftStatus.Open)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> OpenAsync(Guid eventId, IReadOnlyCollection<Guid> shiftIds, Guid openedByUserId, CancellationToken cancellationToken = default)
    {
        var ids = shiftIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        // Én transaktion: alle valgte kladder bliver åbne på samme tid, eller ingen (krav K12).
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var toOpen = await dbContext.Shifts
            .Where(x => x.EventId == eventId && ids.Contains(x.Id) && x.Status == ShiftStatus.Draft)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (toOpen.Count > 0)
        {
            var now = DateTime.UtcNow;
            await dbContext.Shifts
                .Where(x => toOpen.Contains(x.Id))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, ShiftStatus.Open)
                    .SetProperty(x => x.OpenedAt, now)
                    .SetProperty(x => x.OpenedByUserId, openedByUserId)
                    .SetProperty(x => x.UpdatedAt, now), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return toOpen;
    }

    public async Task<ShiftDto> CreateAsync(Guid eventId, CreateShiftDto dto, CancellationToken cancellationToken = default)
    {
        var entity = dto.ToEntity(eventId);
        dbContext.Shifts.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<ShiftChangeResultDto> UpdateAsync(Guid id, UpdateShiftDto dto, CancellationToken cancellationToken = default)
    {
        // Samme slags transaktion som bookingkernen (BEGIN IMMEDIATE), så ingen kan tilmelde sig mellem tælling og ændring.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var entity = await dbContext.Shifts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return new ShiftChangeResultDto { Status = ShiftChangeStatus.NotFound };
        }

        var taken = await dbContext.Assignments.CountAsync(a => a.ShiftId == id && a.IsActive, cancellationToken);
        if (dto.Capacity < taken)
        {
            return new ShiftChangeResultDto { Status = ShiftChangeStatus.CapacityBelowTaken, Taken = taken };
        }

        dto.ApplyTo(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ShiftChangeResultDto { Status = ShiftChangeStatus.Success, Taken = taken };
    }

    public async Task<ShiftChangeResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var taken = await dbContext.Assignments.CountAsync(a => a.ShiftId == id && a.IsActive, cancellationToken);
        if (taken > 0)
        {
            return new ShiftChangeResultDto { Status = ShiftChangeStatus.HasActiveAssignments, Taken = taken };
        }

        var deleted = await dbContext.Shifts.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ShiftChangeResultDto { Status = deleted > 0 ? ShiftChangeStatus.Success : ShiftChangeStatus.NotFound };
    }
}
