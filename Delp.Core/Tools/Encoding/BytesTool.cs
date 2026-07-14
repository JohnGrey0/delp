using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Delp.Core.Tools.Encoding;

/// <summary>Converts between raw bytes and their text, hex, binary, and decimal-list representations.</summary>
public static class BytesTool
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.None, RegexTimeout);
    private static readonly Regex HexSeparatorRegex = new(@"[\s,]+", RegexOptions.None, RegexTimeout);
    private static readonly Regex HexPrefixRegex = new("0[xX]", RegexOptions.None, RegexTimeout);

    public static byte[] FromText(string text) => System.Text.Encoding.UTF8.GetBytes(text ?? "");

    /// <exception cref="FormatException">The hex string has odd length or contains a non-hex character.</exception>
    public static byte[] FromHex(string hex)
    {
        var cleaned = CleanHex(hex);
        if (cleaned.Length % 2 != 0)
            throw new FormatException($"Hex string has odd length ({cleaned.Length} characters); each byte needs 2 digits.");

        var bytes = new byte[cleaned.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            var hi = cleaned[i * 2];
            var lo = cleaned[i * 2 + 1];
            if (!Uri.IsHexDigit(hi))
                throw new FormatException($"Invalid hex character '{hi}' at position {i * 2}.");
            if (!Uri.IsHexDigit(lo))
                throw new FormatException($"Invalid hex character '{lo}' at position {i * 2 + 1}.");

            bytes[i] = byte.Parse(cleaned.AsSpan(i * 2, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        }

        return bytes;
    }

    /// <exception cref="FormatException">The binary string length is not a multiple of 8 or contains a non-0/1 character.</exception>
    public static byte[] FromBinary(string binary)
    {
        var cleaned = WhitespaceRegex.Replace(binary ?? "", "");
        if (cleaned.Length % 8 != 0)
            throw new FormatException($"Binary string length ({cleaned.Length}) is not a multiple of 8.");

        var bytes = new byte[cleaned.Length / 8];
        for (var i = 0; i < bytes.Length; i++)
        {
            byte value = 0;
            for (var bit = 0; bit < 8; bit++)
            {
                var c = cleaned[i * 8 + bit];
                if (c != '0' && c != '1')
                    throw new FormatException($"Invalid binary character '{c}' at position {i * 8 + bit}.");
                value = (byte)((value << 1) | (c - '0'));
            }

            bytes[i] = value;
        }

        return bytes;
    }

    /// <exception cref="FormatException">A token is not an integer in the range 0-255.</exception>
    public static byte[] FromDecimalBytes(string text)
    {
        var tokens = (text ?? "").Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var bytes = new byte[tokens.Length];
        for (var i = 0; i < tokens.Length; i++)
        {
            if (!int.TryParse(tokens[i], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) || value is < 0 or > 255)
                throw new FormatException($"'{tokens[i]}' is not a valid byte value (0-255).");
            bytes[i] = (byte)value;
        }

        return bytes;
    }

    /// <summary>Decodes as UTF-8, substituting U+FFFD for invalid byte sequences.</summary>
    public static string ToText(byte[] bytes) => System.Text.Encoding.UTF8.GetString(bytes);

    public static string ToHex(byte[] bytes, bool spaced, bool uppercase)
    {
        var format = uppercase ? "X2" : "x2";
        var sb = new StringBuilder(bytes.Length * (spaced ? 3 : 2));
        for (var i = 0; i < bytes.Length; i++)
        {
            if (spaced && i > 0)
                sb.Append(' ');
            sb.Append(bytes[i].ToString(format, CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    public static string ToBinary(byte[] bytes, bool spaced)
    {
        var sb = new StringBuilder(bytes.Length * (spaced ? 9 : 8));
        for (var i = 0; i < bytes.Length; i++)
        {
            if (spaced && i > 0)
                sb.Append(' ');
            sb.Append(Convert.ToString(bytes[i], 2).PadLeft(8, '0'));
        }

        return sb.ToString();
    }

    public static string ToDecimalBytes(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 4);
        for (var i = 0; i < bytes.Length; i++)
        {
            if (i > 0)
                sb.Append(' ');
            sb.Append(bytes[i].ToString(CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    private static string CleanHex(string hex)
    {
        var noSeparators = HexSeparatorRegex.Replace(hex ?? "", "");
        return HexPrefixRegex.Replace(noSeparators, "");
    }
}
