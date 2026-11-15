using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using web.Services.Mail;
using web.Services.Mail.Dtos.Mailbox;
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
        var messageId = $"{Guid.NewGuid():N}@test.local";
        if (request.SaveCopyToSent)
        {
            AddMessage(MailFolder.Sent, new MailMessageDto
            {
                From = new MailAddressDto { Name = "Vagttilmelding", Address = SystemAddress },
                To = [new MailAddressDto { Name = request.ToName, Address = request.ToAddress }],
                Subject = request.Subject,
                TextBody = request.TextBody,
                HtmlBody = request.HtmlBody,
                MessageId = messageId,
                Seen = true
            });
        }

        return Task.FromResult(new SendMailResponseDto { MessageId = messageId, Delivery = MailDelivery.Pickup });
    }

    // ----- Postkassen (IMAP) i hukommelsen -----

    public const string SystemAddress = "system@test.local";

    public bool MailboxConfigured { get; set; } = true;

    /// <summary>Sættes den, kaster alle IMAP-kald den (fx "postkassen kan ikke nås").</summary>
    public MailException? MailboxError { get; set; }

    private readonly Lock _mailboxLock = new();
    private readonly List<(MailFolder Folder, MailMessageDto Message, List<MailAttachmentDto> Files)> _mailbox = [];
    private uint _nextUid = 1;

    public uint AddMessage(MailFolder folder, MailMessageDto message, params MailAttachmentDto[] attachments)
    {
        lock (_mailboxLock)
        {
            message.Uid = _nextUid++;
            message.Folder = folder;
            message.Attachments = attachments.Select((x, i) => new MailAttachmentInfoDto
            {
                Index = i, FileName = x.FileName, ContentType = x.ContentType, SizeBytes = x.Content.Length
            }).ToList();
            _mailbox.Add((folder, message, attachments.ToList()));
            return message.Uid;
        }
    }

    public List<MailMessageDto> MessagesIn(MailFolder folder)
    {
        lock (_mailboxLock)
        {
            return _mailbox.Where(x => x.Folder == folder).Select(x => x.Message).ToList();
        }
    }

    private void ThrowIfBroken()
    {
        if (MailboxError is not null)
        {
            throw MailboxError;
        }
    }

    private (MailFolder Folder, MailMessageDto Message, List<MailAttachmentDto> Files)? Find(MailFolder folder, uint uid) =>
        _mailbox.Where(x => x.Folder == folder && x.Message.Uid == uid).Cast<(MailFolder, MailMessageDto, List<MailAttachmentDto>)?>().FirstOrDefault();

    public Task<bool> IsMailboxConfiguredAsync(CancellationToken cancellationToken = default) => Task.FromResult(MailboxConfigured);

    public Task<MailboxStatusDto> GetMailboxStatusAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfBroken();
        lock (_mailboxLock)
        {
            return Task.FromResult(new MailboxStatusDto { UnreadCount = _mailbox.Count(x => x.Folder == MailFolder.Inbox && !x.Message.Seen) });
        }
    }

    public Task<ListMessagesResponseDto> ListMessagesAsync(ListMessagesRequestDto request, CancellationToken cancellationToken = default)
    {
        ThrowIfBroken();
        lock (_mailboxLock)
        {
            var all = _mailbox.Where(x => x.Folder == request.Folder).Select(x => x.Message)
                .Where(x => string.IsNullOrWhiteSpace(request.Search) ||
                            x.Subject.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                            x.From.Address.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                            x.From.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Uid).ToList();
            var items = all.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).Select(x => new MailSummaryDto
            {
                Uid = x.Uid, From = x.From, To = x.To, Subject = x.Subject, DateUtc = x.DateUtc, Seen = x.Seen, HasAttachments = x.Attachments.Count > 0
            }).ToList();
            return Task.FromResult(new ListMessagesResponseDto
            {
                Items = items, TotalCount = all.Count, Page = request.Page, PageSize = request.PageSize, UnreadCount = all.Count(x => !x.Seen)
            });
        }
    }

    public Task<MailMessageDto?> GetMessageAsync(MailFolder folder, uint uid, CancellationToken cancellationToken = default)
    {
        ThrowIfBroken();
        lock (_mailboxLock)
        {
            return Task.FromResult(Find(folder, uid)?.Message);
        }
    }

    public Task<bool> SetSeenAsync(MailFolder folder, uint uid, bool seen, CancellationToken cancellationToken = default)
    {
        ThrowIfBroken();
        lock (_mailboxLock)
        {
            var found = Find(folder, uid);
            if (found is null)
            {
                return Task.FromResult(false);
            }

            found.Value.Message.Seen = seen;
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAsync(MailFolder folder, uint uid, CancellationToken cancellationToken = default)
    {
        ThrowIfBroken();
        lock (_mailboxLock)
        {
            var index = _mailbox.FindIndex(x => x.Folder == folder && x.Message.Uid == uid);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            var item = _mailbox[index];
            _mailbox.RemoveAt(index);
            if (folder != MailFolder.Trash)
            {
                item.Message.Uid = _nextUid++;
                item.Message.Folder = MailFolder.Trash;
                _mailbox.Add((MailFolder.Trash, item.Message, item.Files));
            }

            return Task.FromResult(true);
        }
    }

    public Task<MailAttachmentDto?> GetAttachmentAsync(MailFolder folder, uint uid, int index, CancellationToken cancellationToken = default)
    {
        ThrowIfBroken();
        lock (_mailboxLock)
        {
            return Task.FromResult(Find(folder, uid)?.Files.ElementAtOrDefault(index));
        }
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
