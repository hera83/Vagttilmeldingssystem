using System.Threading.RateLimiting;
using web.Infrastructure.Authentication;

namespace web.Infrastructure.Security;

/// <summary>
/// Rate limiting (step 15) mod gætning af adgangskoder og misbrug af links og tilmelding. Faste vinduer pr. IP-adresse
/// (tilmelding pr. bruger). Grænserne står i RateLimiting:* og kan slås fra med RateLimiting:Enabled = false (kun til tests).
/// I drift bag en reverse proxy skal den rigtige klient-IP videregives (X-Forwarded-For), ellers deler alle én grænse.
/// </summary>
public static class RateLimitPolicies
{
    public const string Login = "login";
    public const string PasswordLinks = "password-links";
    public const string Enroll = "enroll";

    public static RateLimitPartition<string> For(HttpContext context, IConfiguration configuration, string policy, int defaultPermits, int defaultWindowSeconds)
    {
        if (!configuration.GetValue("RateLimiting:Enabled", true))
        {
            return RateLimitPartition.GetNoLimiter(policy);
        }

        var permits = configuration.GetValue($"RateLimiting:{Section(policy)}PermitLimit", defaultPermits);
        var window = TimeSpan.FromSeconds(configuration.GetValue($"RateLimiting:{Section(policy)}WindowSeconds", defaultWindowSeconds));
        var partition = policy == Enroll && UserPrincipalFactory.GetUserId(context.User) is { } userId
            ? $"user:{userId:N}"
            : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "ukendt"}";

        return RateLimitPartition.GetFixedWindowLimiter($"{policy}:{partition}", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = Math.Max(1, permits),
            Window = window,
            QueueLimit = 0
        });
    }

    private static string Section(string policy) => policy switch
    {
        Login => "Login",
        PasswordLinks => "Password",
        _ => "Enroll"
    };
}
