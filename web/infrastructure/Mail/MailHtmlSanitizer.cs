using Ganss.Xss;

namespace web.Infrastructure.Mail;

/// <summary>
/// Renser HTML fra mails, før den vises i webmailen: ingen scripts, event-attributter, formularer, iframes, styles
/// eller billeder (eksterne billeder er ofte sporingspixels). Links åbner i et nyt vindue uden referrer. Singleton.
/// </summary>
public sealed class MailHtmlSanitizer
{
    private static readonly string[] RemovedTags =
        ["img", "picture", "source", "video", "audio", "iframe", "frame", "object", "embed", "form", "input", "button", "select", "textarea", "style", "link", "meta", "svg", "math"];

    private readonly HtmlSanitizer _sanitizer;

    public MailHtmlSanitizer()
    {
        _sanitizer = new HtmlSanitizer();
        foreach (var tag in RemovedTags)
        {
            _sanitizer.AllowedTags.Remove(tag);
        }

        _sanitizer.AllowedAttributes.Remove("style");
        _sanitizer.AllowedAttributes.Remove("background");
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("mailto");
        _sanitizer.AllowedAttributes.Add("target");
        _sanitizer.AllowedAttributes.Add("rel");
        _sanitizer.PostProcessNode += (_, e) =>
        {
            if (e.Node is AngleSharp.Dom.IElement { TagName: "A" } link)
            {
                link.SetAttribute("target", "_blank");
                link.SetAttribute("rel", "noopener noreferrer nofollow");
            }
        };
    }

    public string Sanitize(string html) => _sanitizer.Sanitize(html);
}
