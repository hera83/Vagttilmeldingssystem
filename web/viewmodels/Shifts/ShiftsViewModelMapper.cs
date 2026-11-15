using System.ComponentModel.DataAnnotations;
using web.Data.Models;
using web.Extensions;
using web.Managers.Planning.Dtos;
using web.Repositories.Events.Dtos;
using web.Repositories.Shifts.Dtos;

namespace web.ViewModels.Shifts;

public static class ShiftsViewModelMapper
{
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(12);

    public static CreateShiftsViewModel ToCreateShiftsViewModel(this EventDto item)
    {
        var model = new CreateShiftsViewModel { Date = item.StartDate };
        model.FillDisplayFields(item);
        return model;
    }

    public static void FillDisplayFields(this CreateShiftsViewModel model, EventDto item)
    {
        model.EventId = item.Id;
        model.EventName = item.Name;
        model.EventPeriodText = DateTimeExtensions.ToDanishPeriodText(item.StartDate, item.EndDate);
    }

    public static CreateShiftDto ToCreateShiftDto(this CreateShiftsViewModel model)
    {
        var (startUtc, endUtc) = ToUtcRange(model.Date!.Value, model.StartTime!.Value, model.EndTime!.Value);
        return new CreateShiftDto
        {
            Title = model.Title,
            Description = model.Description ?? string.Empty,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Capacity = model.Capacity!.Value
        };
    }

    public static EditShiftsViewModel ToEditShiftsViewModel(this ShiftEditorDto editor)
    {
        var start = editor.Shift.StartUtc.ToDanishTime();
        var end = editor.Shift.EndUtc.ToDanishTime();
        var model = new EditShiftsViewModel
        {
            Title = editor.Shift.Title,
            Description = editor.Shift.Description,
            Date = DateOnly.FromDateTime(start),
            StartTime = TimeOnly.FromDateTime(start),
            EndTime = TimeOnly.FromDateTime(end),
            Capacity = editor.Shift.Capacity
        };
        model.FillDisplayFields(editor);
        return model;
    }

    public static void FillDisplayFields(this EditShiftsViewModel model, ShiftEditorDto editor)
    {
        model.Id = editor.Shift.Id;
        model.EventId = editor.Event.Id;
        model.EventName = editor.Event.Name;
        model.EventPeriodText = DateTimeExtensions.ToDanishPeriodText(editor.Event.StartDate, editor.Event.EndDate);
        model.IsOpen = editor.Shift.Status == ShiftStatus.Open;
    }

    public static UpdateShiftDto ToUpdateShiftDto(this EditShiftsViewModel model)
    {
        var (startUtc, endUtc) = ToUtcRange(model.Date!.Value, model.StartTime!.Value, model.EndTime!.Value);
        return new UpdateShiftDto
        {
            Title = model.Title,
            Description = model.Description ?? string.Empty,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Capacity = model.Capacity!.Value
        };
    }

    public static string OutsidePeriodMessage(EventDto item) =>
        $"Datoen skal ligge i begivenhedens periode ({DateTimeExtensions.ToDanishPeriodText(item.StartDate, item.EndDate)}).";

    /// <summary>
    /// Fælles validering af dato og tider i dansk tid. Et sluttidspunkt før starttidspunktet betyder, at vagten slutter efter midnat.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateTimes(DateOnly? date, TimeOnly? start, TimeOnly? end)
    {
        if (date is null || start is null || end is null)
        {
            yield break;
        }

        if (start == end)
        {
            yield return new ValidationResult("Sluttidspunktet skal være et andet end starttidspunktet.", ["EndTime"]);
            yield break;
        }

        var startLocal = date.Value.ToDateTime(start.Value);
        var endLocal = EndLocal(date.Value, start.Value, end.Value);
        if (endLocal - startLocal > MaxDuration)
        {
            yield return new ValidationResult($"En vagt kan højst vare {MaxDuration.TotalHours:0} timer. Er sluttidspunktet før starttidspunktet, slutter vagten næste dag.", ["EndTime"]);
        }

        if (startLocal.FromDanishTimeToUtc() is null)
        {
            yield return new ValidationResult("Starttidspunktet findes ikke den dag på grund af skift til sommertid. Vælg et andet tidspunkt.", ["StartTime"]);
        }

        if (endLocal.FromDanishTimeToUtc() is null)
        {
            yield return new ValidationResult("Sluttidspunktet findes ikke den dag på grund af skift til sommertid. Vælg et andet tidspunkt.", ["EndTime"]);
        }
    }

    private static DateTime EndLocal(DateOnly date, TimeOnly start, TimeOnly end) =>
        end > start ? date.ToDateTime(end) : date.AddDays(1).ToDateTime(end);

    private static (DateTime StartUtc, DateTime EndUtc) ToUtcRange(DateOnly date, TimeOnly start, TimeOnly end) =>
        (date.ToDateTime(start).FromDanishTimeToUtc()!.Value, EndLocal(date, start, end).FromDanishTimeToUtc()!.Value);
}
