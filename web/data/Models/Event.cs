namespace web.Data.Models;

/// <summary>En begivenhed, fx "Skolefest" eller "Arbejdslørdag". Vagterne hører under den.</summary>
public class Event : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Arkiverede begivenheder er afsluttede og skjules i listerne. De slettes ikke, så historikken bevares.</summary>
    public bool IsArchived { get; set; }
}
