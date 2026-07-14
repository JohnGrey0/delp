using System.Globalization;
using System.Text;

namespace Delp.Core.Tools.Encoding;

public static class UnicodeEscapeTool
{
    /// <summary>Escapes text as \uXXXX per UTF-16 code unit (astral characters become two escapes).
    /// \n \r \t \\ \" are always emitted as their short forms. When <paramref name="nonAsciiOnly"/> is
    /// true, printable ASCII characters are left as-is.</summary>
    public static string Escape(string text, bool nonAsciiOnly)
    {
        var sb = new StringBuilder((text ?? "").Length);
        foreach (var ch in text ?? "")
        {
            switch (ch)
            {
                case '\n': sb.Append("\\n"); continue;
                case '\r': sb.Append("\\r"); continue;
                case '\t': sb.Append("\\t"); continue;
                case '\\': sb.Append("\\\\"); continue;
                case '\"': sb.Append("\\\""); continue;
            }

            if (nonAsciiOnly && ch <= 0x7F)
            {
                sb.Append(ch);
                continue;
            }

            sb.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    /// <summary>Unescapes \uXXXX, \U0001F600 (8-digit codepoint), \xNN, and \n \r \t \0 \\ \" \'.</summary>
    /// <exception cref="FormatException">A trailing backslash, unknown escape, invalid hex digits, or an
    /// out-of-range \U codepoint was found.</exception>
    public static string Unescape(string text)
    {
        text ??= "";
        var sb = new StringBuilder(text.Length);
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c != '\\')
            {
                sb.Append(c);
                i++;
                continue;
            }

            if (i + 1 >= text.Length)
                throw new FormatException($"Trailing backslash at position {i}.");

            var next = text[i + 1];
            switch (next)
            {
                case 'n': sb.Append('\n'); i += 2; break;
                case 'r': sb.Append('\r'); i += 2; break;
                case 't': sb.Append('\t'); i += 2; break;
                case '0': sb.Append('\0'); i += 2; break;
                case '\\': sb.Append('\\'); i += 2; break;
                case '\"': sb.Append('\"'); i += 2; break;
                case '\'': sb.Append('\''); i += 2; break;
                case 'u': AppendFixedHex(text, ref i, sb, 4, "\\u"); break;
                case 'U': AppendUnicodeCodepoint(text, ref i, sb); break;
                case 'x': AppendFixedHex(text, ref i, sb, 2, "\\x"); break;
                default: throw new FormatException($"Unknown escape sequence '\\{next}' at position {i}.");
            }
        }

        return sb.ToString();
    }

    private static void AppendFixedHex(string text, ref int i, StringBuilder sb, int digits, string tag)
    {
        if (i + 2 + digits > text.Length)
            throw new FormatException($"Incomplete {tag} escape at position {i}.");

        var hex = text.Substring(i + 2, digits);
        if (!TryParseHex(hex, out var value))
            throw new FormatException($"Invalid hex digits '{hex}' in {tag} escape at position {i}.");

        sb.Append((char)value);
        i += 2 + digits;
    }

    private static void AppendUnicodeCodepoint(string text, ref int i, StringBuilder sb)
    {
        const int digits = 8;
        if (i + 2 + digits > text.Length)
            throw new FormatException($"Incomplete \\U escape at position {i}.");

        var hex = text.Substring(i + 2, digits);
        if (!TryParseHex(hex, out var value))
            throw new FormatException($"Invalid hex digits '{hex}' in \\U escape at position {i}.");

        if (value > 0x10FFFF)
            throw new FormatException($"Codepoint U+{value:X} in \\U escape at position {i} exceeds U+10FFFF.");

        sb.Append(char.ConvertFromUtf32(value));
        i += 2 + digits;
    }

    private static bool TryParseHex(string hex, out int value)
    {
        value = 0;
        foreach (var c in hex)
            if (!Uri.IsHexDigit(c))
                return false;

        value = int.Parse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        return true;
    }
}
