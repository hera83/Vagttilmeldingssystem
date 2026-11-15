using web.Extensions;
using web.Managers.Enrollment.Dtos;

namespace web.ViewModels.Home;

public static class HomeViewModelMapper
{
    public static IndexHomeViewModel ToIndexHomeViewModel(this HomeOverviewDto overview, DateTime nowUtc)
    {
        var next = overview.NextShift;
        return new IndexHomeViewModel
        {
            ProfileComplete = overview.ProfileComplete,
            UpcomingCount = overview.UpcomingCount,
            AvailableCount = overview.AvailableCount,
            HasNextShift = next is not null,
            NextShiftId = next?.ShiftId ?? Guid.Empty,
            NextShiftTitle = next?.Title ?? string.Empty,
            NextShiftEventName = next?.EventName ?? string.Empty,
            NextShiftLocation = next?.EventLocation ?? string.Empty,
            NextShiftDateText = next?.StartUtc.ToDanishDateText() ?? string.Empty,
            NextShiftTimeText = next is null ? string.Empty : DateTimeExtensions.ToDanishTimeRangeText(next.StartUtc, next.EndUtc)
        };
    }
}
