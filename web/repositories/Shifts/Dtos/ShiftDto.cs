using web.Data.Models;

namespace web.Repositories.Shifts.Dtos;

public class ShiftDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public int Capacity { get; set; }
    public ShiftStatus Status { get; set; }
    public DateTime? OpenedAt { get; set; }
}
