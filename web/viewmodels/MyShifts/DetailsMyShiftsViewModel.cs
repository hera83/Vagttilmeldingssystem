namespace web.ViewModels.MyShifts;

public class DetailsMyShiftsViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public string DurationText { get; set; } = string.Empty;
    public bool IsAdminAssigned { get; set; }
    public bool IsPast { get; set; }

    /// <summary>Vagten er tildelt i det skjulte, før der er åbnet for tilmelding.</summary>
    public bool IsNotPublishedYet { get; set; }

    public string AssignedText { get; set; } = string.Empty;
}
