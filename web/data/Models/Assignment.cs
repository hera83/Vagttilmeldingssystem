namespace web.Data.Models;

/// <summary>
/// En brugers plads på en vagt. Rækker slettes aldrig ved afmelding: de sættes til IsActive = false (historik).
/// Kun aktive tildelinger tæller med i kapaciteten.
/// </summary>
public class Assignment : EntityBase
{
    public Guid ShiftId { get; set; }
    public Guid UserId { get; set; }
    public AssignmentSource Source { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? AssignedByUserId { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }
}
