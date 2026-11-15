namespace web.ViewModels.Events;

/// <summary>Bekræftelsessiden "Åbn for tilmelding" (GET viser, POST modtager de valgte vagter).</summary>
public class OpenEventsViewModel
{
    /// <summary>De kladder, der skal åbnes. Sendes fra afkrydsningsfelterne.</summary>
    public List<Guid> ShiftIds { get; set; } = [];

    /// <summary>Kun til visning.</summary>
    public Guid EventId { get; set; }

    /// <summary>Kun til visning.</summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>Kun til visning: spørgsmålet med perioden, fx "til perioden fre. 12. mar. 2027 til lør. 13. mar. 2027".</summary>
    public string PeriodQuestion { get; set; } = string.Empty;

    /// <summary>Kun til visning.</summary>
    public List<OpenShiftOptionViewModel> Drafts { get; set; } = [];
}
