namespace web.ViewModels.Account;

/// <summary>Glemt adgangskode: brugeren vælger en ny adgangskode.</summary>
public class ResetPasswordAccountViewModel : PasswordLinkFormViewModel
{
    public override string FormAction => "ResetPassword";
}
