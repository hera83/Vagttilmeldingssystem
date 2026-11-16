namespace web.ViewModels.Profile;

public class ExportMessageViewModel
{
    public string Type { get; set; } = string.Empty;
    public string Kanal { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Emne { get; set; } = string.Empty;
    public string Oprettet { get; set; } = string.Empty;
    public string? Sendt { get; set; }
}
