using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using web.Services.Push;
using web.Services.Push.Dtos.Messages;

namespace web.Tests;

/// <summary>Den rigtige PushService (kryptering og VAPID) mod en falsk push-tjeneste. Intet sendes over nettet.</summary>
public class PushServiceTests
{
    private static readonly (string PublicKey, string PrivateKey) Keys = VapidKeyGenerator.Generate();

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        /// <summary>Indholdet læses her, fordi biblioteket lukker det efter afsendelsen.</summary>
        public List<byte[]> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken));
            return respond(request);
        }
    }

    private sealed class StubFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private static PushService Create(HttpMessageHandler handler, string? publicKey = null, string? privateKey = null, string? subject = "mailto:test@example.com") =>
        new(new StubFactory(handler), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Push:PublicKey"] = publicKey ?? Keys.PublicKey,
            ["Push:PrivateKey"] = privateKey ?? Keys.PrivateKey,
            ["Push:Subject"] = subject
        }).Build());

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Et abonnement, som en browser ville lave det: en P-256-nøgle (p256dh) og 16 tilfældige bytes (auth).</summary>
    private static SendPushRequestDto Request(string endpoint = "https://fcm.googleapis.com/fcm/send/abc")
    {
        using var browserKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var q = browserKey.ExportParameters(false).Q;
        return new SendPushRequestDto
        {
            Endpoint = endpoint,
            P256dh = Base64Url([0x04, .. q.X!, .. q.Y!]),
            Auth = Base64Url(RandomNumberGenerator.GetBytes(16)),
            Title = "Nye vagter er åbne",
            Body = "Fest: 2 vagter er åbne for tilmelding.",
            Url = "/Signup",
            Urgency = PushUrgency.High
        };
    }

    [Fact]
    public void VapidKeyGenerator_LaverNoeglerIDetRigtigeFormat()
    {
        var (publicKey, privateKey) = VapidKeyGenerator.Generate();

        Assert.Equal(87, publicKey.Length);
        Assert.Equal(43, privateKey.Length);
        Assert.DoesNotContain('=', publicKey + privateKey);
        Assert.NotEqual(publicKey, VapidKeyGenerator.Generate().PublicKey);
    }

    [Fact]
    public async Task Levering_KrypteretMedVapid()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var service = Create(handler);

        var response = await service.SendAsync(Request());

        Assert.Equal(PushDeliveryStatus.Delivered, response.Status);
        var sent = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, sent.Method);
        Assert.Equal("vapid", sent.Headers.Authorization?.Scheme);
        Assert.Contains("aes128gcm", sent.Content!.Headers.ContentEncoding);
        Assert.Equal("high", sent.Headers.GetValues("Urgency").Single());
        Assert.True(sent.Headers.Contains("TTL"));

        // Indholdet er krypteret: titlen står ikke i klartekst.
        var body = Assert.Single(handler.Bodies);
        Assert.NotEmpty(body);
        Assert.DoesNotContain("Nye vagter", System.Text.Encoding.UTF8.GetString(body));
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Gone)]
    public async Task UdloebetAbonnement_GiverSubscriptionGone(HttpStatusCode status)
    {
        var service = Create(new StubHandler(_ => new HttpResponseMessage(status)));

        var response = await service.SendAsync(Request());

        Assert.Equal(PushDeliveryStatus.SubscriptionGone, response.Status);
    }

    [Fact]
    public async Task AfvistNoegle_Giver502UdenAtViseNoeglen()
    {
        var service = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)));

        var ex = await Assert.ThrowsAsync<PushException>(() => service.SendAsync(Request()));

        Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, ex.UpstreamStatusCode);
        Assert.Contains("Push:PrivateKey", ex.Message);
        Assert.DoesNotContain(Keys.PrivateKey, ex.Message);
    }

    [Fact]
    public async Task Serverfejl_ViderestillesMedStatuskoden()
    {
        var service = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var ex = await Assert.ThrowsAsync<PushException>(() => service.SendAsync(Request()));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.StatusCode);
        Assert.DoesNotContain("/fcm/send/abc", ex.Message);
    }

    [Fact]
    public async Task IngenForbindelse_Giver502()
    {
        var service = Create(new StubHandler(_ => throw new HttpRequestException("Ingen forbindelse")));

        var ex = await Assert.ThrowsAsync<PushException>(() => service.SendAsync(Request()));

        Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
    }

    [Fact]
    public async Task ManglendeKonfiguration_ErSlaaetFra()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var service = Create(handler, publicKey: "", privateKey: "");

        Assert.Null(await service.GetPublicKeyAsync());
        var ex = await Assert.ThrowsAsync<PushException>(() => service.SendAsync(Request()));
        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Contains("Push:PublicKey", ex.Message);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task UgyldigeNoegler_Giver500UdenAtViseDem()
    {
        var service = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)), privateKey: "ikke-en-noegle");

        var ex = await Assert.ThrowsAsync<PushException>(() => service.SendAsync(Request()));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.DoesNotContain("ikke-en-noegle", ex.Message);
    }

    [Theory]
    [InlineData("https://evil.example.com/push")]
    [InlineData("https://127.0.0.1/push")]
    [InlineData("http://fcm.googleapis.com/fcm/send/abc")]
    [InlineData("https://fcm.googleapis.com:8443/fcm/send/abc")]
    public async Task UkendtPushTjeneste_KaldesAldrig(string endpoint)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var service = Create(handler);

        var ex = await Assert.ThrowsAsync<PushException>(() => service.SendAsync(Request(endpoint)));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData("https://fcm.googleapis.com/fcm/send/abc", true)]
    [InlineData("https://updates.push.services.mozilla.com/wpush/v2/abc", true)]
    [InlineData("https://wns2-db5p.notify.windows.com/w/?token=abc", true)]
    [InlineData("https://web.push.apple.com/abc", true)]
    [InlineData("https://notify.windows.com.evil.example.com/abc", false)]
    [InlineData("https://evilpush.apple.com/abc", false)]
    [InlineData("ikke en adresse", false)]
    public void PushEndpoints_KunKendteTjenester(string endpoint, bool allowed) =>
        Assert.Equal(allowed, PushEndpoints.IsAllowed(endpoint));
}
