using System.Net;
using System.Text;

namespace Delp.Core.Tools.Encoding;

public enum UrlEncodeMode
{
    /// <summary>Percent-encodes everything except unreserved characters (RFC 3986 §2.3). <see cref="Uri.EscapeDataString"/>.</summary>
    Component,

    /// <summary>application/x-www-form-urlencoded: space becomes '+'. <see cref="WebUtility.UrlEncode"/>.</summary>
    FormData,

    /// <summary>Like Component, but leaves the general URI delimiter/sub-delimiter characters unescaped.</summary>
    PreserveUriChars,
}

public static class UrlEncodeTool
{
    /// <summary>Characters left unescaped by <see cref="UrlEncodeMode.PreserveUriChars"/> in addition to unreserved characters.</summary>
    private const string PreservedChars = ":/?#[]@!$&'()*+,;=";

    public static string Encode(string text, UrlEncodeMode mode)
    {
        text ??= "";
        return mode switch
        {
            UrlEncodeMode.Component => Uri.EscapeDataString(text),
            UrlEncodeMode.FormData => WebUtility.UrlEncode(text) ?? "",
            UrlEncodeMode.PreserveUriChars => EncodePreserving(text),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown URL encode mode."),
        };
    }

    /// <exception cref="FormatException">The input contains a malformed percent-encoded sequence.</exception>
    public static string Decode(string text, UrlEncodeMode mode)
    {
        text ??= "";
        var s = mode == UrlEncodeMode.FormData ? text.Replace('+', ' ') : text;
        ValidatePercentEncoding(s);
        return Uri.UnescapeDataString(s);
    }

    private static string EncodePreserving(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var sb = new StringBuilder(bytes.Length);
        foreach (var b in bytes)
        {
            var c = (char)b;
            var isUnreserved = b < 0x80 && (char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or '~');
            if (isUnreserved || (b < 0x80 && PreservedChars.IndexOf(c) >= 0))
                sb.Append(c);
            else
                sb.Append('%').Append(b.ToString("X2"));
        }
        return sb.ToString();
    }

    private static void ValidatePercentEncoding(string s)
    {
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] != '%')
                continue;

            var hasPair = i + 2 < s.Length && Uri.IsHexDigit(s[i + 1]) && Uri.IsHexDigit(s[i + 2]);
            if (!hasPair)
            {
                var end = Math.Min(i + 3, s.Length);
                throw new FormatException($"Invalid percent sequence '{s[i..end]}' at position {i}");
            }
        }
    }
}
