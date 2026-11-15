using web.Data.Models;
using web.Managers.AccountRecovery.Dtos;

namespace web.Managers.AccountRecovery.Interfaces;

public interface IAccountRecoveryManager
{
    /// <summary>Opretter brugeren med en ukendt adgangskode og sender en invitation (link gælder 7 dage).</summary>
    Task<InviteUserResultDto> InviteNewUserAsync(InviteUserDto dto, CancellationToken cancellationToken = default);

    /// <summary>Sender en ny invitation til en eksisterende bruger. Tidligere invitationslinks holder op med at virke.</summary>
    Task<SendInvitationResult> SendInvitationAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Glemt adgangskode: sender et link (1 time), hvis en aktiv bruger har e-mailen. Gør intet synligt ellers.</summary>
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);

    Task<PasswordLinkStatus> CheckLinkAsync(string token, PasswordTokenPurpose purpose, CancellationToken cancellationToken = default);

    /// <summary>Bruger linket og sætter adgangskoden. Andre sessioner logges ud (SecurityStamp skiftes). Valid = adgangskoden er sat.</summary>
    Task<PasswordLinkStatus> SetPasswordAsync(string token, PasswordTokenPurpose purpose, string newPassword, CancellationToken cancellationToken = default);
}
