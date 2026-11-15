using System.ComponentModel.DataAnnotations;
using web.Extensions;
using web.Repositories.Events.Dtos;

namespace web.ViewModels.Events;

public static class EventsViewModelMapper
{
    public static EventSearchDto ToEventSearchDto(string? query, bool archived) => new()
    {
        Text = query,
        Archived = archived
    };

    public static IndexEventsViewModel ToIndexEventsViewModel(this List<EventDto> events, EventCountsDto counts, string? query, bool archived) => new()
    {
        Events = events.Select(x => new EventListItemViewModel
        {
            Id = x.Id,
            Name = x.Name,
            Location = x.Location,
            PeriodText = DateTimeExtensions.ToDanishPeriodText(x.StartDate, x.EndDate),
            IsArchived = x.IsArchived
        }).ToList(),
        Query = query,
        ShowArchived = archived,
        ActiveCount = counts.Active,
        ArchivedCount = counts.Archived
    };

    public static DetailsEventsViewModel ToDetailsEventsViewModel(this EventDto item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Description = item.Description,
        Location = item.Location,
        PeriodText = DateTimeExtensions.ToDanishPeriodText(item.StartDate, item.EndDate),
        IsArchived = item.IsArchived
    };

    public static CreateEventDto ToCreateEventDto(this CreateEventsViewModel model) => new()
    {
        Name = model.Name,
        Description = model.Description ?? string.Empty,
        Location = model.Location,
        StartDate = model.StartDate!.Value,
        EndDate = model.EndDate!.Value
    };

    public static EditEventsViewModel ToEditEventsViewModel(this EventDto item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Description = item.Description,
        Location = item.Location,
        StartDate = item.StartDate,
        EndDate = item.EndDate
    };

    public static UpdateEventDto ToUpdateEventDto(this EditEventsViewModel model) => new()
    {
        Name = model.Name,
        Description = model.Description ?? string.Empty,
        Location = model.Location,
        StartDate = model.StartDate!.Value,
        EndDate = model.EndDate!.Value
    };

    /// <summary>Fælles validering for Create og Edit: slutdato må ikke ligge før startdato.</summary>
    public static IEnumerable<ValidationResult> ValidatePeriod(DateOnly? start, DateOnly? end)
    {
        if (start is not null && end is not null && end < start)
        {
            yield return new ValidationResult("Slutdatoen må ikke ligge før startdatoen.", ["EndDate"]);
        }
    }
}
