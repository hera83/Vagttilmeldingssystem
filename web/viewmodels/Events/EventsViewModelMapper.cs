using System.ComponentModel.DataAnnotations;
using web.Data.Models;
using web.Extensions;
using web.Managers.Planning.Dtos;
using web.Repositories.Events.Dtos;

namespace web.ViewModels.Events;

public static class EventsViewModelMapper
{
    public static EventSearchDto ToEventSearchDto(string? query, bool archived) => new()
    {
        Text = query,
        Archived = archived
    };

    public static IndexEventsViewModel ToIndexEventsViewModel(this List<EventListItemDto> events, EventCountsDto counts, string? query, bool archived) => new()
    {
        Events = events.Select(x => new EventListItemViewModel
        {
            Id = x.Event.Id,
            Name = x.Event.Name,
            Location = x.Event.Location,
            PeriodText = DateTimeExtensions.ToDanishPeriodText(x.Event.StartDate, x.Event.EndDate),
            IsArchived = x.Event.IsArchived,
            DraftCount = x.DraftCount,
            OpenCount = x.OpenCount
        }).ToList(),
        Query = query,
        ShowArchived = archived,
        ActiveCount = counts.Active,
        ArchivedCount = counts.Archived
    };

    public static DetailsEventsViewModel ToDetailsEventsViewModel(this EventOverviewDto overview, ShiftStatus? statusFilter) => new()
    {
        Id = overview.Event.Id,
        Name = overview.Event.Name,
        Description = overview.Event.Description,
        Location = overview.Event.Location,
        PeriodText = DateTimeExtensions.ToDanishPeriodText(overview.Event.StartDate, overview.Event.EndDate),
        IsArchived = overview.Event.IsArchived,
        Shifts = overview.Shifts.Select(x => new ShiftRowViewModel
        {
            Id = x.Id,
            Title = x.Title,
            DateText = x.StartUtc.ToDanishDateText(),
            TimeText = DateTimeExtensions.ToDanishTimeRangeText(x.StartUtc, x.EndUtc),
            Capacity = x.Capacity,
            Taken = overview.TakenCounts.GetValueOrDefault(x.Id),
            IsOpen = x.Status == ShiftStatus.Open
        }).ToList(),
        DraftCount = overview.DraftCount,
        OpenCount = overview.OpenCount,
        StatusFilter = statusFilter?.ToString()
    };

    /// <summary>Bekræftelsessiden. Er selectedIds null (GET), er alle kladder valgt.</summary>
    public static OpenEventsViewModel ToOpenEventsViewModel(this EventOverviewDto overview, IReadOnlyCollection<Guid>? selectedIds = null)
    {
        var drafts = overview.Shifts.Where(x => x.Status == ShiftStatus.Draft).ToList();
        var dates = drafts.Select(x => DateOnly.FromDateTime(x.StartUtc.ToDanishTime())).ToList();
        var question = dates.Count == 0
            ? string.Empty
            : dates.Min() == dates.Max()
                ? $"den {dates.Min().ToDanishDateText()}"
                : $"til perioden {dates.Min().ToDanishDateText()} til {dates.Max().ToDanishDateText()}";

        return new OpenEventsViewModel
        {
            ShiftIds = selectedIds?.ToList() ?? drafts.Select(x => x.Id).ToList(),
            EventId = overview.Event.Id,
            EventName = overview.Event.Name,
            PeriodQuestion = question,
            Drafts = drafts.Select(x => new OpenShiftOptionViewModel
            {
                Id = x.Id,
                Title = x.Title,
                DateText = x.StartUtc.ToDanishDateText(),
                TimeText = DateTimeExtensions.ToDanishTimeRangeText(x.StartUtc, x.EndUtc),
                Capacity = x.Capacity,
                Taken = overview.TakenCounts.GetValueOrDefault(x.Id),
                Selected = selectedIds is null || selectedIds.Contains(x.Id)
            }).ToList()
        };
    }

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

    /// <summary>Kolonnerne i deltagerlisten (CSV).</summary>
    public static readonly string[] ParticipantColumns =
        ["Vagt", "Dato", "Tid", "Navn", "Telefon", "E-mail", "Trøjestørrelse", "Væskebilletter", "Tivoliark", "Barnets navn", "Klasse", "Tilmeldt"];

    /// <summary>Deltagerlisten som CSV til dansk Excel (semikolon, CRLF). Kun admin.</summary>
    public static string ToParticipantsCsv(this ParticipantListDto list)
    {
        var builder = new System.Text.StringBuilder();
        builder.Append(CsvHelper.Line(ParticipantColumns)).Append(CsvHelper.NewLine);
        foreach (var row in list.Rows)
        {
            builder.Append(CsvHelper.Line(
            [
                row.ShiftTitle,
                row.StartUtc.ToDanishDateText(),
                DateTimeExtensions.ToDanishTimeRangeText(row.StartUtc, row.EndUtc),
                row.Name,
                row.Phone,
                row.Email,
                row.ShirtSize,
                row.DrinkTickets?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.TivoliSheets?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.ChildName,
                row.ChildClass,
                row.Source == AssignmentSource.Self ? "Selv" : "Af administrator"
            ])).Append(CsvHelper.NewLine);
        }

        return builder.ToString();
    }

    public static string ToParticipantsFileName(this ParticipantListDto list, DateTime nowUtc) =>
        $"deltagerliste-{list.Event.Name.ToFileNameSlug()}-{nowUtc.ToDanishTime():yyyy-MM-dd}.csv";
}
