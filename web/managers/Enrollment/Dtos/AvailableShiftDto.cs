using web.Repositories.Shifts.Dtos;

namespace web.Managers.Enrollment.Dtos;

/// <summary>En åben vagt med antal besatte pladser og om den indloggede bruger allerede er på.</summary>
public class AvailableShiftDto
{
    public OpenShiftDto Shift { get; set; } = new();
    public int Taken { get; set; }
    public bool IsMine { get; set; }
    public int Free => Math.Max(0, Shift.Capacity - Taken);
}
