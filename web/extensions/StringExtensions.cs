using System.Text;

namespace web.Extensions;

public static class StringExtensions
{
    /// <summary>Et sikkert filnavn uden mellemrum og specialtegn: "Skolefest 2027" → "skolefest-2027", æ/ø/å → ae/oe/aa.</summary>
    public static string ToFileNameSlug(this string value, int maxLength = 50)
    {
        var builder = new StringBuilder();
        foreach (var c in value.Trim().ToLowerInvariant())
        {
            switch (c)
            {
                case 'æ': builder.Append("ae"); break;
                case 'ø': builder.Append("oe"); break;
                case 'å': builder.Append("aa"); break;
                default:
                    if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
                    {
                        builder.Append(c);
                    }
                    else if (builder.Length > 0 && builder[^1] != '-')
                    {
                        builder.Append('-');
                    }

                    break;
            }
        }

        var slug = builder.ToString().Trim('-');
        slug = slug.Length > maxLength ? slug[..maxLength].Trim('-') : slug;
        return slug.Length == 0 ? "fil" : slug;
    }
}
