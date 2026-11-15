using web.Data.Models;

namespace web.Managers.Planning.Dtos;

/// <summary>Én række i deltagerlisten: en person på en vagt med profilens stamdata (kun admin).</summary>
public class ParticipantRowDto
{
    public string ShiftTitle { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    /// <summary>Profilens fulde navn, eller brugerens navn, hvis profilen ikke er udfyldt.</summary>
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ShirtSize { get; set; } = string.Empty;
    public int? DrinkTickets { get; set; }
    public int? TivoliSheets { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public string ChildClass { get; set; } = string.Empty;
    public AssignmentSource Source { get; set; }
}
