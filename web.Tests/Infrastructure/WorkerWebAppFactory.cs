using Microsoft.AspNetCore.Hosting;

namespace web.Tests.Infrastructure;

/// <summary>Som WebAppFactory, men NotificationWorker kører (interval 1 sekund).</summary>
public class WorkerWebAppFactory : WebAppFactory
{
    protected override void ConfigureTestServices(IWebHostBuilder builder)
    {
        builder.UseSetting("Notifications:WorkerEnabled", "true");
        builder.UseSetting("Notifications:WorkerIntervalSeconds", "1");
    }
}
