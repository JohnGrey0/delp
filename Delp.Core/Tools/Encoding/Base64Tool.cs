using System.Text;

namespace Delp.Core.Tools.Encoding;

public static class Base64Tool
{
    public static string Encode(string text, bool urlSafe = false)
    {
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
        return urlSafe
            ? encoded.Replace('+', '-').Replace('/', '_').TrimEnd('=')
            : encoded;
    }

    /// <summary>Decodes standard or URL-safe Base64; missing padding is tolerated.</summary>
    /// <exception cref="FormatException">The input is not valid Base64.</exception>
    public static string Decode(string base64, bool urlSafe = false)
    {
        var s = base64.Trim();
        if (s.Length == 0)
            return "";
        if (urlSafe || s.Contains('-') || s.Contains('_'))
            s = s.Replace('-', '+').Replace('_', '/');
        s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        var bytes = Convert.FromBase64String(s);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
