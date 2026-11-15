using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using web.Data;
using web.Data.Models;
using web.Infrastructure.Notifications;
using web.Managers.Notifications.Interfaces;
using web.Repositories.Shifts.Interfaces;
using web.Repositories.Users.Interfaces;
using web.Services.Mail;
using web.Tests.Infrastructure;

namespace web.Tests;

/// <summary>E-mail: invitation, glemt adgangskode, bekræftelser og frigivelser (step 11). Køen styres direkte (workeren er slået fra).</summary>
public class EmailTests : IClassFixture<WebAppFactory>
{
    private const string NewPassword = "Ny-Adgangskode-2027!";

    private readonly WebAppFactory _factory;

    public EmailTests(WebAppFactory factory) => _factory = factory;

    private Task<T> With<T>(Func<IServiceProvider, Task<T>> action) => _factory.WithServiceAsync<IServiceProvider, T>(action);

    private Task<int> SendDueAsync() => With(sp => sp.GetRequiredService<INotificationManager>().SendDueAsync());

    private static string NewEmail() => $"mail-{Guid.NewGuid():N}@test.local";

    private async Task<HttpClient> AdminAsync()
    {
        var admin = _factory.CreateBrowser();
        await admin.LoginAsync(WebAppFactory.AdminUserName, WebAppFactory.AdminPassword);
        return admin;
    }

    private Task<List<Notification>> QueueForAsync(Guid userId, string kind) =>
        With(sp => sp.GetRequiredService<WebDbContext>().Notifications.AsNoTracking()
            .Where(x => x.UserId == userId && x.Channel == NotificationChannel.Email && x.Kind == kind).ToListAsync());

    private async Task<Guid> UserIdAsync(string email) =>
        (await With(sp => sp.GetRequiredService<IUserRepository>().GetByUserNameAsync(email)))!.Id;

    private async Task<string> InviteAsync(string email, string displayName = "Ida Invitation")
    {
        using var admin = await AdminAsync();
        var response = await admin.PostFormAsync("/Users/Create", "/Users/Create",
            ("UserName", email), ("DisplayName", displayName), ("SendInvitation", "true"), ("SendInvitation", "false"));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await SendDueAsync();
        var mail = Assert.Single(_factory.Mail.SentTo(email));
        return FakeMailService.LinkIn(mail, "/Account/SetPassword") ?? throw new InvalidOperationException("Intet link i invitationen.");
    }

    private static async Task<HttpResponseMessage> SetPasswordAsync(HttpClient browser, string link, string action, string password)
    {
        var token = System.Web.HttpUtility.ParseQueryString(new Uri(new Uri(WebAppFactory.BaseUrl), link).Query)["token"]!;
        // Tokenet til anti-forgery hentes fra en anden anonym side, fordi siden med et brugt link ikke har en formular.
        return await browser.PostFormAsync("/Account/ForgotPassword", $"/Account/{action}", ("Token", token), ("NewPassword", password), ("ConfirmPassword", password));
    }

    [Fact]
    public async Task Invitation_MailMedLink_AdgangskodeSaettes_LoginVirker()
    {
        var email = NewEmail();
        var link = await InviteAsync(email);

        var mail = Assert.Single(_factory.Mail.SentTo(email));
        Assert.Equal("Du er inviteret til Vagttilmelding", mail.Subject);
        Assert.StartsWith("Hej Ida Invitation", mail.TextBody);
        Assert.Contains("7 dage", mail.TextBody);
        Assert.Contains("Vælg adgangskode", mail.HtmlBody);
        Assert.StartsWith($"{WebAppFactory.BaseUrl}/Account/SetPassword?token=", mail.TextBody.Split('\n').Single(x => x.StartsWith("http")));

        // Linket ligger ikke i databasen efter afsendelsen.
        var queued = Assert.Single(await QueueForAsync(await UserIdAsync(email), NotificationKinds.Invitation));
        Assert.Equal(NotificationStatus.Sent, queued.Status);
        Assert.Empty(queued.Body);
        Assert.Empty(queued.Url);

        using var browser = _factory.CreateBrowser();
        var page = await browser.GetHtmlAsync(link);
        Assert.Contains("Velkommen til Vagttilmelding", page);
        var response = await SetPasswordAsync(browser, link, "SetPassword", NewPassword);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/Login", response.Headers.Location?.OriginalString);

        await browser.LoginAsync(email, NewPassword);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task Invitationslink_KanKunBrugesEnGang()
    {
        var email = NewEmail();
        var link = await InviteAsync(email);
        using var browser = _factory.CreateBrowser();
        await SetPasswordAsync(browser, link, "SetPassword", NewPassword);

        Assert.Contains("Linket er allerede brugt.", await browser.GetHtmlAsync(link));
        var again = await SetPasswordAsync(browser, link, "SetPassword", "En-Helt-Anden-Kode-123");
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Contains("Linket er allerede brugt.", await again.Content.ReadAsStringAsync());

        // Den første adgangskode gælder stadig.
        await browser.LoginAsync(email, NewPassword);
    }

    [Fact]
    public async Task Invitationslink_Udloeber()
    {
        var email = NewEmail();
        var link = await InviteAsync(email);
        var userId = await UserIdAsync(email);
        await With(sp => sp.GetRequiredService<WebDbContext>().PasswordTokens.Where(x => x.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAt, DateTime.UtcNow.AddMinutes(-1))));

        using var browser = _factory.CreateBrowser();
        Assert.Contains("Linket er udløbet.", await browser.GetHtmlAsync(link));
    }

    [Fact]
    public async Task SendInvitationIgen_GoerDetGamleLinkUgyldigt()
    {
        var email = NewEmail();
        var oldLink = await InviteAsync(email);
        var userId = await UserIdAsync(email);
        using var admin = await AdminAsync();

        var response = await admin.PostFormAsync("/Users", $"/Users/Invite/{userId}");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await SendDueAsync();

        var mails = _factory.Mail.SentTo(email);
        Assert.Equal(2, mails.Count);
        var newLink = FakeMailService.LinkIn(mails[1], "/Account/SetPassword")!;
        Assert.NotEqual(oldLink, newLink);
        using var browser = _factory.CreateBrowser();
        Assert.Contains("Linket er ugyldigt.", await browser.GetHtmlAsync(oldLink));
        Assert.Contains("Velkommen til Vagttilmelding", await browser.GetHtmlAsync(newLink));
    }

    [Fact]
    public async Task Invitation_UdenMailOpsaetning_GiverFejlOgIngenBruger()
    {
        var email = NewEmail();
        using var admin = await AdminAsync();
        _factory.Mail.Configured = false;
        try
        {
            var response = await admin.PostFormAsync("/Users/Create", "/Users/Create",
                ("UserName", email), ("DisplayName", "Uden Mail"), ("SendInvitation", "true"), ("SendInvitation", "false"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Der kan ikke sendes e-mails", await response.Content.ReadAsStringAsync());
        }
        finally
        {
            _factory.Mail.Configured = true;
        }

        Assert.Null(await With(sp => sp.GetRequiredService<IUserRepository>().GetByUserNameAsync(email)));
    }

    [Fact]
    public async Task Opret_UdenInvitation_KraeverAdgangskode()
    {
        using var admin = await AdminAsync();
        var response = await admin.PostFormAsync("/Users/Create", "/Users/Create",
            ("UserName", NewEmail()), ("DisplayName", "Uden Kode"), ("SendInvitation", "false"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Skriv en adgangskode, eller vælg Send invitation på e-mail.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GlemtAdgangskode_UkendtEmail_SammeSvarOgIngenMail()
    {
        var unknown = NewEmail();
        var known = await _factory.CreateUserAsync(NewEmail());
        using var browser = _factory.CreateBrowser();

        var forUnknown = await browser.PostFormAsync("/Account/ForgotPassword", "/Account/ForgotPassword", ("Email", unknown));
        var unknownMessage = await browser.GetHtmlAsync(forUnknown.Headers.Location!.OriginalString);
        var forKnown = await browser.PostFormAsync("/Account/ForgotPassword", "/Account/ForgotPassword", ("Email", known.UserName));
        var knownMessage = await browser.GetHtmlAsync(forKnown.Headers.Location!.OriginalString);
        await SendDueAsync();

        Assert.Equal(HttpStatusCode.Redirect, forUnknown.StatusCode);
        Assert.Equal(forUnknown.Headers.Location, forKnown.Headers.Location);
        Assert.Contains("Hvis e-mailen findes i systemet", unknownMessage);
        Assert.Contains("Hvis e-mailen findes i systemet", knownMessage);
        Assert.Empty(_factory.Mail.SentTo(unknown));
        Assert.Single(_factory.Mail.SentTo(known.UserName));
    }

    [Fact]
    public async Task GlemtAdgangskode_NulstillerOgLoggerAndreSessionerUd()
    {
        var user = await _factory.CreateUserAsync(NewEmail());
        using var oldSession = _factory.CreateBrowser();
        await oldSession.LoginAsync(user.UserName, WebAppFactory.UserPassword);
        using var browser = _factory.CreateBrowser();

        await browser.PostFormAsync("/Account/ForgotPassword", "/Account/ForgotPassword", ("Email", user.UserName.ToUpperInvariant()));
        await SendDueAsync();
        var mail = Assert.Single(_factory.Mail.SentTo(user.UserName));
        Assert.Equal("Nulstil din adgangskode til Vagttilmelding", mail.Subject);
        Assert.Contains("60 minutter", mail.TextBody);
        var link = FakeMailService.LinkIn(mail, "/Account/ResetPassword")!;

        var response = await SetPasswordAsync(browser, link, "ResetPassword", NewPassword);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var old = await oldSession.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, old.StatusCode);
        Assert.StartsWith("/Account/Login", old.Headers.Location?.PathAndQuery);
        await Assert.ThrowsAsync<InvalidOperationException>(() => browser.LoginAsync(user.UserName, WebAppFactory.UserPassword));
        await browser.LoginAsync(user.UserName, NewPassword);
    }

    [Fact]
    public async Task Nulstillingslink_VirkerIkkeSomInvitation_OgNytLinkGoerDetGamleUgyldigt()
    {
        var user = await _factory.CreateUserAsync(NewEmail());
        using var browser = _factory.CreateBrowser();
        await browser.PostFormAsync("/Account/ForgotPassword", "/Account/ForgotPassword", ("Email", user.UserName));
        await SendDueAsync();
        var first = FakeMailService.LinkIn(Assert.Single(_factory.Mail.SentTo(user.UserName)), "/Account/ResetPassword")!;

        Assert.Contains("Linket er ugyldigt.", await browser.GetHtmlAsync(first.Replace("/Account/ResetPassword", "/Account/SetPassword")));

        await browser.PostFormAsync("/Account/ForgotPassword", "/Account/ForgotPassword", ("Email", user.UserName));
        await SendDueAsync();
        Assert.Contains("Linket er ugyldigt.", await browser.GetHtmlAsync(first));
    }

    [Fact]
    public async Task GlemtAdgangskode_DeaktiveretBruger_FaarIngenMail()
    {
        var user = await _factory.CreateUserAsync(NewEmail());
        await With(sp => sp.GetRequiredService<IUserRepository>().UpdateAsync(user.Id,
            new Repositories.Users.Dtos.UpdateUserDto { DisplayName = user.DisplayName, IsActive = false }));
        using var browser = _factory.CreateBrowser();

        await browser.PostFormAsync("/Account/ForgotPassword", "/Account/ForgotPassword", ("Email", user.UserName));
        await SendDueAsync();

        Assert.Empty(_factory.Mail.SentTo(user.UserName));
    }

    [Fact]
    public async Task Tilmelding_SenderBekraeftelseMedVagterne()
    {
        var user = await _factory.CreateUserAsync(NewEmail(), displayName: "Bo Bekræft");
        await _factory.CompleteProfileAsync(user.Id);
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 3, title: "Kagebod");
        await _factory.OpenShiftsAsync(shift.Id);
        using var browser = _factory.CreateBrowser();
        await browser.LoginAsync(user.UserName, WebAppFactory.UserPassword);

        var response = await browser.PostFormAsync("/Signup", "/Signup/Enroll", ("ShiftIds", shift.Id.ToString()));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await SendDueAsync();

        var mail = Assert.Single(_factory.Mail.SentTo(user.UserName));
        Assert.Equal("Bekræftelse: du er tilmeldt en vagt", mail.Subject);
        Assert.Contains("Kagebod", mail.TextBody);
        Assert.Contains(ev.Name, mail.TextBody);
        Assert.Contains(" kl. ", mail.TextBody);
        Assert.Contains($"{WebAppFactory.BaseUrl}/MyShifts", mail.TextBody);
        Assert.Contains("Se mine vagter", mail.HtmlBody);
    }

    [Fact]
    public async Task Tilmelding_Konflikt_GiverIngenBekraeftelse()
    {
        var other = await _factory.CreateUserAsync(NewEmail());
        var user = await _factory.CreateUserAsync(NewEmail());
        await _factory.CompleteProfileAsync(other.Id);
        await _factory.CompleteProfileAsync(user.Id);
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 1);
        await _factory.OpenShiftsAsync(shift.Id);
        using var first = _factory.CreateBrowser();
        await first.LoginAsync(other.UserName, WebAppFactory.UserPassword);
        await first.PostFormAsync("/Signup", "/Signup/Enroll", ("ShiftIds", shift.Id.ToString()));
        using var browser = _factory.CreateBrowser();
        await browser.LoginAsync(user.UserName, WebAppFactory.UserPassword);

        var response = await browser.PostFormAsync("/Signup", "/Signup/Enroll", ("ShiftIds", shift.Id.ToString()));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Empty(await QueueForAsync(user.Id, NotificationKinds.EnrollmentConfirmation));
    }

    [Fact]
    public async Task EmailBeskederFra_IngenBekraeftelseEllerFrigivelse_MenNulstillingSendes()
    {
        var user = await _factory.CreateUserAsync(NewEmail());
        await _factory.CompleteProfileAsync(user.Id);
        using var browser = _factory.CreateBrowser();
        await browser.LoginAsync(user.UserName, WebAppFactory.UserPassword);
        var off = await browser.PostFormAsync("/Profile", "/Profile/Notifications", ("EmailNotifications", "false"));
        Assert.Equal(HttpStatusCode.Redirect, off.StatusCode);
        Assert.Contains("Slå e-mails til", await browser.GetHtmlAsync("/Profile"));

        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 3);
        await OpenForRealAsync(ev.Id, shift.Id);
        await With(sp => sp.GetRequiredService<INotificationManager>().QueueOpenedShiftsAsync());
        await browser.PostFormAsync("/Signup", "/Signup/Enroll", ("ShiftIds", shift.Id.ToString()));
        await SendDueAsync();
        Assert.Empty(_factory.Mail.SentTo(user.UserName));

        using var anonymous = _factory.CreateBrowser();
        await anonymous.PostFormAsync("/Account/ForgotPassword", "/Account/ForgotPassword", ("Email", user.UserName));
        await SendDueAsync();
        Assert.Equal("Nulstil din adgangskode til Vagttilmelding", Assert.Single(_factory.Mail.SentTo(user.UserName)).Subject);
    }

    [Fact]
    public async Task Frigivelse_SenderMail_UdenKladdetitler()
    {
        var user = await _factory.CreateUserAsync(NewEmail());
        var ev = await _factory.CreateEventAsync();
        var first = await _factory.CreateShiftAsync(ev.Id, 2, title: "Bod 1");
        var second = await _factory.CreateShiftAsync(ev.Id, 2, title: "Bod 2");
        await _factory.CreateShiftAsync(ev.Id, 2, title: "Hemmelig kladde");
        await OpenForRealAsync(ev.Id, first.Id, second.Id);

        await With(sp => sp.GetRequiredService<INotificationManager>().QueueOpenedShiftsAsync());
        await SendDueAsync();

        var mail = Assert.Single(_factory.Mail.SentTo(user.UserName));
        Assert.Equal($"Nye vagter er åbne: {ev.Name}", mail.Subject);
        Assert.Contains("2 vagter", mail.TextBody);
        Assert.Contains($"{WebAppFactory.BaseUrl}/Signup?eventId={ev.Id}", mail.TextBody);
        Assert.DoesNotContain("Hemmelig", mail.TextBody + mail.HtmlBody);
        Assert.DoesNotContain("Bod 1", mail.TextBody);
    }

    [Fact]
    public async Task Mailfejl_ProeverIgen_OgAfvistModtagerOpgivesStraks()
    {
        var retry = await _factory.CreateUserAsync(NewEmail());
        var rejected = NewEmail();
        _factory.Mail.Behaviors[retry.UserName] = _ => throw new MailException(HttpStatusCode.BadGateway, "Kunne ikke forbinde til SMTP-serveren.");
        _factory.Mail.Behaviors[rejected] = _ => throw new MailException(HttpStatusCode.BadRequest, "SMTP-serveren afviste modtageradressen (550).");
        using var anonymous = _factory.CreateBrowser();
        await anonymous.PostFormAsync("/Account/ForgotPassword", "/Account/ForgotPassword", ("Email", retry.UserName));
        using var admin = await AdminAsync();
        await admin.PostFormAsync("/Users/Create", "/Users/Create",
            ("UserName", rejected), ("DisplayName", "Afvist"), ("SendInvitation", "true"), ("SendInvitation", "false"));

        await SendDueAsync();

        var pending = Assert.Single(await QueueForAsync(retry.Id, NotificationKinds.PasswordReset));
        Assert.Equal(NotificationStatus.Pending, pending.Status);
        Assert.Equal(1, pending.Attempts);
        Assert.NotEmpty(pending.Body);

        var failed = Assert.Single(await QueueForAsync(await UserIdAsync(rejected), NotificationKinds.Invitation));
        Assert.Equal(NotificationStatus.Failed, failed.Status);
        Assert.Empty(failed.Body);
    }

    [Fact]
    public async Task MailIkkeSatOp_BeskedenSpringesOver()
    {
        var user = await _factory.CreateUserAsync(NewEmail());
        using var anonymous = _factory.CreateBrowser();
        await anonymous.PostFormAsync("/Account/ForgotPassword", "/Account/ForgotPassword", ("Email", user.UserName));
        _factory.Mail.Configured = false;
        try
        {
            await SendDueAsync();
        }
        finally
        {
            _factory.Mail.Configured = true;
        }

        var skipped = Assert.Single(await QueueForAsync(user.Id, NotificationKinds.PasswordReset));
        Assert.Equal(NotificationStatus.Skipped, skipped.Status);
        Assert.Empty(skipped.Body);
    }

    private Task<List<Guid>> OpenForRealAsync(Guid eventId, params Guid[] shiftIds) =>
        With(async sp =>
        {
            var admin = (await sp.GetRequiredService<IUserRepository>().GetAllAsync()).Single(x => x.UserName == WebAppFactory.AdminUserName);
            return await sp.GetRequiredService<IShiftRepository>().OpenAsync(eventId, shiftIds, admin.Id);
        });
}
