namespace web.Data.Models;

/// <summary>En vagt under en begivenhed, med tidsrum og maksimalt antal personer.</summary>
public class Shift : EntityBase
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public int Capacity { get; set; }

    /// <summary>Nye vagter er altid Draft. Kun frigivelsen må sætte Open.</summary>
    public ShiftStatus Status { get; set; } = ShiftStatus.Draft;

    public DateTime? OpenedAt { get; set; }
    public Guid? OpenedByUserId { get; set; }
}
