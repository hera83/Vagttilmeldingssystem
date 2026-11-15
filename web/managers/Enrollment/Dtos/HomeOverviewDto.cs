using web.Repositories.Assignments.Dtos;

namespace web.Managers.Enrollment.Dtos;

/// <summary>Til forsiden: profilstatus, brugerens næste vagt og hvor mange åbne vagter der har ledige pladser.</summary>
public class HomeOverviewDto
{
    public bool ProfileComplete { get; set; }
    public MyShiftDto? NextShift { get; set; }
    public int UpcomingCount { get; set; }
    public int AvailableCount { get; set; }
}
