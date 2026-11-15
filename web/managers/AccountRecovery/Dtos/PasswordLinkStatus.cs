namespace web.Managers.AccountRecovery.Dtos;

/// <summary>Status for et link til at vælge adgangskode.</summary>
public enum PasswordLinkStatus
{
    Valid,

    /// <summary>Findes ikke, har et andet formål, eller brugeren findes ikke længere.</summary>
    Invalid,

    Expired,
    Used
}
