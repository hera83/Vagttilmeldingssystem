using System.ComponentModel.DataAnnotations;

namespace web.ViewModels.Push;

/// <summary>Browserens PushSubscription.toJSON(), sendt som JSON af push.js.</summary>
public class SubscribePushViewModel
{
    [Required(ErrorMessage = "Adressen mangler.")]
    [StringLength(2048, ErrorMessage = "Adressen er for lang.")]
    public string Endpoint { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nøglerne mangler.")]
    public PushKeysViewModel Keys { get; set; } = new();
}
