using Microsoft.AspNetCore.Razor.TagHelpers;

namespace web.Infrastructure.Views;

/// <summary>
/// &lt;icon name="calendar" /&gt; bliver til en inline SVG, der bruger symbolerne i views/Shared/_IconSprite.cshtml.
/// Ikonerne er dekorative (aria-hidden). Teksten ved siden af bærer betydningen.
/// </summary>
[HtmlTargetElement("icon", TagStructure = TagStructure.WithoutEndTag)]
public class IconTagHelper : TagHelper
{
    public string Name { get; set; } = string.Empty;

    public string? Class { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", string.IsNullOrWhiteSpace(Class) ? "icon" : $"icon {Class}");
        output.Attributes.SetAttribute("aria-hidden", "true");
        output.Attributes.SetAttribute("focusable", "false");
        output.Content.SetHtmlContent($"<use href=\"#i-{System.Net.WebUtility.HtmlEncode(Name)}\"></use>");
    }
}
