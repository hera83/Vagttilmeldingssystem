using System.Net;
using web.Infrastructure.Mail;
using web.Services.Mail;
using web.Services.Mail.Dtos.Mailbox;
using web.Tests.Infrastructure;

namespace web.Tests;

/// <summary>Webmailen (step 18, krav K22) mod en postkasse i hukommelsen (FakeMailService).</summary>
public class MailboxTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public MailboxTests(WebAppFactory factory) => _factory = factory;

    private FakeMailService Mail => _factory.Mail;

    private async Task<HttpClient> AdminAsync()
    {
        var admin = _factory.CreateBrowser();
        await admin.LoginAsync(WebAppFactory.AdminUserName, WebAppFactory.AdminPassword);
        return admin;
    }

    private uint Incoming(string subject, string fromAddress, string? text = "Hej\nTak for mailen.", string? html = null, string? replyTo = null,
        params MailAttachmentDto[] files) =>
        Mail.AddMessage(MailFolder.Inbox, new MailMessageDto
        {
            From = new MailAddressDto { Name = "Afsender Test", Address = fromAddress },
            ReplyTo = replyTo is null ? null : new MailAddressDto { Name = "Svar her", Address = replyTo },
            To = [new MailAddressDto { Name = "Vagttilmelding", Address = FakeMailService.SystemAddress }],
            Subject = subject,
            TextBody = text,
            HtmlBody = html,
            DateUtc = DateTime.UtcNow.AddMinutes(-5),
            MessageId = $"{Guid.NewGuid():N}@forælder.test",
            References = ["tidligere@test.local"]
        }, files);

    [Fact]
    public async Task KunAdmin_HarAdgang_OgsaaTilPost()
    {
        var uid = Incoming($"Privat {Guid.NewGuid():N}", "ukendt@example.com");
        var user = await _factory.CreateUserAsync($"mailbox-{Guid.NewGuid():N}@test.local");
        using var parent = _factory.CreateBrowser();
        await parent.LoginAsync(user.UserName, WebAppFactory.UserPassword);

        foreach (var url in new[] { "/Mailbox", $"/Mailbox/Read/{uid}", "/Mailbox/Compose", $"/Mailbox/Attachment/{uid}?index=0" })
        {
            var response = await parent.GetAsync(url);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith("/Account/AccessDenied", response.Headers.Location?.PathAndQuery);
        }

        var delete = await parent.PostFormAsync("/Profile", $"/Mailbox/Delete/{uid}?folder=inbox");
        Assert.Equal(HttpStatusCode.Redirect, delete.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", delete.Headers.Location?.PathAndQuery);
        Assert.Contains(Mail.MessagesIn(MailFolder.Inbox), x => x.Uid == uid);
    }

    [Fact]
    public async Task Post_UdenAntiforgeryToken_Giver400()
    {
        var uid = Incoming($"Token {Guid.NewGuid():N}", "ukendt@example.com");
        using var admin = await AdminAsync();

        var response = await admin.PostAsync($"/Mailbox/Delete/{uid}?folder=inbox", new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(Mail.MessagesIn(MailFolder.Inbox), x => x.Uid == uid);
    }

    [Fact]
    public async Task Indbakke_ViserMails_GenkenderBrugere_OgSoeger()
    {
        var user = await _factory.CreateUserAsync($"kendt-{Guid.NewGuid():N}@test.local", displayName: "Karen Kendt");
        var marker = Guid.NewGuid().ToString("N")[..8];
        Incoming($"Spørgsmål om vagten {marker}", user.UserName);
        Incoming($"Reklame {marker}", "nyhedsbrev@example.com");
        using var admin = await AdminAsync();

        var html = await admin.GetHtmlAsync("/Mailbox");
        Assert.Contains($"Spørgsmål om vagten {marker}", html);
        Assert.Contains("Fra: Karen Kendt (bruger)", html);
        Assert.Contains("Ulæst", html);

        var search = await admin.GetHtmlAsync($"/Mailbox?folder=inbox&q=Reklame {marker}");
        Assert.Contains($"Reklame {marker}", search);
        Assert.DoesNotContain($"Spørgsmål om vagten {marker}", search);
    }

    [Fact]
    public async Task Laes_MarkererLaest_OgRenserHtml()
    {
        var html = "<p>Hej <b>admin</b></p><script>alert('x')</script><img src=\"https://tracker.example.com/pixel.gif\" onerror=\"alert(1)\">" +
                   "<a href=\"javascript:alert(2)\">klik</a><a href=\"https://skolen.dk\">skolen</a><form action=\"https://evil.example.com\"><input name=\"x\"></form>" +
                   "<p style=\"background:url(https://tracker.example.com/bg.gif)\" onclick=\"alert(3)\">tekst</p>";
        var uid = Incoming($"Html {Guid.NewGuid():N}", "html@example.com", text: null, html: html);
        using var admin = await AdminAsync();

        var page = await admin.GetHtmlAsync($"/Mailbox/Read/{uid}?folder=inbox");

        Assert.Contains("Hej <b>admin</b>", page);
        Assert.Contains("href=\"https://skolen.dk\" target=\"_blank\" rel=\"noopener noreferrer nofollow\"", page);
        Assert.DoesNotContain("alert(", page);
        Assert.DoesNotContain("tracker.example.com", page);
        Assert.DoesNotContain("javascript:", page);
        Assert.DoesNotContain("evil.example.com", page);
        Assert.True(Mail.MessagesIn(MailFolder.Inbox).Single(x => x.Uid == uid).Seen);
    }

    [Fact]
    public async Task Laes_AfsenderGenkendesMedLinkTilProfil()
    {
        var user = await _factory.CreateUserAsync($"afsender-{Guid.NewGuid():N}@test.local", displayName: "Anders Afsender");
        var uid = Incoming($"Fra bruger {Guid.NewGuid():N}", user.UserName.ToUpperInvariant());
        using var admin = await AdminAsync();

        var page = await admin.GetHtmlAsync($"/Mailbox/Read/{uid}?folder=inbox");

        Assert.Contains($"href=\"/Users/Profile/{user.Id}\"", page);
        Assert.Contains("Anders Afsender", page);
    }

    [Fact]
    public async Task Svar_FaarReOgTraadheaders_TilOprindeligAfsender_OgKopiISendt()
    {
        var subject = $"Kan jeg bytte? {Guid.NewGuid():N}";
        var uid = Incoming(subject, "forælder@example.com", text: "Hej\nKan jeg bytte vagt?", replyTo: "svar@example.com");
        var original = Mail.MessagesIn(MailFolder.Inbox).Single(x => x.Uid == uid);
        using var admin = await AdminAsync();

        var form = await admin.GetHtmlAsync($"/Mailbox/Reply/{uid}?folder=inbox");
        Assert.Contains($"value=\"Re: {subject}\"", form);
        Assert.Contains("&gt; Kan jeg bytte vagt?", form);
        Assert.Contains("svar@example.com", form);

        var response = await admin.PostFormAsync($"/Mailbox/Reply/{uid}?folder=inbox", $"/Mailbox/Reply/{uid}",
            ("Folder", "inbox"), ("Subject", $"Re: {subject}"), ("Body", "Desværre, vagterne er låst."), ("ToAddress", "angriber@example.com"));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var sent = Assert.Single(Mail.Sent, x => x.Subject == $"Re: {subject}");
        Assert.Equal("svar@example.com", sent.ToAddress);
        Assert.Equal(original.MessageId, sent.InReplyTo);
        Assert.Contains("tidligere@test.local", sent.References);
        Assert.True(sent.SaveCopyToSent);
        Assert.Contains(Mail.MessagesIn(MailFolder.Sent), x => x.Subject == $"Re: {subject}");

        // "Re:" gentages ikke.
        var again = Incoming($"Re: {subject}", "forælder@example.com");
        Assert.Contains($"value=\"Re: {subject}\"", await admin.GetHtmlAsync($"/Mailbox/Reply/{again}?folder=inbox"));
    }

    [Fact]
    public async Task NyMail_TilFriAdresse_OgUgyldigAdresseAfvises()
    {
        var subject = $"Husk mødet {Guid.NewGuid():N}";
        using var admin = await AdminAsync();

        var form = await admin.GetHtmlAsync("/Mailbox/Compose");
        Assert.Contains("<datalist id=\"recipients\">", form);
        Assert.Contains(WebAppFactory.AdminUserName, form);

        var invalid = await admin.PostFormAsync("/Mailbox/Compose", "/Mailbox/Compose", ("ToAddress", "ikke-en-adresse"), ("Subject", subject), ("Body", "Hej"));
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        Assert.Contains("Skriv en gyldig e-mailadresse.", await invalid.Content.ReadAsStringAsync());

        var response = await admin.PostFormAsync("/Mailbox/Compose", "/Mailbox/Compose", ("ToAddress", "ny@example.com"), ("Subject", subject), ("Body", "Hej\nVi ses."));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var sent = Assert.Single(Mail.Sent, x => x.Subject == subject);
        Assert.Equal("ny@example.com", sent.ToAddress);
        Assert.Null(sent.InReplyTo);
        Assert.Contains("Vi ses.", sent.HtmlBody);
    }

    [Fact]
    public async Task Slet_FlytterTilPapirkurv_OgSletterHeltDerfra()
    {
        var subject = $"Slet mig {Guid.NewGuid():N}";
        var uid = Incoming(subject, "slet@example.com");
        using var admin = await AdminAsync();

        await admin.PostFormAsync($"/Mailbox/Read/{uid}?folder=inbox", $"/Mailbox/Delete/{uid}?folder=inbox");
        Assert.DoesNotContain(Mail.MessagesIn(MailFolder.Inbox), x => x.Subject == subject);
        var trashed = Assert.Single(Mail.MessagesIn(MailFolder.Trash), x => x.Subject == subject);
        Assert.Contains(subject, await admin.GetHtmlAsync("/Mailbox?folder=trash"));

        await admin.PostFormAsync($"/Mailbox/Read/{trashed.Uid}?folder=trash", $"/Mailbox/Delete/{trashed.Uid}?folder=trash");
        Assert.DoesNotContain(Mail.MessagesIn(MailFolder.Trash), x => x.Subject == subject);
    }

    [Fact]
    public async Task MarkerSomUlaest()
    {
        var uid = Incoming($"Ulæst igen {Guid.NewGuid():N}", "ulaest@example.com");
        using var admin = await AdminAsync();
        await admin.GetHtmlAsync($"/Mailbox/Read/{uid}?folder=inbox");

        await admin.PostFormAsync($"/Mailbox/Read/{uid}?folder=inbox", $"/Mailbox/MarkUnread/{uid}?folder=inbox");

        Assert.False(Mail.MessagesIn(MailFolder.Inbox).Single(x => x.Uid == uid).Seen);
    }

    [Fact]
    public async Task Vedhaeftninger_HtmlHentesSomDownload()
    {
        var uid = Incoming($"Bilag {Guid.NewGuid():N}", "bilag@example.com", files:
        [
            new MailAttachmentDto { FileName = "program.pdf", ContentType = "application/pdf", Content = [1, 2, 3] },
            new MailAttachmentDto { FileName = "side.html", ContentType = "text/html", Content = "<script>alert(1)</script>"u8.ToArray() }
        ]);
        using var admin = await AdminAsync();

        Assert.Contains("program.pdf", await admin.GetHtmlAsync($"/Mailbox/Read/{uid}?folder=inbox"));
        var pdf = await admin.GetAsync($"/Mailbox/Attachment/{uid}?folder=inbox&index=0");
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        var page = await admin.GetAsync($"/Mailbox/Attachment/{uid}?folder=inbox&index=1");
        Assert.Equal("application/octet-stream", page.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", page.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/Mailbox/Attachment/{uid}?folder=inbox&index=5")).StatusCode);
    }

    [Fact]
    public async Task ImapFejl_GiverForstaaeligBesked_OgDashboardetVirker()
    {
        using var admin = await AdminAsync();
        Mail.MailboxError = new MailException(HttpStatusCode.BadGateway, "Kunne ikke forbinde til IMAP-serveren imap.test:993.");
        try
        {
            var page = await admin.GetHtmlAsync("/Mailbox");
            Assert.Contains("Postkassen kunne ikke nås: Kunne ikke forbinde til IMAP-serveren imap.test:993.", page);

            var dashboard = await admin.GetHtmlAsync("/Dashboard");
            Assert.Contains("Ulæste beskeder", dashboard);
            Assert.Contains("Postkassen kunne ikke nås", dashboard);
        }
        finally
        {
            Mail.MailboxError = null;
        }

        await admin.GetHtmlAsync("/Mailbox");
        Assert.Contains("i Indbakke", await admin.GetHtmlAsync("/Dashboard"));
    }

    [Fact]
    public async Task IkkeSatOp_GiverBesked()
    {
        using var admin = await AdminAsync();
        Mail.MailboxConfigured = false;
        try
        {
            Assert.Contains("Webmailen er ikke sat op.", await admin.GetHtmlAsync("/Mailbox"));
        }
        finally
        {
            Mail.MailboxConfigured = true;
        }
    }

    [Fact]
    public void Sanitizer_FjernerFarligtIndhold()
    {
        var sanitizer = new MailHtmlSanitizer();

        var clean = sanitizer.Sanitize("<div onmouseover=\"x()\">a<iframe src=\"https://evil\"></iframe><svg><script>x()</script></svg>" +
                                       "<img src=\"cid:logo\"><style>body{}</style><a href=\"mailto:a@b.dk\">mail</a></div>");

        Assert.Equal("<div>a<a href=\"mailto:a@b.dk\" target=\"_blank\" rel=\"noopener noreferrer nofollow\">mail</a></div>", clean);
    }
}
