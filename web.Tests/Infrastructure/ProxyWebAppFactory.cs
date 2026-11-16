using Microsoft.AspNetCore.Hosting;

namespace web.Tests.Infrastructure;

/// <summary>
/// Som WebAppFactory, men som i drift bag en reverse proxy (step 16 og 19): miljøet er Production. Private netværk er
/// betroet uden konfiguration. Oveni er en proxy med en offentlig IP-adresse (203.0.113.50) og netværket 100.64.0.0/10
/// sat i ForwardedHeaders:*, som en proxy uden for de private netværk ville kræve.
/// </summary>
public class ProxyWebAppFactory : WebAppFactory
{
    public const string ConfiguredProxy = "203.0.113.50";
    public const string ConfiguredNetwork = "100.64.0.0/10";

    protected override void ConfigureTestServices(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ForwardedHeaders:KnownProxies", ConfiguredProxy);
        builder.UseSetting("ForwardedHeaders:KnownNetworks", ConfiguredNetwork);
    }
}
