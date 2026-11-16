using Microsoft.AspNetCore.Hosting;

namespace web.Tests.Infrastructure;

/// <summary>
/// Som WebAppFactory, men som i drift bag en reverse proxy (step 16): miljøet er Production, og appen stoler på
/// proxyen 10.0.0.5 og netværket 192.168.50.0/24.
/// </summary>
public class ProxyWebAppFactory : WebAppFactory
{
    public const string KnownProxy = "10.0.0.5";
    public const string KnownNetwork = "192.168.50.0/24";

    protected override void ConfigureTestServices(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ForwardedHeaders:KnownProxies", KnownProxy);
        builder.UseSetting("ForwardedHeaders:KnownNetworks", KnownNetwork);
    }
}
