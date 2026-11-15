using System.ComponentModel.DataAnnotations;

namespace web.ViewModels.Push;

public class UnsubscribePushViewModel
{
    [Required(ErrorMessage = "Adressen mangler.")]
    [StringLength(2048, ErrorMessage = "Adressen er for lang.")]
    public string Endpoint { get; set; } = string.Empty;
}
