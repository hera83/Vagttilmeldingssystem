using web.Extensions;
using web.Managers.Mailbox.Dtos;
using web.Services.Mail.Dtos.Mailbox;

namespace web.ViewModels.Mailbox;

public static class MailboxViewModelMapper
{
    public static MailFolder ToMailFolder(string? folder) => folder?.ToLowerInvariant() switch
    {
        "sent" => MailFolder.Sent,
        "trash" => MailFolder.Trash,
        _ => MailFolder.Inbox
    };

    public static string ToRouteValue(this MailFolder folder) => folder switch
    {
        MailFolder.Sent => "sent",
        MailFolder.Trash => "trash",
        _ => "inbox"
    };

    public static string ToTitle(this MailFolder folder) => folder switch
    {
        MailFolder.Sent => "Sendt",
        MailFolder.Trash => "Papirkurv",
        _ => "Indbakke"
    };

    public static IndexMailboxViewModel ToIndexMailboxViewModel(this MailboxPageDto dto, DateTime nowUtc) => new()
    {
        Folder = dto.Folder.ToRouteValue(),
        FolderTitle = dto.Folder.ToTitle(),
        PersonColumnTitle = dto.Folder == MailFolder.Sent ? "Til" : "Fra",
        Search = dto.Search,
        Page = dto.Page,
        TotalPages = Math.Max(1, (int)Math.Ceiling(dto.TotalCount / (double)dto.PageSize)),
        TotalCount = dto.TotalCount,
        UnreadCount = dto.UnreadCount,
        Error = dto.Error,
        Items = dto.Items.Select(x =>
        {
            var person = dto.Folder == MailFolder.Sent ? x.Summary.To.FirstOrDefault() ?? new MailAddressDto() : x.Summary.From;
            return new MailListItemViewModel
            {
                Uid = x.Summary.Uid,
                PersonText = x.KnownUser?.DisplayName ?? (string.IsNullOrWhiteSpace(person.Name) ? person.Address : person.Name),
                Address = person.Address,
                KnownUserId = x.KnownUser?.UserId,
                Subject = string.IsNullOrWhiteSpace(x.Summary.Subject) ? "(intet emne)" : x.Summary.Subject,
                DateText = x.Summary.DateUtc.ToDanishRelativeText(nowUtc),
                Seen = x.Summary.Seen,
                HasAttachments = x.Summary.HasAttachments
            };
        }).ToList()
    };

    public static ReadMailboxViewModel ToReadMailboxViewModel(this MailboxMessageDto dto, bool showHtml)
    {
        var message = dto.Message;
        return new ReadMailboxViewModel
        {
            Folder = message.Folder.ToRouteValue(),
            FolderTitle = message.Folder.ToTitle(),
            Uid = message.Uid,
            Subject = string.IsNullOrWhiteSpace(message.Subject) ? "(intet emne)" : message.Subject,
            FromText = AddressText(message.From),
            KnownUserId = dto.Sender?.UserId,
            KnownUserName = dto.Sender?.DisplayName,
            ToText = string.Join(", ", message.To.Select(AddressText)),
            CcText = string.Join(", ", message.Cc.Select(AddressText)),
            DateText = message.DateUtc.ToDanishDateTimeText(),
            Text = dto.Text,
            SafeHtml = dto.SafeHtml,
            ShowHtml = dto.SafeHtml is not null && (showHtml || string.IsNullOrWhiteSpace(message.TextBody)),
            IsTrash = message.Folder == MailFolder.Trash,
            IsSent = message.Folder == MailFolder.Sent,
            Attachments = message.Attachments.Select(x => new MailAttachmentViewModel
            {
                Index = x.Index,
                FileName = x.FileName,
                SizeText = SizeText(x.SizeBytes)
            }).ToList()
        };
    }

    public static ReplyMailboxViewModel ToReplyMailboxViewModel(this ReplyDraftDto dto) => new()
    {
        Folder = dto.Folder.ToRouteValue(),
        ToText = dto.KnownUser is null ? AddressText(dto.To) : $"{dto.KnownUser.DisplayName} <{dto.To.Address}>",
        Subject = dto.Subject,
        Body = dto.Body
    };

    public static ReplyDto ToReplyDto(this ReplyMailboxViewModel model, uint uid) => new()
    {
        Folder = ToMailFolder(model.Folder),
        Uid = uid,
        Subject = model.Subject,
        Body = model.Body
    };

    public static ComposeDto ToComposeDto(this ComposeMailboxViewModel model) => new()
    {
        ToAddress = model.ToAddress,
        Subject = model.Subject,
        Body = model.Body
    };

    public static List<RecipientOptionViewModel> ToRecipientOptions(this List<RecipientDto> recipients) =>
        recipients.Select(x => new RecipientOptionViewModel { Email = x.Email, DisplayName = x.DisplayName }).ToList();

    private static string AddressText(MailAddressDto address) =>
        string.IsNullOrWhiteSpace(address.Name) ? address.Address : $"{address.Name} <{address.Address}>";

    private static string SizeText(long bytes) =>
        bytes >= 1024 * 1024 ? $"{bytes / 1024d / 1024d:0.0} MB" : $"{Math.Max(1, (int)Math.Ceiling(bytes / 1024d))} kB";
}
