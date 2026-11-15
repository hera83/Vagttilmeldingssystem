using System.Globalization;
using web.Data.Models;
using web.Extensions;
using web.Repositories.Assignments.Dtos;

namespace web.ViewModels.MyShifts;

public static class MyShiftsViewModelMapper
{
    public static IndexMyShiftsViewModel ToIndexMyShiftsViewModel(this List<MyShiftDto> shifts, string? month, DateTime nowUtc)
    {
        var items = shifts.Select(x => x.ToMyShiftItemViewModel(nowUtc)).ToList();
        return new IndexMyShiftsViewModel
        {
            Upcoming = items.Where(x => !x.IsPast).ToList(),
            // Afholdte: nyeste først.
            Past = items.Where(x => x.IsPast).Reverse().ToList(),
            Calendar = BuildCalendar(shifts, month, nowUtc)
        };
    }

    public static DetailsMyShiftsViewModel ToDetailsMyShiftsViewModel(this MyShiftDto shift, DateTime nowUtc)
    {
        var duration = shift.EndUtc - shift.StartUtc;
        return new DetailsMyShiftsViewModel
        {
            Title = shift.Title,
            Description = shift.Description,
            EventName = shift.EventName,
            Location = shift.EventLocation,
            DateText = shift.StartUtc.ToDanishDateText(),
            TimeText = DateTimeExtensions.ToDanishTimeRangeText(shift.StartUtc, shift.EndUtc),
            DurationText = duration.Minutes == 0 ? $"{(int)duration.TotalHours} timer" : $"{(int)duration.TotalHours} t. {duration.Minutes} min.",
            IsAdminAssigned = shift.Source == AssignmentSource.Admin,
            IsPast = shift.EndUtc < nowUtc,
            IsNotPublishedYet = shift.ShiftStatus == ShiftStatus.Draft,
            AssignedText = shift.AssignedAt.ToDanishDateTimeText()
        };
    }

    private static MyShiftItemViewModel ToMyShiftItemViewModel(this MyShiftDto shift, DateTime nowUtc)
    {
        var start = shift.StartUtc.ToDanishTime();
        return new MyShiftItemViewModel
        {
            ShiftId = shift.ShiftId,
            Title = shift.Title,
            EventName = shift.EventName,
            Location = shift.EventLocation,
            WeekdayShort = start.ToString("ddd", DateTimeExtensions.Danish).TrimEnd('.').ToUpperInvariant(),
            DayNumber = start.Day.ToString(CultureInfo.InvariantCulture),
            MonthShort = start.ToString("MMM", DateTimeExtensions.Danish).TrimEnd('.').ToUpperInvariant(),
            DateText = shift.StartUtc.ToDanishDateText(),
            TimeText = DateTimeExtensions.ToDanishTimeRangeText(shift.StartUtc, shift.EndUtc),
            IsAdminAssigned = shift.Source == AssignmentSource.Admin,
            IsPast = shift.EndUtc < nowUtc
        };
    }

    private static MyShiftsCalendarViewModel BuildCalendar(List<MyShiftDto> shifts, string? month, DateTime nowUtc)
    {
        var today = DateOnly.FromDateTime(nowUtc.ToDanishTime());
        var firstUpcoming = shifts.FirstOrDefault(x => x.EndUtc >= nowUtc);
        var defaultMonth = firstUpcoming is null ? today : DateOnly.FromDateTime(firstUpcoming.StartUtc.ToDanishTime());
        var first = TryParseMonth(month) ?? new DateOnly(defaultMonth.Year, defaultMonth.Month, 1);

        var byDay = shifts
            .GroupBy(x => DateOnly.FromDateTime(x.StartUtc.ToDanishTime()))
            .ToDictionary(g => g.Key, g => (Upcoming: g.Count(x => x.EndUtc >= nowUtc), Past: g.Count(x => x.EndUtc < nowUtc)));

        // Mandag er første ugedag.
        var start = first.AddDays(-(((int)first.DayOfWeek + 6) % 7));
        var last = first.AddMonths(1).AddDays(-1);
        var end = last.AddDays(6 - (((int)last.DayOfWeek + 6) % 7));

        var weeks = new List<List<CalendarDayViewModel>>();
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            if (weeks.Count == 0 || weeks[^1].Count == 7)
            {
                weeks.Add([]);
            }

            var counts = byDay.GetValueOrDefault(day);
            weeks[^1].Add(new CalendarDayViewModel
            {
                Day = day.Day,
                DateText = day.ToDanishDateText(),
                InMonth = day.Month == first.Month,
                IsToday = day == today,
                UpcomingCount = counts.Upcoming,
                PastCount = counts.Past
            });
        }

        var title = first.ToString("MMMM yyyy", DateTimeExtensions.Danish);
        return new MyShiftsCalendarViewModel
        {
            MonthTitle = char.ToUpperInvariant(title[0]) + title[1..],
            PreviousMonth = first.AddMonths(-1).ToString("yyyy'-'MM", CultureInfo.InvariantCulture),
            NextMonth = first.AddMonths(1).ToString("yyyy'-'MM", CultureInfo.InvariantCulture),
            Weeks = weeks
        };
    }

    private static DateOnly? TryParseMonth(string? month) =>
        DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;
}
