using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using web.Services.Mail.Dtos.Messages;
using web.Services.Mail.Interfaces;

namespace web.Tests.Infrastructure;

/// <summary>Falsk mail-service: sender intet, men husker hver mail. Opførslen kan styres pr. modtageradresse.</summary>
public partial class FakeMailService : IMailService
{
    public bool Configured { get; set; } = true;

    public ConcurrentQueue<SendMailRequestDto> Sent { get; } = new();

    /// <summary>Opførsel pr. modtager: kast fx en MailException. Uden opførsel "sendes" mailen.</summary>
    public ConcurrentDictionary<string, Action<SendMailRequestDto>> Behaviors { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default) => Task.FromResult(Configured);

    public Task<SendMailResponseDto> SendAsync(SendMailRequestDto request, CancellationToken cancellationToken = default)
    {
        if (Behaviors.TryGetValue(request.ToAddress, out var behavior))
        {
            behavior(request);
        }

        Sent.Enqueue(request);
        return Task.FromResult(new SendMailResponseDto { MessageId = Guid.NewGuid().ToString("N"), Delivery = MailDelivery.Pickup });
    }

    public List<SendMailRequestDto> SentTo(string address) =>
        Sent.Where(x => string.Equals(x.ToAddress, address, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>Linket i mailens tekst, der starter med stien, fx "/Account/SetPassword".</summary>
    public static string? LinkIn(SendMailRequestDto mail, string path)
    {
        var match = LinkRegex().Matches(mail.TextBody).FirstOrDefault(m => new Uri(m.Value).AbsolutePath == path);
        return match is null ? null : new Uri(match.Value).PathAndQuery;
    }

    [GeneratedRegex(@"https?://\S+")]
    private static partial Regex LinkRegex();
}
