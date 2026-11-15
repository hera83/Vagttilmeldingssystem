using web.Repositories.Events.Dtos;

namespace web.Repositories.Events.Interfaces;

public interface IEventRepository
{
    Task<List<EventDto>> SearchAsync(EventSearchDto search, CancellationToken cancellationToken = default);

    Task<EventCountsDto> GetCountsAsync(CancellationToken cancellationToken = default);

    Task<EventDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<EventDto> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default);

    Task<EventDto?> UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken cancellationToken = default);

    Task<bool> SetArchivedAsync(Guid id, bool archived, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Ikke-arkiverede begivenheder, der ikke er slut før fromDate, de næste først.</summary>
    Task<List<EventDto>> GetUpcomingAsync(DateOnly fromDate, int take, CancellationToken cancellationToken = default);
}
