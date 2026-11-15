using System.Security.Cryptography;

namespace web.Services.Push;

/// <summary>Laver et VAPID-nøglepar (P-256) til Push:PublicKey og Push:PrivateKey. Bruges af flaget --generate-vapid-keys.</summary>
public static class VapidKeyGenerator
{
    public static (string PublicKey, string PrivateKey) Generate()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = key.ExportParameters(includePrivateParameters: true);

        // Offentlig nøgle: ukomprimeret punkt (0x04 || X || Y), 65 bytes. Privat nøgle: D, 32 bytes. Begge base64url.
        var publicKey = new byte[65];
        publicKey[0] = 0x04;
        parameters.Q.X!.CopyTo(publicKey, 1);
        parameters.Q.Y!.CopyTo(publicKey, 33);

        return (Base64Url(publicKey), Base64Url(parameters.D!));
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
