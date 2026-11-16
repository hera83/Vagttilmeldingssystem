namespace web.ViewModels.Profile;

/// <summary>
/// Dataudtræk (GDPR art. 15) som JSON-fil. Bruges både af "Hent mine data" (Min profil) og af administratorens
/// "Hent data" på en bruger. Tidspunkter i dansk tid som tekst.
/// </summary>
public class ExportProfileViewModel
{
    public string Beskrivelse { get; set; } = "Alle oplysninger, Vagttilmelding har om dig. Adgangskoden gemmes kun som et hash og er ikke med.";
    public string Hentet { get; set; } = string.Empty;
    public ExportAccountViewModel Konto { get; set; } = new();
    public ExportPersonalDataViewModel? Profil { get; set; }
    public bool EmailBeskeder { get; set; }
    public List<ExportShiftViewModel> Vagter { get; set; } = [];
    public List<ExportDeviceViewModel> EnhederMedPush { get; set; } = [];
    public List<ExportMessageViewModel> Beskeder { get; set; } = [];
}
