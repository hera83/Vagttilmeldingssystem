using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using MimeKit;
using web.Services.Mail;
using web.Services.Mail.Dtos.Messages;

namespace web.Tests;

/// <summary>Den rigtige MailService uden en SMTP-server: pickup-mappen og fejloversættelsen.</summary>
public class MailServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vagttilmelding-mail-tests", Guid.NewGuid().ToString("N"));

    private MailService Create(Dictionary<string, string?> settings) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), new HostingEnvironment { ContentRootPath = _root });

    private static SendMailRequestDto Request(string to = "forælder@example.com") => new()
    {
        ToAddress = to,
        ToName = "Anne Forælder",
        Subject = "Bekræftelse: du er tilmeldt en vagt",
        TextBody = "Hej Anne\n\nDu er tilmeldt.",
        HtmlBody = "<p>Hej Anne</p>"
    };

    [Fact]
    public async Task Pickup_GemmerMailenSomEml()
    {
        var service = Create(new() { ["Mail:FromAddress"] = "system@example.com", ["Mail:FromName"] = "Vagttilmelding", ["Mail:PickupDirectory"] = "app_files/MailPickup" });

        Assert.True(await service.IsConfiguredAsync());
        var response = await service.SendAsync(Request());

        Assert.Equal(MailDelivery.Pickup, response.Delivery);
        var file = Assert.Single(Directory.GetFiles(Path.Combine(_root, "app_files", "MailPickup"), "*.eml"));
        var message = await MimeMessage.LoadAsync(file);
        Assert.Equal("system@example.com", message.From.Mailboxes.Single().Address);
        Assert.Equal("Vagttilmelding", message.From.Mailboxes.Single().Name);
        Assert.Equal("forælder@example.com", message.To.Mailboxes.Single().Address);
        Assert.Equal("Bekræftelse: du er tilmeldt en vagt", message.Subject);
        Assert.Equal("auto-generated", message.Headers["Auto-Submitted"]);
        Assert.Contains("Du er tilmeldt.", message.TextBody);
        Assert.Contains("<p>Hej Anne</p>", message.HtmlBody);
    }

    [Fact]
    public async Task UdenAfsender_Giver500MedNoeglensNavn()
    {
        var service = Create(new() { ["Mail:PickupDirectory"] = "app_files/MailPickup" });

        Assert.False(await service.IsConfiguredAsync());
        var ex = await Assert.ThrowsAsync<MailException>(() => service.SendAsync(Request()));
        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Contains("Mail:FromAddress", ex.Message);
    }

    [Fact]
    public async Task UdenSmtpHost_Giver500MedNoeglensNavn()
    {
        var service = Create(new() { ["Mail:FromAddress"] = "system@example.com" });

        var ex = await Assert.ThrowsAsync<MailException>(() => service.SendAsync(Request()));
        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Contains("Mail:Smtp:Host", ex.Message);
    }

    [Fact]
    public async Task UgyldigModtager_Giver400()
    {
        var service = Create(new() { ["Mail:FromAddress"] = "system@example.com", ["Mail:PickupDirectory"] = "x" });

        var ex = await Assert.ThrowsAsync<MailException>(() => service.SendAsync(Request("ikke en adresse")));
        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task BrugernavnUdenAdgangskode_Giver500()
    {
        var service = Create(new() { ["Mail:FromAddress"] = "system@example.com", ["Mail:Smtp:Host"] = "127.0.0.1", ["Mail:Smtp:UserName"] = "system" });

        var ex = await Assert.ThrowsAsync<MailException>(() => service.SendAsync(Request()));
        Assert.Contains("Mail:Smtp:Password", ex.Message);
    }

    [Fact]
    public async Task UkendtSikkerhed_Giver500()
    {
        var service = Create(new() { ["Mail:FromAddress"] = "system@example.com", ["Mail:Smtp:Host"] = "127.0.0.1", ["Mail:Smtp:Security"] = "Måske" });

        var ex = await Assert.ThrowsAsync<MailException>(() => service.SendAsync(Request()));
        Assert.Contains("Mail:Smtp:Security", ex.Message);
    }

    [Fact]
    public async Task SmtpServerKanIkkeNaas_Giver502()
    {
        var service = Create(new()
        {
            ["Mail:FromAddress"] = "system@example.com",
            ["Mail:Smtp:Host"] = "127.0.0.1",
            ["Mail:Smtp:Port"] = "1",
            ["Mail:Smtp:Security"] = "None",
            ["Mail:Smtp:UserName"] = "system",
            ["Mail:Smtp:Password"] = "hemmelig-adgangskode"
        });

        var ex = await Assert.ThrowsAsync<MailException>(() => service.SendAsync(Request()));
        Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
        Assert.Contains("Mail:Smtp:Host", ex.Message);
        Assert.DoesNotContain("hemmelig-adgangskode", ex.Message);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
    }
}
