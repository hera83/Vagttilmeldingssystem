using web.Managers.AccountRecovery.Dtos;

namespace web.ViewModels.Account;

public static class AccountViewModelMapper
{
    /// <summary>Forklaring til brugeren, når linket ikke kan bruges, eller null.</summary>
    public static string? ToLinkProblem(this PasswordLinkStatus status) => status switch
    {
        PasswordLinkStatus.Valid => null,
        PasswordLinkStatus.Expired => "Linket er udløbet.",
        PasswordLinkStatus.Used => "Linket er allerede brugt.",
        _ => "Linket er ugyldigt."
    };
}
