using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Delp.Core.Tools.Encoding;

public static class HtmlEntityTool
{
    /// <summary>HTML-encodes reserved characters (&amp; &lt; &gt; " '). <see cref="WebUtility.HtmlEncode"/>
    /// already emits every character above U+007F as a decimal numeric entity, merging UTF-16 surrogate
    /// pairs into a single entity per Unicode codepoint. When <paramref name="nonAsciiToNumeric"/> is true,
    /// those decimal entities (and any raw non-ASCII character that slips through) are reformatted as
    /// hex entities (&amp;#xXXXX;) instead — the more common convention for numeric character references.</summary>
    public static string Encode(string text, bool nonAsciiToNumeric)
    {
        var encoded = WebUtility.HtmlEncode(text ?? "") ?? "";
        if (!nonAsciiToNumeric)
            return encoded;

        // Reformat decimal numeric entities WebUtility.HtmlEncode already produced as hex.
        encoded = Regex.Replace(encoded, "&#([0-9]+);", m =>
        {
            var codepoint = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            return "&#x" + codepoint.ToString("X", CultureInfo.InvariantCulture) + ";";
        }, RegexOptions.None, TimeSpan.FromSeconds(2));

        // Defensively handle any raw non-ASCII character WebUtility.HtmlEncode left untouched.
        var sb = new StringBuilder(encoded.Length);
        for (var i = 0; i < encoded.Length; i++)
        {
            var c = encoded[i];
            if (c <= 0x7F)
            {
                sb.Append(c);
                continue;
            }

            int codepoint;
            if (char.IsHighSurrogate(c) && i + 1 < encoded.Length && char.IsLowSurrogate(encoded[i + 1]))
            {
                codepoint = char.ConvertToUtf32(c, encoded[i + 1]);
                i++;
            }
            else
            {
                codepoint = c;
            }

            sb.Append("&#x").Append(codepoint.ToString("X", CultureInfo.InvariantCulture)).Append(';');
        }

        return sb.ToString();
    }

    /// <summary>Decodes named (&amp;nbsp; &amp;mdash; …) and numeric entities. Unknown entities pass through unchanged.</summary>
    public static string Decode(string text) => WebUtility.HtmlDecode(text ?? "") ?? "";
}
