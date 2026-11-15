using web.Data.Models;
using web.Managers.Planning.Dtos;
using web.Repositories.Events.Dtos;
using web.Repositories.Shifts.Dtos;

namespace web.Managers.Planning.Interfaces;

public interface IPlanningManager
{
    Task<EventDto?> GetEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<List<EventListItemDto>> SearchEventsAsync(EventSearchDto search, CancellationToken cancellationToken = default);

    /// <summary>Frigivelsen: åbner de valgte kladder i begivenheden for tilmelding i én transaktion.</summary>
    Task<OpenShiftsResultDto> OpenShiftsAsync(Guid eventId, IReadOnlyCollection<Guid> shiftIds, Guid openedByUserId, CancellationToken cancellationToken = default);

    Task<EventOverviewDto?> GetEventOverviewAsync(Guid eventId, ShiftStatus? statusFilter = null, CancellationToken cancellationToken = default);

    Task<ShiftEditorDto?> GetShiftEditorAsync(Guid shiftId, CancellationToken cancellationToken = default);

    Task<PlanningResult> CreateShiftAsync(Guid eventId, CreateShiftDto dto, CancellationToken cancellationToken = default);

    Task<PlanningResult> UpdateShiftAsync(Guid shiftId, UpdateShiftDto dto, CancellationToken cancellationToken = default);

    Task<PlanningResult> DeleteShiftAsync(Guid shiftId, CancellationToken cancellationToken = default);

    Task<PlanningResult> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default);
}
