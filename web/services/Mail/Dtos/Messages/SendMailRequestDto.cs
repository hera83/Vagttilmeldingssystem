namespace web.Services.Mail.Dtos.Messages;

public class SendMailRequestDto
{
    public string ToAddress { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string TextBody { get; set; } = string.Empty;

    /// <summary>Valgfri HTML-udgave. Uden den sendes kun tekst.</summary>
    public string? HtmlBody { get; set; }

    /// <summary>Svar: Message-ID på den mail, der svares på (In-Reply-To), så svaret bliver i samme tråd.</summary>
    public string? InReplyTo { get; set; }

    /// <summary>Svar: trådens Message-ID'er (References).</summary>
    public List<string> References { get; set; } = [];

    /// <summary>Gem en kopi i postkassens Sendt-mappe (kun mails skrevet i webmailen, og kun med Mail:Imap:SaveSentCopy).</summary>
    public bool SaveCopyToSent { get; set; }
}
