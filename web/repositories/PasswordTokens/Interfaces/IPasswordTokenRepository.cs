using web.Repositories.PasswordTokens.Dtos;

namespace web.Repositories.PasswordTokens.Interfaces;

public interface IPasswordTokenRepository
{
    /// <summary>Laver et nyt engangslink. Brugerens ubrugte links med samme formål slettes, så kun det nyeste virker.</summary>
    Task<IssuedPasswordTokenDto> CreateAsync(CreatePasswordTokenDto dto, CancellationToken cancellationToken = default);

    /// <summary>Slår et link op ud fra det rå token (også brugte og udløbne), eller null.</summary>
    Task<PasswordTokenDto?> FindAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Bruger linket: true kun for den første, der gør det, og kun før det udløber.</summary>
    Task<bool> ConsumeAsync(Guid id, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Sletter alle brugerens ubrugte links (fx når adgangskoden er sat).</summary>
    Task<int> DeleteUnusedForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
