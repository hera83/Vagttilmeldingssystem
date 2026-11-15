namespace web.Repositories.Events.Dtos;

public class EventSearchDto
{
    /// <summary>Tekst i navn eller sted. Tom = alle.</summary>
    public string? Text { get; set; }

    /// <summary>false = aktive, true = arkiverede.</summary>
    public bool Archived { get; set; }
}
