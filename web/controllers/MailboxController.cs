using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Infrastructure.Authentication;
using web.Managers.Mailbox.Dtos;
using web.Managers.Mailbox.Interfaces;
using web.ViewModels.Mailbox;

namespace web.Controllers;

/// <summary>
/// Webmail til systemets egen postkasse (krav K22): Indbakke, Sendt og Papirkurv. Kun administratorer, fordi
/// mails fra forældre kan indeholde persondata. Mails læses direkte fra IMAP og gemmes ikke i app.db.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class MailboxController(IMailboxManager mailboxManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? folder, string? q, int page, CancellationToken cancellationToken) =>
        View((await mailboxManager.GetPageAsync(MailboxViewModelMapper.ToMailFolder(folder), q, page, cancellationToken))
            .ToIndexMailboxViewModel(DateTime.UtcNow));

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Read(uint id, string? folder, bool html, CancellationToken cancellationToken)
    {
        var message = await mailboxManager.ReadAsync(MailboxViewModelMapper.ToMailFolder(folder), id, cancellationToken);
        return message is null ? NotFound() : View(message.ToReadMailboxViewModel(html));
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Attachment(uint id, string? folder, int index, CancellationToken cancellationToken)
    {
        var attachment = await mailboxManager.GetAttachmentAsync(MailboxViewModelMapper.ToMailFolder(folder), id, index, cancellationToken);
        if (attachment is null)
        {
            return NotFound();
        }

        // Filer fra fremmede afsendere hentes altid som download, aldrig som en side i appen (HTML/SVG/scripts).
        var contentType = IsSafeToKeep(attachment.ContentType) ? attachment.ContentType : "application/octet-stream";
        return File(attachment.Content, contentType, attachment.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> Reply(uint id, string? folder, CancellationToken cancellationToken)
    {
        var draft = await mailboxManager.GetReplyDraftAsync(MailboxViewModelMapper.ToMailFolder(folder), id, SignatureName(), cancellationToken);
        return draft is null ? NotFound() : View(draft.ToReplyMailboxViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Reply(uint id, ReplyMailboxViewModel model, CancellationToken cancellationToken)
    {
        var folder = MailboxViewModelMapper.ToMailFolder(model.Folder);
        if (!ModelState.IsValid)
        {
            return await ReplyAgainAsync(id, model, cancellationToken);
        }

        var result = await mailboxManager.SendReplyAsync(model.ToReplyDto(id), cancellationToken);
        if (result.Status == SendMailboxStatus.NotFound)
        {
            return NotFound();
        }

        if (result.Status != SendMailboxStatus.Sent)
        {
            ModelState.AddModelError(string.Empty, $"Svaret blev ikke sendt: {result.Error}");
            return await ReplyAgainAsync(id, model, cancellationToken);
        }

        TempData[result.SentCopyError is null ? "Success" : "Error"] = result.SentCopyError is null
            ? "Svaret er sendt."
            : $"Svaret er sendt, men kopien kunne ikke gemmes i Sendt: {result.SentCopyError}";
        return RedirectToAction(nameof(Read), new { id, folder = folder.ToRouteValue() });
    }

    [HttpGet]
    public async Task<IActionResult> Compose(string? to, CancellationToken cancellationToken) =>
        View(new ComposeMailboxViewModel
        {
            ToAddress = to ?? string.Empty,
            Recipients = (await mailboxManager.GetRecipientsAsync(cancellationToken)).ToRecipientOptions()
        });

    [HttpPost]
    public async Task<IActionResult> Compose(ComposeMailboxViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await mailboxManager.SendNewAsync(model.ToComposeDto(), cancellationToken);
            if (result.Status == SendMailboxStatus.Sent)
            {
                TempData[result.SentCopyError is null ? "Success" : "Error"] = result.SentCopyError is null
                    ? $"Mailen er sendt til {model.ToAddress.Trim()}."
                    : $"Mailen er sendt, men kopien kunne ikke gemmes i Sendt: {result.SentCopyError}";
                return RedirectToAction(nameof(Index), new { folder = "sent" });
            }

            ModelState.AddModelError(result.Status == SendMailboxStatus.InvalidRecipient ? nameof(model.ToAddress) : string.Empty,
                $"Mailen blev ikke sendt: {result.Error}");
        }

        model.Recipients = (await mailboxManager.GetRecipientsAsync(cancellationToken)).ToRecipientOptions();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> MarkUnread(uint id, string? folder, CancellationToken cancellationToken)
    {
        var mailFolder = MailboxViewModelMapper.ToMailFolder(folder);
        if (!await mailboxManager.MarkUnreadAsync(mailFolder, id, cancellationToken))
        {
            return NotFound();
        }

        TempData["Success"] = "Mailen er markeret som ulæst.";
        return RedirectToAction(nameof(Index), new { folder = mailFolder.ToRouteValue() });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(uint id, string? folder, CancellationToken cancellationToken)
    {
        var mailFolder = MailboxViewModelMapper.ToMailFolder(folder);
        if (!await mailboxManager.DeleteAsync(mailFolder, id, cancellationToken))
        {
            return NotFound();
        }

        TempData["Success"] = mailFolder.ToRouteValue() == "trash" ? "Mailen er slettet." : "Mailen er flyttet til Papirkurv.";
        return RedirectToAction(nameof(Index), new { folder = mailFolder.ToRouteValue() });
    }

    private async Task<IActionResult> ReplyAgainAsync(uint id, ReplyMailboxViewModel model, CancellationToken cancellationToken)
    {
        // Modtageren vises igen fra den oprindelige mail (feltet sendes ikke med formularen).
        var draft = await mailboxManager.GetReplyDraftAsync(MailboxViewModelMapper.ToMailFolder(model.Folder), id, SignatureName(), cancellationToken);
        if (draft is null)
        {
            return NotFound();
        }

        model.ToText = draft.ToReplyMailboxViewModel().ToText;
        return View(nameof(Reply), model);
    }

    private string SignatureName() => User.FindFirst(UserClaimTypes.DisplayName)?.Value ?? "Vagttilmelding";

    private static bool IsSafeToKeep(string contentType) =>
        contentType is "application/pdf" or "image/png" or "image/jpeg" or "image/gif" or "image/webp" or
            "text/plain" or "text/csv" or "message/rfc822" ||
        contentType.StartsWith("application/vnd.openxmlformats", StringComparison.Ordinal) ||
        contentType.StartsWith("application/vnd.ms-", StringComparison.Ordinal);
}
