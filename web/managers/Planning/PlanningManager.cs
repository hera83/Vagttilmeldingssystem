using web.Data.Models;
using web.Extensions;
using web.Managers.Planning.Dtos;
using web.Managers.Planning.Interfaces;
using web.Repositories.Events.Dtos;
using web.Repositories.Events.Interfaces;
using web.Repositories.Shifts.Dtos;
using web.Repositories.Shifts.Interfaces;

namespace web.Managers.Planning;

public class PlanningManager(IEventRepository eventRepository, IShiftRepository shiftRepository) : IPlanningManager
{
    public Task<EventDto?> GetEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        eventRepository.GetByIdAsync(eventId, cancellationToken);

    public async Task<EventOverviewDto?> GetEventOverviewAsync(Guid eventId, ShiftStatus? statusFilter = null, CancellationToken cancellationToken = default)
    {
        var item = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (item is null)
        {
            return null;
        }

        var shifts = await shiftRepository.GetByEventIdAsync(eventId, cancellationToken: cancellationToken);
        return new EventOverviewDto
        {
            Event = item,
            Shifts = statusFilter is null ? shifts : shifts.Where(x => x.Status == statusFilter).ToList(),
            DraftCount = shifts.Count(x => x.Status == ShiftStatus.Draft),
            OpenCount = shifts.Count(x => x.Status == ShiftStatus.Open)
        };
    }

    public async Task<ShiftEditorDto?> GetShiftEditorAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        var shift = await shiftRepository.GetByIdAsync(shiftId, cancellationToken);
        if (shift is null)
        {
            return null;
        }

        var item = await eventRepository.GetByIdAsync(shift.EventId, cancellationToken);
        return item is null ? null : new ShiftEditorDto { Event = item, Shift = shift };
    }

    public async Task<PlanningResult> CreateShiftAsync(Guid eventId, CreateShiftDto dto, CancellationToken cancellationToken = default)
    {
        var item = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (item is null)
        {
            return PlanningResult.EventNotFound;
        }

        if (!IsWithinPeriod(item, dto.StartUtc))
        {
            return PlanningResult.OutsideEventPeriod;
        }

        await shiftRepository.CreateAsync(eventId, dto, cancellationToken);
        return PlanningResult.Success;
    }

    public async Task<PlanningResult> UpdateShiftAsync(Guid shiftId, UpdateShiftDto dto, CancellationToken cancellationToken = default)
    {
        var editor = await GetShiftEditorAsync(shiftId, cancellationToken);
        if (editor is null)
        {
            return PlanningResult.ShiftNotFound;
        }

        if (!IsWithinPeriod(editor.Event, dto.StartUtc))
        {
            return PlanningResult.OutsideEventPeriod;
        }

        return await shiftRepository.UpdateAsync(shiftId, dto, cancellationToken) is null
            ? PlanningResult.ShiftNotFound
            : PlanningResult.Success;
    }

    public async Task<PlanningResult> DeleteShiftAsync(Guid shiftId, CancellationToken cancellationToken = default) =>
        await shiftRepository.DeleteAsync(shiftId, cancellationToken) ? PlanningResult.Success : PlanningResult.ShiftNotFound;

    public async Task<PlanningResult> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        if (await shiftRepository.AnyForEventAsync(eventId, cancellationToken))
        {
            return PlanningResult.EventHasShifts;
        }

        return await eventRepository.DeleteAsync(eventId, cancellationToken) ? PlanningResult.Success : PlanningResult.EventNotFound;
    }

    // Vagten skal starte på en dag (dansk tid) inden for begivenhedens periode.
    private static bool IsWithinPeriod(EventDto item, DateTime startUtc)
    {
        var startDate = DateOnly.FromDateTime(startUtc.ToDanishTime());
        return startDate >= item.StartDate && startDate <= item.EndDate;
    }
}
