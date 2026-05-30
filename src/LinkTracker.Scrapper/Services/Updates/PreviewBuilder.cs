using System.Net;
using System.Text.RegularExpressions;

namespace LinkTracker.Scrapper.Services.Updates;

public static partial class PreviewBuilder
{
    public static string Build(string? text)
    {
        var decoded = WebUtility.HtmlDecode(text ?? string.Empty);
        var plainText = HtmlTagRegex().Replace(decoded, string.Empty).Trim();

        return plainText.Length <= 200 ? plainText : plainText[..200];
    }

    [GeneratedRegex("<.*?>")]
    private static partial Regex HtmlTagRegex();
}