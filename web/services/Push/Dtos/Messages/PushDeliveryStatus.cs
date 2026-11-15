namespace web.Services.Push.Dtos.Messages;

public enum PushDeliveryStatus
{
    /// <summary>Push-tjenesten har modtaget beskeden.</summary>
    Delivered,

    /// <summary>Abonnementet findes ikke længere (404/410). Det skal slettes.</summary>
    SubscriptionGone
}
