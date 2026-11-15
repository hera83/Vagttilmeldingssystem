namespace web.Repositories.Shifts.Dtos;

/// <summary>Har bevidst ingen Status: nye vagter er altid kladder.</summary>
public class CreateShiftDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public int Capacity { get; set; }
}
