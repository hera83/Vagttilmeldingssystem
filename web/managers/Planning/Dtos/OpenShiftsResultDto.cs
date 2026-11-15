namespace web.Managers.Planning.Dtos;

public class OpenShiftsResultDto
{
    public PlanningResult Result { get; set; }

    /// <summary>De vagter, der blev åbnet nu. Tom, hvis de allerede var åbne (idempotent).</summary>
    public List<Guid> OpenedShiftIds { get; set; } = [];
}
