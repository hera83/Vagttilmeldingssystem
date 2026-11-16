using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using web.Data;
using web.Data.Models;
using web.Infrastructure.Mail;
using web.Managers.Maintenance.Interfaces;
using web.Repositories.Assignments.Dtos;
using web.Repositories.Assignments.Interfaces;
using web.Repositories.Notifications.Dtos;
using web.Repositories.Notifications.Interfaces;
using web.Repositories.PasswordTokens.Dtos;
using web.Repositories.PasswordTokens.Interfaces;
using web.Repositories.Profiles.Interfaces;
using web.Repositories.PushSubscriptions.Dtos;
using web.Repositories.PushSubscriptions.Interfaces;
using web.Repositories.Users.Interfaces;
using web.Tests.Infrastructure;

namespace web.Tests;

/// <summary>Negative tests og persondata (step 15, krav K05, K10, K16 og regel 8 i krav.md).</summary>
public class SecurityTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public SecurityTests(WebAppFactory factory) => _factory = factory;

    private Task<T> With<T>(Func<IServiceProvider, Task<T>> action) => _factory.WithServiceAsync<IServiceProvider, T>(action);

    private async Task<(HttpClient Browser, Guid UserId, string UserName)> ParentAsync(bool completeProfile = true)
    {
        var user = await _factory.CreateUserAsync($"sec-{Guid.NewGuid():N}@test.local", displayName: "Sikker Forælder");
        if (completeProfile)
        {
            await _factory.CompleteProfileAsync(user.Id);
        }

        var browser = _factory.CreateBrowser();
        await browser.LoginAsync(user.UserName, WebAppFactory.UserPassword);
        return (browser, user.Id, user.UserName);
    }

    private async Task<HttpClient> AdminAsync()
    {
        var admin = _factory.CreateBrowser();
        await admin.LoginAsync(WebAppFactory.AdminUserName, WebAppFactory.AdminPassword);
        return admin;
    }

    private Task<AssignResultDto> AssignAsync(Guid userId, Guid shiftId) =>
        With(sp => sp.GetRequiredService<IAssignmentRepository>().CreateManyAsync(new CreateAssignmentsDto
        {
            UserId = userId, ShiftIds = [shiftId], Source = AssignmentSource.Admin, OnlyOpen = false
        }));

    [Fact]
    public async Task AdminSider_SomBruger_GivesIngenAdgang_GetOgPost()
    {
        var (parent, parentId, _) = await ParentAsync();
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 2);
        var other = await _factory.CreateUserAsync($"anden-{Guid.NewGuid():N}@test.local");

        // Brugerens egen vagt: kan ikke afmeldes via admin-actionen (K16).
        await AssignAsync(parentId, shift.Id);
        var ownAssignmentId = await With(sp => sp.GetRequiredService<WebDbContext>().Assignments
            .Where(x => x.UserId == parentId && x.ShiftId == shift.Id).Select(x => x.Id).SingleAsync());

        foreach (var url in new[]
                 {
                     "/Dashboard", "/Events", $"/Events/Details/{ev.Id}", $"/Events/Open/{ev.Id}", $"/Events/Participants/{ev.Id}",
                     $"/Shifts/Edit/{shift.Id}", $"/Shifts/Assign/{shift.Id}", "/Users", $"/Users/Profile/{other.Id}", $"/Users/Export/{other.Id}",
                     "/Mailbox", "/Mailbox/Compose"
                 })
        {
            var response = await parent.GetAsync(url);
            Assert.True(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location!.PathAndQuery.StartsWith("/Account/AccessDenied"), url);
        }

        foreach (var (url, field) in new[]
                 {
                     ($"/Events/Open/{ev.Id}", ("ShiftIds", shift.Id.ToString())),
                     ($"/Shifts/Assign/{shift.Id}", ("userId", other.Id.ToString())),
                     ($"/Shifts/Unassign/{shift.Id}", ("assignmentId", ownAssignmentId.ToString())),
                     ($"/Shifts/Delete/{shift.Id}", ("x", "y")),
                     ($"/Users/Anonymize/{other.Id}", ("x", "y")),
                     ($"/Users/Delete/{other.Id}", ("x", "y")),
                     ($"/Events/Delete/{ev.Id}", ("x", "y"))
                 })
        {
            var response = await parent.PostFormAsync("/Profile", url, field);
            Assert.True(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location!.PathAndQuery.StartsWith("/Account/AccessDenied"), url);
        }

        var stillDraft = await With(sp => sp.GetRequiredService<WebDbContext>().Shifts.Where(x => x.Id == shift.Id).Select(x => x.Status).SingleAsync());
        Assert.Equal(ShiftStatus.Draft, stillDraft);
        Assert.NotNull(await With(sp => sp.GetRequiredService<IUserRepository>().GetByIdAsync(other.Id)));
        Assert.Single(await With(sp => sp.GetRequiredService<IAssignmentRepository>().GetActiveForUserAsync(parentId)));
    }

    [Fact]
    public async Task GaettedeId_GiverIkkeAdgangTilAndresVagter()
    {
        var (parent, _, _) = await ParentAsync();
        var other = await _factory.CreateUserAsync($"anden-{Guid.NewGuid():N}@test.local");
        var ev = await _factory.CreateEventAsync();
        var othersShift = await _factory.CreateShiftAsync(ev.Id, 2, title: "Andens hemmelige kladde");
        await AssignAsync(other.Id, othersShift.Id);

        var details = await parent.GetAsync($"/MyShifts/Details/{othersShift.Id}");
        Assert.Equal(HttpStatusCode.NotFound, details.StatusCode);
        Assert.DoesNotContain("Andens hemmelige kladde", await parent.GetHtmlAsync("/MyShifts"));
        Assert.DoesNotContain("Andens hemmelige kladde", await parent.GetHtmlAsync("/Signup"));
        Assert.DoesNotContain("Andens hemmelige kladde", await parent.GetHtmlAsync("/"));
    }

    [Fact]
    public async Task Kladde_KanIkkeTilmeldesMedManipuleretPost()
    {
        var (parent, userId, _) = await ParentAsync();
        var ev = await _factory.CreateEventAsync();
        var draft = await _factory.CreateShiftAsync(ev.Id, 2, title: "Hemmelig kladde 42");

        var response = await parent.PostFormAsync("/Signup", "/Signup/Enroll", ("ShiftIds", draft.Id.ToString()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Hemmelig kladde 42", await response.Content.ReadAsStringAsync());
        Assert.Empty(await With(sp => sp.GetRequiredService<IAssignmentRepository>().GetActiveForUserAsync(userId)));
    }

    [Fact]
    public async Task GentagetTilmelding_GiverKunEnPlads()
    {
        var (parent, userId, _) = await ParentAsync();
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 3);
        await _factory.OpenShiftsAsync(shift.Id);
        var token = await parent.GetAntiforgeryTokenAsync("/Signup");
        var form = new[] { new KeyValuePair<string, string>("ShiftIds", shift.Id.ToString()), new("__RequestVerificationToken", token) };

        var first = await parent.PostAsync("/Signup/Enroll", new FormUrlEncodedContent(form));
        var replay = await parent.PostAsync("/Signup/Enroll", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Single(await With(sp => sp.GetRequiredService<IAssignmentRepository>().GetActiveForUserAsync(userId)));
    }

    [Fact]
    public async Task MassAssignment_EkstraFelterIgnoreres()
    {
        var (parent, userId, _) = await ParentAsync(completeProfile: false);
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 3);
        await _factory.OpenShiftsAsync(shift.Id);

        // Profilen er ikke udfyldt: et forfalsket ProfileComplete hjælper ikke.
        await parent.PostFormAsync("/Signup", "/Signup/Enroll", ("ShiftIds", shift.Id.ToString()), ("ProfileComplete", "true"));
        Assert.Empty(await With(sp => sp.GetRequiredService<IAssignmentRepository>().GetActiveForUserAsync(userId)));

        // Felter som IsAdmin, UserId og Email i profilformularen har ingen virkning.
        var other = await _factory.CreateUserAsync($"offer-{Guid.NewGuid():N}@test.local");
        await parent.PostFormAsync("/Profile", "/Profile",
            ("FullName", "Ny Navn"), ("Address", "Vej 1"), ("Phone", "12345678"), ("ShirtSize", "M"), ("DrinkTickets", "1"), ("TivoliSheets", "1"),
            ("ChildName", "Barn"), ("ChildClass", "1.A"), ("IsAdmin", "true"), ("UserId", other.Id.ToString()), ("Email", "hacker@example.com"));
        var me = await With(sp => sp.GetRequiredService<IUserRepository>().GetByIdAsync(userId));
        Assert.False(me!.IsAdmin);
        Assert.Null(await With(sp => sp.GetRequiredService<IProfileRepository>().GetByUserIdAsync(other.Id)));
        Assert.Equal("Ny Navn", (await With(sp => sp.GetRequiredService<IProfileRepository>().GetByUserIdAsync(userId)))!.FullName);
    }

    [Fact]
    public async Task PostUdenAntiforgeryToken_Giver400()
    {
        var (parent, _, _) = await ParentAsync();
        using var admin = await AdminAsync();

        foreach (var (client, url) in new[]
                 {
                     (parent, "/Profile"), (parent, "/Signup/Enroll"), (parent, "/Account/Logout"), (parent, "/Profile/Notifications"),
                     (admin, "/Users/Create"), (admin, "/Events/Create")
                 })
        {
            var response = await client.PostAsync(url, new FormUrlEncodedContent([new("x", "y")]));
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, url);
        }
    }

    [Fact]
    public async Task SikkerhedsHeaders_PaaSiderOgStatiskeFiler()
    {
        using var anonymous = _factory.CreateBrowser();
        foreach (var url in new[] { "/Account/Login", "/css/site.css", "/Home/Status/404", "/service-worker.js" })
        {
            var response = await anonymous.GetAsync(url);
            var csp = response.Headers.GetValues("Content-Security-Policy").Single();
            Assert.Contains("default-src 'self'", csp);
            Assert.Contains("script-src 'self'", csp);
            Assert.DoesNotContain("unsafe-inline", csp);
            Assert.Contains("frame-ancestors 'none'", csp);
            Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal("same-origin", response.Headers.GetValues("Referrer-Policy").Single());
            Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
            Assert.True(response.Headers.Contains("Permissions-Policy"), url);
        }

        // Ingen inline-scripts eller -styles i siderne (CSP ville blokere dem).
        var (parent, _, _) = await ParentAsync();
        using var admin = await AdminAsync();
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 2);
        var pages = new[] { "/", "/Signup", "/MyShifts", "/Profile", "/Home/Privacy" }.Select(url => (parent, url))
            .Concat(new[] { "/Dashboard", "/Events", $"/Events/Details/{ev.Id}", $"/Events/Open/{ev.Id}", $"/Shifts/Assign/{shift.Id}", "/Users", "/Users/Create" }
                .Select(url => (admin, url)));
        foreach (var (client, url) in pages)
        {
            var html = await client.GetHtmlAsync(url);
            Assert.DoesNotMatch("<script(?![^>]*\\bsrc=)[^>]*>", html);
            Assert.False(html.Contains(" style=\""), url);
            Assert.DoesNotMatch(" on(click|load|submit|change)=\"", html);
        }

        // Fejl på frigivelsessiden vises også uden inline-style (asp-validation-summary="All" ville indsætte en).
        var invalid = await admin.PostFormAsync($"/Events/Open/{ev.Id}", $"/Events/Open/{ev.Id}");
        var invalidHtml = await invalid.Content.ReadAsStringAsync();
        Assert.Contains("Vælg mindst én vagt, der skal åbnes.", invalidHtml);
        Assert.DoesNotContain(" style=\"", invalidHtml);
    }

    [Fact]
    public async Task Anonymisering_SletterPersondata_OgBevarerVagthistorik()
    {
        var (parent, userId, userName) = await ParentAsync();
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 3);
        await AssignAsync(userId, shift.Id);
        await With(sp => sp.GetRequiredService<IPushSubscriptionRepository>().SaveAsync(new SavePushSubscriptionDto
        {
            UserId = userId, Endpoint = FakePushService.NewEndpoint(), P256dh = "p", Auth = "a"
        }));
        await With(sp => sp.GetRequiredService<INotificationRepository>().CreateManyAsync([new CreateNotificationDto
        {
            UserId = userId, Channel = NotificationChannel.Email, Kind = "Test", Title = "t", Body = "b", DedupKey = $"anon:{userId:N}", NotBeforeUtc = DateTime.UtcNow.AddDays(1)
        }]));
        using var admin = await AdminAsync();

        var response = await admin.PostFormAsync($"/Users/Profile/{userId}", $"/Users/Anonymize/{userId}");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var user = (await With(sp => sp.GetRequiredService<IUserRepository>().GetByIdAsync(userId)))!;
        // De sidste 8 tegn af id'et: de første er UUIDv7's tidsstempel og ens for brugere oprettet samtidig.
        Assert.Equal($"Slettet bruger {userId.ToString("N")[^8..]}", user.DisplayName);
        Assert.DoesNotContain("sec-", user.UserName);
        Assert.False(user.IsActive);
        Assert.Null(await With(sp => sp.GetRequiredService<IProfileRepository>().GetByUserIdAsync(userId)));
        Assert.Empty(await With(sp => sp.GetRequiredService<IPushSubscriptionRepository>().GetByUserIdAsync(userId)));
        Assert.Empty(await With(sp => sp.GetRequiredService<INotificationRepository>().GetForUserAsync(userId, 10)));
        Assert.Single(await With(sp => sp.GetRequiredService<IAssignmentRepository>().GetActiveForUserAsync(userId)));

        // Den gamle session er død, og login er ikke muligt.
        var old = await parent.GetAsync("/");
        Assert.StartsWith("/Account/Login", old.Headers.Location?.PathAndQuery);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using var again = _factory.CreateBrowser();
            await again.LoginAsync(userName, WebAppFactory.UserPassword);
        });

        // Vagthistorikken viser "Slettet bruger" for administratoren.
        Assert.Contains("Slettet bruger", await admin.GetHtmlAsync($"/Shifts/Assign/{shift.Id}"));
    }

    [Fact]
    public async Task Anonymisering_AfSigSelv_Afvises()
    {
        using var admin = await AdminAsync();
        var adminId = (await With(async sp => (await sp.GetRequiredService<IUserRepository>().GetAllAsync()).Single(x => x.UserName == WebAppFactory.AdminUserName))).Id;

        var response = await admin.PostFormAsync("/Users", $"/Users/Anonymize/{adminId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Dataudtraek_KunEgneData_UdenHemmeligheder()
    {
        var (parent, userId, userName) = await ParentAsync();
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 3, title: "Min vagt til udtræk");
        await AssignAsync(userId, shift.Id);
        var endpoint = FakePushService.NewEndpoint();
        await With(sp => sp.GetRequiredService<IPushSubscriptionRepository>().SaveAsync(new SavePushSubscriptionDto
        {
            UserId = userId, Endpoint = endpoint, P256dh = "hemmelig-p256dh", Auth = "hemmelig-auth"
        }));

        var response = await parent.GetAsync("/Profile/Export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("mine-data-", response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(userName, doc.RootElement.GetProperty("Konto").GetProperty("Email").GetString());
        Assert.Equal("Test Forælder", doc.RootElement.GetProperty("Profil").GetProperty("FuldeNavn").GetString());
        Assert.Contains("Min vagt til udtræk", json);
        Assert.Equal("fcm.googleapis.com", doc.RootElement.GetProperty("EnhederMedPush")[0].GetProperty("PushTjeneste").GetString());
        Assert.DoesNotContain(endpoint, json);
        Assert.DoesNotContain("hemmelig", json);
        Assert.DoesNotContain("PasswordHash", json);
        Assert.DoesNotContain("SecurityStamp", json);

        using var admin = await AdminAsync();
        var adminExport = await admin.GetAsync($"/Users/Export/{userId}");
        Assert.Equal(HttpStatusCode.OK, adminExport.StatusCode);
        Assert.Contains("Min vagt til udtræk", await adminExport.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task OmDineData_KanLaesesUdenLogin()
    {
        using var anonymous = _factory.CreateBrowser();
        var html = await anonymous.GetHtmlAsync("/Home/Privacy");

        Assert.Contains("Om dine data", html);
        Assert.Contains("Hent mine data", html);
        Assert.Contains("href=\"/Home/Privacy\"", await anonymous.GetHtmlAsync("/Account/Login"));
    }

    [Fact]
    public async Task Oprydning_SletterGamleBeskederOgUdloebneLinks()
    {
        var user = await _factory.CreateUserAsync($"ryd-{Guid.NewGuid():N}@test.local");
        var marker = Guid.NewGuid().ToString("N");
        await With(sp => sp.GetRequiredService<INotificationRepository>().CreateManyAsync(
        [
            new CreateNotificationDto { UserId = user.Id, Channel = NotificationChannel.Push, Kind = "Test", Title = "gammel", DedupKey = $"old:{marker}", NotBeforeUtc = DateTime.UtcNow },
            new CreateNotificationDto { UserId = user.Id, Channel = NotificationChannel.Push, Kind = "Test", Title = "ny", DedupKey = $"new:{marker}", NotBeforeUtc = DateTime.UtcNow },
            new CreateNotificationDto { UserId = user.Id, Channel = NotificationChannel.Push, Kind = "Test", Title = "venter", DedupKey = $"pending:{marker}", NotBeforeUtc = DateTime.UtcNow.AddDays(1) }
        ]));
        await With(sp => sp.GetRequiredService<WebDbContext>().Notifications.Where(x => x.UserId == user.Id && x.Title != "venter")
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, NotificationStatus.Sent)));
        await With(sp => sp.GetRequiredService<WebDbContext>().Notifications.Where(x => x.UserId == user.Id && x.Title != "ny")
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UpdatedAt, DateTime.UtcNow.AddDays(-120))));
        var expired = await With(sp => sp.GetRequiredService<IPasswordTokenRepository>().CreateAsync(new CreatePasswordTokenDto
        {
            UserId = user.Id, Purpose = PasswordTokenPurpose.Reset, ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        }));

        var result = await With(sp => sp.GetRequiredService<IMaintenanceManager>().CleanupAsync());

        Assert.True(result.DeletedNotifications >= 1);
        Assert.True(result.DeletedPasswordTokens >= 1);
        var left = (await With(sp => sp.GetRequiredService<INotificationRepository>().GetForUserAsync(user.Id, 10))).Select(x => x.Title).ToList();
        Assert.Equal(["venter", "ny"], left.OrderByDescending(x => x).ToList());
        Assert.Null(await With(sp => sp.GetRequiredService<IPasswordTokenRepository>().FindAsync(expired.Token)));
    }

    [Fact]
    public async Task LogUd_MedPushAdresse_SletterAbonnementetPaaEnheden()
    {
        var (parent, userId, _) = await ParentAsync();
        var endpoint = FakePushService.NewEndpoint();
        var otherDevice = FakePushService.NewEndpoint();
        foreach (var e in new[] { endpoint, otherDevice })
        {
            await With(sp => sp.GetRequiredService<IPushSubscriptionRepository>().SaveAsync(new SavePushSubscriptionDto { UserId = userId, Endpoint = e, P256dh = "p", Auth = "a" }));
        }

        var response = await parent.PostFormAsync("/Profile", "/Account/Logout", ("pushEndpoint", endpoint));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var left = await With(sp => sp.GetRequiredService<IPushSubscriptionRepository>().GetByUserIdAsync(userId));
        Assert.Equal(otherDevice, Assert.Single(left).Endpoint);
    }

    [Theory]
    [InlineData("<svg onload=alert(1)>", "alert")]
    [InlineData("<a href=\"data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==\">x</a>", "data:")]
    [InlineData("<a href=\"jav&#x09;ascript:alert(1)\">x</a>", "ascript")]
    [InlineData("<div style=\"background:url(https://track.example.com/x)\">x</div>", "track.example.com")]
    [InlineData("<math><mtext><table><mglyph><style><img src=x onerror=alert(1)>", "onerror")]
    [InlineData("<base href=\"https://evil.example.com/\">", "evil.example.com")]
    [InlineData("<meta http-equiv=\"refresh\" content=\"0;url=https://evil.example.com\">", "evil.example.com")]
    [InlineData("<object data=\"https://evil.example.com/x.swf\"></object>", "evil.example.com")]
    [InlineData("<img src=\"https://track.example.com/pixel.gif\">", "track.example.com")]
    public void Webmail_HtmlRensning_KendteXssMoenstre(string html, string forbidden)
    {
        var clean = new MailHtmlSanitizer().Sanitize("<p>Hej</p>" + html);

        Assert.DoesNotContain(forbidden, clean, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>Hej</p>", clean);
    }
}

/// <summary>Rate limiting (step 15) med lave grænser i sin egen app.</summary>
public class RateLimitTests : IClassFixture<RateLimitWebAppFactory>
{
    private readonly RateLimitWebAppFactory _factory;

    public RateLimitTests(RateLimitWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task ForMangeLoginForsoeg_Giver429MedStatusside()
    {
        using var browser = _factory.CreateBrowser();
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
        {
            var response = await browser.PostFormAsync("/Account/Login", "/Account/Login", ("UserName", "ukendt@test.local"), ("Password", "forkert-forkert"));
            statuses.Add(response.StatusCode);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                Assert.Contains("For mange forsøg", await response.Content.ReadAsStringAsync());
            }
        }

        Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests], statuses);
    }

    [Fact]
    public async Task GlemtAdgangskode_BegraenesOgsaa()
    {
        using var browser = _factory.CreateBrowser();
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
        {
            statuses.Add((await browser.PostFormAsync("/Account/ForgotPassword", "/Account/ForgotPassword", ("Email", "x@test.local"))).StatusCode);
        }

        Assert.Equal([HttpStatusCode.Redirect, HttpStatusCode.Redirect, HttpStatusCode.TooManyRequests], statuses);
    }
}
