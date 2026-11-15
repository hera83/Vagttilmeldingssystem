namespace web.Managers.Enrollment.Dtos;

public class AvailableOverviewDto
{
    public List<AvailableShiftDto> Shifts { get; set; } = [];
    public bool ProfileComplete { get; set; }
}
