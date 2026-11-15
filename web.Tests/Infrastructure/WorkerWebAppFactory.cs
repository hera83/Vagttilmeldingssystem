using Microsoft.AspNetCore.Hosting;

namespace web.Tests.Infrastructure;

/// <summary>Som WebAppFactory, men NotificationWorker (interval 1 sekund) og ReminderWorker (scanner ved opstart) kører.</summary>
public class WorkerWebAppFactory : WebAppFactory
{
    protected override void ConfigureTestServices(IWebHostBuilder builder)
    {
        builder.UseSetting("Notifications:WorkerEnabled", "true");
        builder.UseSetting("Notifications:WorkerIntervalSeconds", "1");
        builder.UseSetting("Reminders:Enabled", "true");
    }
}
