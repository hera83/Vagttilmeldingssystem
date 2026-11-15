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

    public async Task<ShiftDto> CreateAsync(Guid eventId, CreateShiftDto dto, CancellationToken cancellationToken = default)
    {
        var entity = dto.ToEntity(eventId);
        dbContext.Shifts.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<ShiftDto?> UpdateAsync(Guid id, UpdateShiftDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Shifts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        dto.ApplyTo(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Shifts.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;
}
