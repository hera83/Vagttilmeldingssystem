using System.Linq.Expressions;
using web.Data.Models;
using web.Repositories.Events.Dtos;

namespace web.Repositories.Events.Mappers;

public static class EventMapper
{
    public static readonly Expression<Func<Event, EventDto>> ToDtoExpression = x => new EventDto
    {
        Id = x.Id,
        Name = x.Name,
        Description = x.Description,
        Location = x.Location,
        StartDate = x.StartDate,
        EndDate = x.EndDate,
        IsArchived = x.IsArchived,
        CreatedAt = x.CreatedAt
    };

    private static readonly Func<Event, EventDto> CompiledToDto = ToDtoExpression.Compile();

    public static EventDto ToDto(this Event entity) => CompiledToDto(entity);

    public static Event ToEntity(this CreateEventDto dto) => new()
    {
        Name = dto.Name.Trim(),
        Description = dto.Description.Trim(),
        Location = dto.Location.Trim(),
        StartDate = dto.StartDate,
        EndDate = dto.EndDate
    };

    public static void ApplyTo(this UpdateEventDto dto, Event entity)
    {
        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description.Trim();
        entity.Location = dto.Location.Trim();
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
    }
}
