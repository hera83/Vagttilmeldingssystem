namespace web.Repositories.Shifts.Dtos;

/// <summary>Har bevidst ingen Status: status ændres kun af frigivelsen.</summary>
public class UpdateShiftDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public int Capacity { get; set; }
}
