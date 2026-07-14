using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Delp.Core.Tools.TextProcessing;

/// <summary>Embedding contexts supported by <see cref="EscapeTool"/>.</summary>
public enum EscapeTarget
{
    Json,
    XmlHtml,
    Csv,
    CSharp,
    JavaScript,
    Sql,
    Regex,
    Url,
}

/// <summary>Escapes/unescapes text for common embedding targets: JSON string bodies,
/// XML/HTML text, CSV cells (RFC 4180), C# and JavaScript string literals, SQL string
/// literals, regex literals, and URL components. Every target supports both directions.</summary>
public static class EscapeTool
{
    public static string Escape(EscapeTarget target, string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return target switch
        {
            EscapeTarget.Json => JsonEncodedText.Encode(input).ToString(),
            EscapeTarget.XmlHtml => EscapeXmlHtml(input),
            EscapeTarget.Csv => EscapeCsv(input),
            EscapeTarget.CSharp => EscapeCLike(input, quote: '"'),
            EscapeTarget.JavaScript => EscapeCLike(input, quote: '\''),
            EscapeTarget.Sql => input.Replace("'", "''"),
            EscapeTarget.Regex => System.Text.RegularExpressions.Regex.Escape(input),
            EscapeTarget.Url => Uri.EscapeDataString(input),
            _ => throw new ArgumentOutOfRangeException(nameof(target)),
        };
    }

    /// <exception cref="FormatException">The input is not validly escaped for the target.</exception>
    public static string Unescape(EscapeTarget target, string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return target switch
        {
            EscapeTarget.Json => UnescapeJson(input),
            EscapeTarget.XmlHtml => UnescapeXmlHtml(input),
            EscapeTarget.Csv => UnescapeCsv(input),
            EscapeTarget.CSharp => UnescapeCLike(input),
            EscapeTarget.JavaScript => UnescapeCLike(input),
            EscapeTarget.Sql => input.Replace("''", "'"),
            EscapeTarget.Regex => System.Text.RegularExpressions.Regex.Unescape(input),
            EscapeTarget.Url => Uri.UnescapeDataString(input),
            _ => throw new ArgumentOutOfRangeException(nameof(target)),
        };
    }

    // ---------------------------------------------------------------- JSON

    private static string UnescapeJson(string input)
    {
        try
        {
            using var doc = JsonDocument.Parse("\"" + input + "\"");
            return doc.RootElement.GetString() ?? "";
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Invalid JSON string escape: {ex.Message}", ex);
        }
    }

    // ------------------------------------------------------------ XML/HTML

    private static string EscapeXmlHtml(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            sb.Append(c switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' => "&quot;",
                '\'' => "&apos;",
                _ => c.ToString(),
            });
        }
        return sb.ToString();
    }

    private static string UnescapeXmlHtml(string input) =>
        input
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Replace("&apos;", "'")
            .Replace("&amp;", "&"); // must run last, or "&amp;lt;" would double-unescape

    // ----------------------------------------------------------------- CSV

    /// <summary>RFC 4180 quoting for a single CSV field/cell.</summary>
    private static string EscapeCsv(string input)
    {
        var needsQuoting = input.IndexOfAny([',', '"', '\n', '\r']) >= 0;
        return needsQuoting ? "\"" + input.Replace("\"", "\"\"") + "\"" : input;
    }

    private static string UnescapeCsv(string input)
    {
        if (input.Length >= 2 && input[0] == '"' && input[^1] == '"')
            return input[1..^1].Replace("\"\"", "\"");
        return input;
    }

    // ------------------------------------------------------- C# / JavaScript

    private static string EscapeCLike(string input, char quote)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\0': sb.Append("\\0"); break;
                case '\a': sb.Append("\\a"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\v': sb.Append("\\v"); break;
                default:
                    if (c == quote)
                        sb.Append('\\').Append(c);
                    else if (char.IsControl(c))
                        sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>Unescapes standard C-family backslash sequences: <c>\\ \" \' \n \r \t \0
    /// \a \b \f \v</c>, <c>\xNN</c> (2 hex digits), <c>\uXXXX</c> (4 hex digits) and
    /// <c>\UXXXXXXXX</c> (8 hex digits).</summary>
    private static string UnescapeCLike(string input)
    {
        var sb = new StringBuilder(input.Length);
        var i = 0;
        while (i < input.Length)
        {
            var c = input[i];
            if (c != '\\')
            {
                sb.Append(c);
                i++;
                continue;
            }

            if (i + 1 >= input.Length)
                throw new FormatException($"Trailing escape character '\\' at position {i}.");

            var next = input[i + 1];
            switch (next)
            {
                case '\\': sb.Append('\\'); i += 2; break;
                case '"': sb.Append('"'); i += 2; break;
                case '\'': sb.Append('\''); i += 2; break;
                case 'n': sb.Append('\n'); i += 2; break;
                case 'r': sb.Append('\r'); i += 2; break;
                case 't': sb.Append('\t'); i += 2; break;
                case '0': sb.Append('\0'); i += 2; break;
                case 'a': sb.Append('\a'); i += 2; break;
                case 'b': sb.Append('\b'); i += 2; break;
                case 'f': sb.Append('\f'); i += 2; break;
                case 'v': sb.Append('\v'); i += 2; break;
                case 'x': i = AppendHexEscape(input, i, 2, sb); break;
                case 'u': i = AppendHexEscape(input, i, 4, sb); break;
                case 'U': i = AppendUnicodeEscape(input, i, sb); break;
                default:
                    throw new FormatException($"Unknown escape sequence '\\{next}' at position {i}.");
            }
        }
        return sb.ToString();
    }

    private static int AppendHexEscape(string input, int i, int digitCount, StringBuilder sb)
    {
        var start = i + 2;
        if (start + digitCount > input.Length)
            throw new FormatException($"Incomplete escape sequence at position {i}.");
        var hex = input.Substring(start, digitCount);
        if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
            throw new FormatException($"Invalid hex digits '{hex}' in escape sequence at position {i}.");
        sb.Append((char)code);
        return start + digitCount;
    }

    private static int AppendUnicodeEscape(string input, int i, StringBuilder sb)
    {
        var start = i + 2;
        if (start + 8 > input.Length)
            throw new FormatException($"Incomplete \\U escape sequence at position {i}.");
        var hex = input.Substring(start, 8);
        if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code) || code > 0x10FFFF)
            throw new FormatException($"Invalid \\U escape sequence '{hex}' at position {i}.");
        sb.Append(char.ConvertFromUtf32((int)code));
        return start + 8;
    }
}
