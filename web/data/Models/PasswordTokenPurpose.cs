namespace web.Data.Models;

public enum PasswordTokenPurpose
{
    /// <summary>Ny bruger vælger sin første adgangskode (gælder 7 dage).</summary>
    Invitation,

    /// <summary>Glemt adgangskode (gælder 1 time).</summary>
    Reset
}
