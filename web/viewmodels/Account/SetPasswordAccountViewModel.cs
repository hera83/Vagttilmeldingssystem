namespace web.ViewModels.Account;

/// <summary>Invitation: den nye bruger vælger sin første adgangskode.</summary>
public class SetPasswordAccountViewModel : PasswordLinkFormViewModel
{
    public override string FormAction => "SetPassword";
}
