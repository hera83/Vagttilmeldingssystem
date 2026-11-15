namespace web.Services.Mail.Dtos.Messages;

public enum MailDelivery
{
    /// <summary>Sendt via SMTP.</summary>
    Smtp,

    /// <summary>Gemt som .eml-fil i Mail:PickupDirectory (kun udvikling).</summary>
    Pickup
}
