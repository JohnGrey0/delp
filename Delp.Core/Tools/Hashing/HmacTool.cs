using System.Security.Cryptography;

namespace Delp.Core.Tools.Hashing;

/// <summary>How raw text typed into the UI should be interpreted as bytes.</summary>
public enum InputInterpretation
{
    Utf8,
    Hex,
    Base64,
}

public static class HmacTool
{
    /// <summary>MD5, SHA-1, SHA-256, SHA-384, SHA-512 in that order.</summary>
    public static readonly IReadOnlyList<string> Algorithms = HashAlgorithms.All;

    /// <summary>Computes an HMAC of <paramref name="message"/> keyed by <paramref name="key"/>.</summary>
    /// <exception cref="ArgumentException">The algorithm name is not recognized.</exception>
    public static byte[] Compute(string algorithm, byte[] key, byte[] message)
    {
        var algoName = HashAlgorithms.ToHashAlgorithmName(algorithm);
        using var hmac = IncrementalHash.CreateHMAC(algoName, key);
        hmac.AppendData(message);
        return hmac.GetHashAndReset();
    }

    /// <summary>
    /// Parses UI text into bytes for use as an HMAC key or message, per the chosen
    /// interpretation. Hex tolerates whitespace, a leading "0x", and an odd digit
    /// count is rejected; Base64 tolerates missing padding.
    /// </summary>
    /// <exception cref="FormatException">The text is not valid for the chosen interpretation.</exception>
    public static byte[] ParseInput(string text, InputInterpretation interpretation)
    {
        text ??= "";
        return interpretation switch
        {
            InputInterpretation.Utf8 => System.Text.Encoding.UTF8.GetBytes(text),
            InputInterpretation.Hex => ParseHex(text),
            InputInterpretation.Base64 => ParseBase64(text),
            _ => throw new ArgumentOutOfRangeException(nameof(interpretation)),
        };
    }

    private static byte[] ParseHex(string text)
    {
        var s = new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            s = s[2..];
        if (s.Length == 0)
            return [];
        try
        {
            return Convert.FromHexString(s);
        }
        catch (FormatException)
        {
            throw new FormatException("Not valid hexadecimal (expected an even number of hex digits).");
        }
    }

    private static byte[] ParseBase64(string text)
    {
        var s = text.Trim();
        if (s.Length == 0)
            return [];
        s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        try
        {
            return Convert.FromBase64String(s);
        }
        catch (FormatException)
        {
            throw new FormatException("Not valid Base64.");
        }
    }
}
