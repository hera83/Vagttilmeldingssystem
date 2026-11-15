using System.ComponentModel.DataAnnotations;

namespace web.ViewModels.Profile;

/// <summary>E-mail-beskeder til/fra (panelet Påmindelser og meddelelser på Min profil).</summary>
public class NotificationsProfileViewModel
{
    [Display(Name = "Send mig e-mails om nye vagter, bekræftelser og påmindelser")]
    public bool EmailNotifications { get; set; }
}
