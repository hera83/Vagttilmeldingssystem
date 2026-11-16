using Microsoft.AspNetCore.Hosting;

namespace web.Tests.Infrastructure;

/// <summary>Som WebAppFactory, men med rate limiting slået til og lave grænser (login 3, adgangskode-links 2, tilmelding 2).</summary>
public class RateLimitWebAppFactory : WebAppFactory
{
    protected override void ConfigureTestServices(IWebHostBuilder builder)
    {
        builder.UseSetting("RateLimiting:Enabled", "true");
        builder.UseSetting("RateLimiting:LoginPermitLimit", "3");
        builder.UseSetting("RateLimiting:PasswordPermitLimit", "2");
        builder.UseSetting("RateLimiting:EnrollPermitLimit", "2");
    }
}
