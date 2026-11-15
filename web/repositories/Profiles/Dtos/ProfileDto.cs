namespace web.Repositories.Profiles.Dtos;

public class ProfileDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ShirtSize { get; set; } = string.Empty;
    public int DrinkTickets { get; set; }
    public int TivoliSheets { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public string ChildClass { get; set; } = string.Empty;

    /// <summary>Alle påkrævede felter er udfyldt (krav K02/K03).</summary>
    public bool IsComplete { get; set; }

    public DateTime UpdatedAt { get; set; }
}
