namespace web.Repositories.Shifts.Dtos;

/// <summary>En åben vagt (til forældrene) med begivenhedens navn og sted. Indeholder aldrig kladder.</summary>
public class OpenShiftDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public int Capacity { get; set; }
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventLocation { get; set; } = string.Empty;
}
