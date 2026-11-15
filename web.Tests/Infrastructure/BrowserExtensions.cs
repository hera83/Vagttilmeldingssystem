using System.Net;
using System.Text.RegularExpressions;

namespace web.Tests.Infrastructure;

/// <summary>Hjælpere, der opfører sig som en browser: henter anti-forgery-token fra formularen og poster den.</summary>
public static partial class BrowserExtensions
{
    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();

    public static async Task<string> GetAntiforgeryTokenAsync(this HttpClient client, string formUrl)
    {
        var response = await client.GetAsync(formUrl);
        var html = await response.Content.ReadAsStringAsync();
        var match = TokenRegex().Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Ingen anti-forgery-token på {formUrl} (status {(int)response.StatusCode}).");
        }

        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    /// <summary>Henter formularen på <paramref name="formUrl"/> og poster felterne med dens token til <paramref name="postUrl"/>.</summary>
    public static async Task<HttpResponseMessage> PostFormAsync(this HttpClient client, string formUrl, string postUrl, IEnumerable<KeyValuePair<string, string>> fields)
    {
        var token = await client.GetAntiforgeryTokenAsync(formUrl);
        var all = fields.Append(new KeyValuePair<string, string>("__RequestVerificationToken", token)).ToList();
        return await client.PostAsync(postUrl, new FormUrlEncodedContent(all));
    }

    public static Task<HttpResponseMessage> PostFormAsync(this HttpClient client, string formUrl, string postUrl, params (string Key, string Value)[] fields) =>
        client.PostFormAsync(formUrl, postUrl, fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)));

    public static async Task LoginAsync(this HttpClient client, string userName, string password)
    {
        var response = await client.PostFormAsync("/Account/Login", "/Account/Login", ("UserName", userName), ("Password", password));
        if (response.StatusCode != HttpStatusCode.Redirect)
        {
            throw new InvalidOperationException($"Login som {userName} fejlede (status {(int)response.StatusCode}).");
        }
    }

    public static async Task<string> GetHtmlAsync(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }
}
