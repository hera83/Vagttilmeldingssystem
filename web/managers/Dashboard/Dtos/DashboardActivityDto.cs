namespace web.Managers.Dashboard.Dtos;

/// <summary>Én linje i "Seneste aktivitet". Kun navne, ingen andre persondata.</summary>
public class DashboardActivityDto
{
    public DashboardActivityKind Kind { get; set; }
    public DateTime At { get; set; }
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? UserDisplayName { get; set; }
    public string? ShiftTitle { get; set; }

    /// <summary>Kun frigivelser: antal vagter og hvem der åbnede.</summary>
    public int ShiftCount { get; set; }

    public string? ByName { get; set; }
}
