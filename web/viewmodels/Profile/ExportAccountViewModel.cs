namespace web.ViewModels.Profile;

public class ExportAccountViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Navn { get; set; } = string.Empty;
    public bool Administrator { get; set; }
    public bool Aktiv { get; set; }
    public string Oprettet { get; set; } = string.Empty;
    public string? SidsteLogin { get; set; }
}
