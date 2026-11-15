using System.Net;
using System.Net.Sockets;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using web.Services.Mail.Dtos.Mailbox;
using MailFolder = web.Services.Mail.Dtos.Mailbox.MailFolder;

namespace web.Services.Mail;

/// <summary>Systemets postkasse via IMAP (webmailen). Én forbindelse pr. kald. Alle fejl oversættes i RunImapAsync.</summary>
public partial class MailService
{
    public const int MaxAttachmentBytes = 25 * 1024 * 1024;

    public Task<bool> IsMailboxConfiguredAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(configuration["Mail:Imap:Host"]));

    public Task<MailboxStatusDto> GetMailboxStatusAsync(CancellationToken cancellationToken = default) =>
        WithImapAsync(async client =>
        {
            await client.Inbox.StatusAsync(StatusItems.Unread, cancellationToken);
            return new MailboxStatusDto { UnreadCount = client.Inbox.Unread };
        }, cancellationToken);

    public Task<ListMessagesResponseDto> ListMessagesAsync(ListMessagesRequestDto request, CancellationToken cancellationToken = default) =>
        WithImapAsync(async client =>
        {
            if (await TryOpenAsync(client, request.Folder, FolderAccess.ReadOnly, cancellationToken) is not { } folder)
            {
                // Sendt og Papirkurv oprettes først, når de bruges.
                return new ListMessagesResponseDto { Page = 1, PageSize = request.PageSize };
            }

            var query = string.IsNullOrWhiteSpace(request.Search)
                ? SearchQuery.All
                : SearchQuery.FromContains(request.Search.Trim()).Or(SearchQuery.SubjectContains(request.Search.Trim()));
            var uids = (await folder.SearchAsync(query, cancellationToken)).OrderByDescending(x => x.Id).ToList();

            var pageSize = Math.Clamp(request.PageSize, 1, 200);
            var page = Math.Max(1, request.Page);
            var pageUids = uids.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var items = new List<MailSummaryDto>();
            if (pageUids.Count > 0)
            {
                var summaries = await folder.FetchAsync(pageUids,
                    MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags |
                    MessageSummaryItems.BodyStructure | MessageSummaryItems.InternalDate, cancellationToken);
                items = summaries.OrderByDescending(x => x.UniqueId.Id).Select(x => new MailSummaryDto
                {
                    Uid = x.UniqueId.Id,
                    From = ToAddress(x.Envelope?.From.Mailboxes.FirstOrDefault()),
                    To = x.Envelope?.To.Mailboxes.Select(ToAddress).ToList() ?? [],
                    Subject = x.Envelope?.Subject ?? string.Empty,
                    DateUtc = (x.Envelope?.Date ?? x.InternalDate ?? DateTimeOffset.UtcNow).UtcDateTime,
                    Seen = x.Flags?.HasFlag(MessageFlags.Seen) == true,
                    HasAttachments = x.Attachments.Any()
                }).ToList();
            }

            var unread = (await folder.SearchAsync(SearchQuery.NotSeen, cancellationToken)).Count;
            return new ListMessagesResponseDto { Items = items, TotalCount = uids.Count, Page = page, PageSize = pageSize, UnreadCount = unread };
        }, cancellationToken);

    public Task<MailMessageDto?> GetMessageAsync(MailFolder folder, uint uid, CancellationToken cancellationToken = default) =>
        WithImapAsync(async client =>
        {
            var mailFolder = await OpenAsync(client, folder, FolderAccess.ReadOnly, cancellationToken);
            var id = new UniqueId(uid);
            var flags = await mailFolder.FetchAsync([id], MessageSummaryItems.Flags, cancellationToken);
            if (flags.Count == 0)
            {
                return null;
            }

            // GetMessage bruger BODY.PEEK, så mailen markeres ikke som læst her.
            var message = await mailFolder.GetMessageAsync(id, cancellationToken);
            return new MailMessageDto
            {
                Uid = uid,
                Folder = folder,
                From = ToAddress(message.From.Mailboxes.FirstOrDefault()),
                ReplyTo = message.ReplyTo.Mailboxes.FirstOrDefault() is { } replyTo ? ToAddress(replyTo) : null,
                To = message.To.Mailboxes.Select(ToAddress).ToList(),
                Cc = message.Cc.Mailboxes.Select(ToAddress).ToList(),
                Subject = message.Subject ?? string.Empty,
                DateUtc = message.Date.UtcDateTime,
                MessageId = message.MessageId,
                References = message.References.ToList(),
                TextBody = message.TextBody,
                HtmlBody = message.HtmlBody,
                Attachments = message.Attachments.Select((part, index) => new MailAttachmentInfoDto
                {
                    Index = index,
                    FileName = FileNameOf(part, index),
                    ContentType = part.ContentType.MimeType,
                    SizeBytes = SizeOf(part)
                }).ToList(),
                Seen = flags[0].Flags?.HasFlag(MessageFlags.Seen) == true
            };
        }, cancellationToken);

    public Task<bool> SetSeenAsync(MailFolder folder, uint uid, bool seen, CancellationToken cancellationToken = default) =>
        WithImapAsync(async client =>
        {
            var mailFolder = await OpenAsync(client, folder, FolderAccess.ReadWrite, cancellationToken);
            var id = new UniqueId(uid);
            if ((await mailFolder.FetchAsync([id], MessageSummaryItems.UniqueId, cancellationToken)).Count == 0)
            {
                return false;
            }

            await mailFolder.StoreAsync(id, new StoreFlagsRequest(seen ? StoreAction.Add : StoreAction.Remove, MessageFlags.Seen) { Silent = true }, cancellationToken);
            return true;
        }, cancellationToken);

    public Task<bool> DeleteAsync(MailFolder folder, uint uid, CancellationToken cancellationToken = default) =>
        WithImapAsync(async client =>
        {
            var mailFolder = await OpenAsync(client, folder, FolderAccess.ReadWrite, cancellationToken);
            var id = new UniqueId(uid);
            if ((await mailFolder.FetchAsync([id], MessageSummaryItems.UniqueId, cancellationToken)).Count == 0)
            {
                return false;
            }

            if (folder == MailFolder.Trash)
            {
                await mailFolder.StoreAsync(id, new StoreFlagsRequest(StoreAction.Add, MessageFlags.Deleted) { Silent = true }, cancellationToken);
                await mailFolder.ExpungeAsync([id], cancellationToken);
            }
            else
            {
                var trash = await FindFolderAsync(client, MailFolder.Trash, create: true, cancellationToken);
                await mailFolder.MoveToAsync(id, trash!, cancellationToken);
            }

            return true;
        }, cancellationToken);

    public Task<MailAttachmentDto?> GetAttachmentAsync(MailFolder folder, uint uid, int index, CancellationToken cancellationToken = default) =>
        WithImapAsync(async client =>
        {
            var mailFolder = await OpenAsync(client, folder, FolderAccess.ReadOnly, cancellationToken);
            var id = new UniqueId(uid);
            if ((await mailFolder.FetchAsync([id], MessageSummaryItems.UniqueId, cancellationToken)).Count == 0)
            {
                return null;
            }

            var message = await mailFolder.GetMessageAsync(id, cancellationToken);
            var part = message.Attachments.ElementAtOrDefault(index);
            if (part is null)
            {
                return null;
            }

            using var stream = new MemoryStream();
            if (part is MimePart mimePart)
            {
                await mimePart.Content.DecodeToAsync(stream, cancellationToken);
            }
            else if (part is MessagePart messagePart)
            {
                await messagePart.Message.WriteToAsync(stream, cancellationToken);
            }

            if (stream.Length > MaxAttachmentBytes)
            {
                throw new MailException(HttpStatusCode.RequestEntityTooLarge, $"Vedhæftningen er større end {MaxAttachmentBytes / 1024 / 1024} MB.");
            }

            return new MailAttachmentDto
            {
                FileName = FileNameOf(part, index),
                ContentType = part is MessagePart ? "message/rfc822" : part.ContentType.MimeType,
                Content = stream.ToArray()
            };
        }, cancellationToken);

    /// <summary>Gemmer en kopi af en sendt mail i Sendt-mappen (markeret som læst).</summary>
    private Task AppendToSentAsync(MimeMessage message, CancellationToken cancellationToken) =>
        WithImapAsync(async client =>
        {
            var sent = await FindFolderAsync(client, MailFolder.Sent, create: true, cancellationToken);
            await sent!.AppendAsync(new AppendRequest(message, MessageFlags.Seen), cancellationToken);
            return true;
        }, cancellationToken);

    private async Task<T> WithImapAsync<T>(Func<ImapClient, Task<T>> action, CancellationToken cancellationToken)
    {
        var host = configuration["Mail:Imap:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new MailException(HttpStatusCode.InternalServerError, "Mail:Imap:Host er ikke sat. Webmailen kræver IMAP.");
        }

        var security = ParseSecurity(configuration["Mail:Imap:Security"], "Mail:Imap:Security", SecureSocketOptions.SslOnConnect);
        var port = int.TryParse(configuration["Mail:Imap:Port"], out var configuredPort) && configuredPort > 0
            ? configuredPort
            : security == SecureSocketOptions.SslOnConnect ? 993 : 143;
        var userName = configuration["Mail:Imap:UserName"];
        var password = configuration["Mail:Imap:Password"];
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
        {
            throw new MailException(HttpStatusCode.InternalServerError, "Mail:Imap:UserName og Mail:Imap:Password skal være sat.");
        }

        return await RunImapAsync(host, port, security, async () =>
        {
            using var client = new ImapClient { Timeout = TimeoutMilliseconds };
            await client.ConnectAsync(host, port, security, cancellationToken);
            await client.AuthenticateAsync(userName, password, cancellationToken);
            var result = await action(client);
            await client.DisconnectAsync(true, cancellationToken);
            return result;
        }, cancellationToken);
    }

    private async Task<IMailFolder> OpenAsync(ImapClient client, MailFolder folder, FolderAccess access, CancellationToken cancellationToken) =>
        await TryOpenAsync(client, folder, access, cancellationToken)
        ?? throw new MailException(HttpStatusCode.NotFound, $"Mappen findes ikke i postkassen endnu. Tjek {FolderKey(folder)}.");

    private async Task<IMailFolder?> TryOpenAsync(ImapClient client, MailFolder folder, FolderAccess access, CancellationToken cancellationToken)
    {
        var mailFolder = await FindFolderAsync(client, folder, create: false, cancellationToken);
        if (mailFolder is not null)
        {
            await mailFolder.OpenAsync(access, cancellationToken);
        }

        return mailFolder;
    }

    private static string FolderKey(MailFolder folder) => folder == MailFolder.Sent ? "Mail:Imap:SentFolder" : "Mail:Imap:TrashFolder";

    /// <summary>
    /// Sendt og Papirkurv findes via IMAP special-use. Ellers via navnet i Mail:Imap:SentFolder/TrashFolder (også under INBOX).
    /// Findes mappen ikke, oprettes den med create (første gang der gemmes en kopi eller slettes), ellers null.
    /// </summary>
    private async Task<IMailFolder?> FindFolderAsync(ImapClient client, MailFolder folder, bool create, CancellationToken cancellationToken)
    {
        if (folder == MailFolder.Inbox)
        {
            return client.Inbox;
        }

        var special = folder == MailFolder.Sent ? SpecialFolder.Sent : SpecialFolder.Trash;
        if (client.Capabilities.Contains(ImapCapability.SpecialUse) || client.Capabilities.Contains(ImapCapability.XList))
        {
            try
            {
                if (client.GetFolder(special) is { } found)
                {
                    return found;
                }
            }
            catch (NotSupportedException)
            {
                // Serveren kender ikke special-use alligevel. Prøv navnet.
            }
        }

        var key = FolderKey(folder);
        var name = configuration[key] is { Length: > 0 } configured ? configured : folder == MailFolder.Sent ? "Sent" : "Trash";
        var root = client.GetFolder(client.PersonalNamespaces.Count > 0 ? client.PersonalNamespaces[0] : new FolderNamespace('.', ""));
        foreach (var candidate in await root.GetSubfoldersAsync(false, cancellationToken))
        {
            if (Matches(candidate, name))
            {
                return candidate;
            }
        }

        foreach (var candidate in await client.Inbox.GetSubfoldersAsync(false, cancellationToken))
        {
            if (Matches(candidate, name))
            {
                return candidate;
            }
        }

        if (!create)
        {
            return null;
        }

        // Under den personlige rod (fx INBOX. hos Dovecot), som mailprogrammer også gør.
        return await root.CreateAsync(name, true, cancellationToken);
    }

    private static bool Matches(IMailFolder folder, string name) =>
        folder.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || folder.FullName.Equals(name, StringComparison.OrdinalIgnoreCase);

    private static MailAddressDto ToAddress(MailboxAddress? mailbox) =>
        new() { Name = mailbox?.Name ?? string.Empty, Address = mailbox?.Address ?? string.Empty };

    private static string FileNameOf(MimeEntity part, int index) =>
        part switch
        {
            MimePart { FileName: { Length: > 0 } name } => name,
            MessagePart => $"vedhaeftet-mail-{index + 1}.eml",
            _ => $"vedhaeftning-{index + 1}"
        };

    private static long SizeOf(MimeEntity part)
    {
        if (part is not MimePart mimePart)
        {
            return 0;
        }

        using var stream = new MemoryStream();
        mimePart.Content.DecodeTo(stream);
        return stream.Length;
    }

    // Alle IMAP-kald går herigennem, så bibliotekets fejl oversættes ét sted (services.md regel 15).
    private static async Task<T> RunImapAsync<T>(string host, int port, SecureSocketOptions security, Func<Task<T>> action, CancellationToken cancellationToken)
    {
        try
        {
            return await action();
        }
        catch (MailException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is TimeoutException or OperationCanceledException)
        {
            throw new MailException(HttpStatusCode.GatewayTimeout, $"IMAP-serveren {host}:{port} svarede ikke inden for {TimeoutMilliseconds / 1000} sekunder.");
        }
        catch (AuthenticationException)
        {
            throw new MailException(HttpStatusCode.BadGateway, $"IMAP-serveren {host} afviste login. Tjek Mail:Imap:UserName og Mail:Imap:Password.");
        }
        catch (FolderNotFoundException ex)
        {
            throw new MailException(HttpStatusCode.NotFound, $"Mappen \"{ex.FolderName}\" findes ikke i postkassen.");
        }
        catch (Exception ex) when (ex is ImapCommandException or ImapProtocolException or ServiceNotAuthenticatedException or ServiceNotConnectedException)
        {
            throw new MailException(HttpStatusCode.BadGateway, $"Fejl fra IMAP-serveren {host}: {MaskAddresses(ex.Message)}");
        }
        catch (Exception ex) when (ex is SocketException or SslHandshakeException or IOException)
        {
            throw new MailException(HttpStatusCode.BadGateway,
                $"Kunne ikke forbinde til IMAP-serveren {host}:{port} ({security}). Tjek Mail:Imap:Host, Mail:Imap:Port og Mail:Imap:Security. {ex.Message}");
        }
    }
}
