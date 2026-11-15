using web.Data.Models;
using web.Extensions;
using web.Infrastructure.Notifications;
using web.Infrastructure.Realtime;
using web.Managers.Planning.Dtos;
using web.Managers.Planning.Interfaces;
using web.Repositories.Assignments.Interfaces;
using web.Repositories.Events.Dtos;
using web.Repositories.Events.Interfaces;
using web.Repositories.Profiles.Interfaces;
using web.Repositories.Shifts.Dtos;
using web.Repositories.Shifts.Interfaces;

namespace web.Managers.Planning;

public class PlanningManager(
    IEventRepository eventRepository,
    IShiftRepository shiftRepository,
    IAssignmentRepository assignmentRepository,
    IProfileRepository profileRepository,
    IShiftUpdatesNotifier notifier,
    NotificationSignal notificationSignal) : IPlanningManager
{
    public Task<EventDto?> GetEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        eventRepository.GetByIdAsync(eventId, cancellationToken);

    public async Task<List<EventListItemDto>> SearchEventsAsync(EventSearchDto search, CancellationToken cancellationToken = default)
    {
        var events = await eventRepository.SearchAsync(search, cancellationToken);
        var counts = (await shiftRepository.GetStatusCountsAsync(events.Select(x => x.Id), cancellationToken))
            .ToDictionary(x => x.EventId);
        return events.Select(x => new EventListItemDto
        {
            Event = x,
            DraftCount = counts.GetValueOrDefault(x.Id)?.DraftCount ?? 0,
            OpenCount = counts.GetValueOrDefault(x.Id)?.OpenCount ?? 0
        }).ToList();
    }

    public async Task<OpenShiftsResultDto> OpenShiftsAsync(Guid eventId, IReadOnlyCollection<Guid> shiftIds, Guid openedByUserId, CancellationToken cancellationToken = default)
    {
        var item = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (item is null)
        {
            return new OpenShiftsResultDto { Result = PlanningResult.EventNotFound };
        }

        if (item.IsArchived)
        {
            return new OpenShiftsResultDto { Result = PlanningResult.EventArchived };
        }

        var opened = await shiftRepository.OpenAsync(eventId, shiftIds, openedByUserId, cancellationToken);
        if (opened.Count > 0)
        {
            // Efter commit: alle åbne sider får besked på samme tid (krav K12).
            await notifier.PublishAsync(new ShiftUpdate { EventId = eventId, ShiftIds = opened, IsPublic = true, Opened = true }, cancellationToken);
            // Push til lukkede apps: NotificationWorker finder frigivelsen og lægger beskeder i køen (step 10).
            notificationSignal.Notify();
        }

        return new OpenShiftsResultDto { Result = PlanningResult.Success, OpenedShiftIds = opened };
    }

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
            OpenCount = shifts.Count(x => x.Status == ShiftStatus.Open),
            TakenCounts = await assignmentRepository.GetActiveCountsAsync(shifts.Select(x => x.Id), cancellationToken)
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

        var created = await shiftRepository.CreateAsync(eventId, dto, cancellationToken);
        await notifier.PublishAsync(new ShiftUpdate { EventId = eventId, ShiftIds = [created.Id], IsPublic = false }, cancellationToken);
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

        var result = await shiftRepository.UpdateAsync(shiftId, dto, cancellationToken);
        if (result.Status == ShiftChangeStatus.Success)
        {
            // Tid/kapacitet ændret: åbne sider og de personer, der er på vagten, får besked.
            var people = await assignmentRepository.GetActiveByShiftAsync(shiftId, cancellationToken);
            await notifier.PublishAsync(new ShiftUpdate
            {
                EventId = editor.Event.Id,
                ShiftIds = [shiftId],
                IsPublic = editor.Shift.Status == ShiftStatus.Open,
                AffectedUserIds = people.Select(x => x.UserId).ToList()
            }, cancellationToken);
        }

        return result.Status switch
        {
            ShiftChangeStatus.Success => PlanningResult.Success,
            ShiftChangeStatus.CapacityBelowTaken => PlanningResult.CapacityBelowTaken,
            _ => PlanningResult.ShiftNotFound
        };
    }

    public async Task<PlanningResult> DeleteShiftAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        var shift = await shiftRepository.GetByIdAsync(shiftId, cancellationToken);
        var result = await shiftRepository.DeleteAsync(shiftId, cancellationToken);
        if (result.Status == ShiftChangeStatus.Success && shift is not null)
        {
            await notifier.PublishAsync(new ShiftUpdate
            {
                EventId = shift.EventId,
                ShiftIds = [shiftId],
                IsPublic = shift.Status == ShiftStatus.Open
            }, cancellationToken);
        }

        return result.Status switch
        {
            ShiftChangeStatus.Success => PlanningResult.Success,
            ShiftChangeStatus.HasActiveAssignments => PlanningResult.ShiftHasAssignments,
            _ => PlanningResult.ShiftNotFound
        };
    }

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

    public async Task<ParticipantListDto?> GetParticipantsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var item = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (item is null)
        {
            return null;
        }

        var participants = await assignmentRepository.GetActiveForEventAsync(eventId, cancellationToken);
        var profiles = await profileRepository.GetByUserIdsAsync(participants.Select(x => x.UserId), cancellationToken);
        return new ParticipantListDto
        {
            Event = item,
            Rows = participants.Select(x =>
            {
                var profile = profiles.GetValueOrDefault(x.UserId);
                return new ParticipantRowDto
                {
                    ShiftTitle = x.ShiftTitle,
                    StartUtc = x.StartUtc,
                    EndUtc = x.EndUtc,
                    Name = string.IsNullOrWhiteSpace(profile?.FullName) ? x.DisplayName : profile.FullName,
                    Email = x.UserName,
                    Phone = profile?.Phone ?? string.Empty,
                    ShirtSize = profile?.ShirtSize ?? string.Empty,
                    DrinkTickets = profile?.DrinkTickets,
                    TivoliSheets = profile?.TivoliSheets,
                    ChildName = profile?.ChildName ?? string.Empty,
                    ChildClass = profile?.ChildClass ?? string.Empty,
                    Source = x.Source
                };
            }).ToList()
        };
    }
}
