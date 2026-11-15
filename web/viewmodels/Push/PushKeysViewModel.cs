using System.ComponentModel.DataAnnotations;

namespace web.ViewModels.Push;

/// <summary>Nøglerne fra browserens PushSubscription.toJSON().keys.</summary>
public class PushKeysViewModel
{
    [Required(ErrorMessage = "Nøglen p256dh mangler.")]
    [StringLength(200, ErrorMessage = "Nøglen p256dh er for lang.")]
    public string P256dh { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nøglen auth mangler.")]
    [StringLength(100, ErrorMessage = "Nøglen auth er for lang.")]
    public string Auth { get; set; } = string.Empty;
}
