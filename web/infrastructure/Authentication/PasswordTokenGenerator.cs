using System.Security.Cryptography;
using System.Text;

namespace web.Infrastructure.Authentication;

/// <summary>Engangslinks til at vælge adgangskode: 32 tilfældige bytes (base64url) i linket, kun SHA-256-hashet i databasen.</summary>
public static class PasswordTokenGenerator
{
    public static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>Tilfældig adgangskode til en inviteret bruger, som ingen kender. Brugeren vælger sin egen via invitationen.</summary>
    public static string GenerateUnknownPassword() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
