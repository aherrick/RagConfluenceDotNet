using System.Net;
using System.Text.RegularExpressions;

namespace RagConfluenceDotNet.Helpers;

public partial class Utils
{
    public static string CleanHtml(string input)
    {
        return WebUtility.HtmlDecode(MyRegex().Replace(input, " "));
    }

    [GeneratedRegex("<.*?>")]
    private static partial Regex MyRegex();
}