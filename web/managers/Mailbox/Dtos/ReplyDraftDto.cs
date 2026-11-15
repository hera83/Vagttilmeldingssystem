using web.Services.Mail.Dtos.Mailbox;

namespace web.Managers.Mailbox.Dtos;

/// <summary>Et udfyldt svar: modtager (Reply-To eller afsender), "Re: emne" og citat af den oprindelige tekst.</summary>
public class ReplyDraftDto
{
    public MailFolder Folder { get; set; }
    public uint Uid { get; set; }
    public MailAddressDto To { get; set; } = new();
    public KnownUserDto? KnownUser { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
