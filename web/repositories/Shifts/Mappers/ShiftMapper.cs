using System.Linq.Expressions;
using web.Data.Models;
using web.Repositories.Shifts.Dtos;

namespace web.Repositories.Shifts.Mappers;

public static class ShiftMapper
{
    public static readonly Expression<Func<Shift, ShiftDto>> ToDtoExpression = x => new ShiftDto
    {
        Id = x.Id,
        EventId = x.EventId,
        Title = x.Title,
        Description = x.Description,
        StartUtc = x.StartUtc,
        EndUtc = x.EndUtc,
        Capacity = x.Capacity,
        Status = x.Status,
        OpenedAt = x.OpenedAt
    };

    private static readonly Func<Shift, ShiftDto> CompiledToDto = ToDtoExpression.Compile();

    public static ShiftDto ToDto(this Shift entity) => CompiledToDto(entity);

    public static Shift ToEntity(this CreateShiftDto dto, Guid eventId) => new()
    {
        EventId = eventId,
        Title = dto.Title.Trim(),
        Description = dto.Description.Trim(),
        StartUtc = DateTime.SpecifyKind(dto.StartUtc, DateTimeKind.Utc),
        EndUtc = DateTime.SpecifyKind(dto.EndUtc, DateTimeKind.Utc),
        Capacity = dto.Capacity,
        // Altid kladde, uanset hvad kalderen ønsker (krav K10).
        Status = ShiftStatus.Draft
    };

    public static void ApplyTo(this UpdateShiftDto dto, Shift entity)
    {
        entity.Title = dto.Title.Trim();
        entity.Description = dto.Description.Trim();
        entity.StartUtc = DateTime.SpecifyKind(dto.StartUtc, DateTimeKind.Utc);
        entity.EndUtc = DateTime.SpecifyKind(dto.EndUtc, DateTimeKind.Utc);
        entity.Capacity = dto.Capacity;
    }
}
