namespace Delp.Core.Tools.Encoding;

/// <summary>Caesar-shift cipher for ASCII letters (ROT13 is the shift-13 special case, its own inverse).</summary>
public static class Rot13Tool
{
    /// <summary>Shifts letters by <paramref name="n"/> positions (wrapping within their case); every other
    /// character passes through unchanged. <paramref name="n"/> is normalized modulo 26 and may be negative.</summary>
    public static string Shift(string text, int n)
    {
        text ??= "";
        var shift = ((n % 26) + 26) % 26;
        if (shift == 0)
            return text;

        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            if (c is >= 'a' and <= 'z')
                chars[i] = (char)('a' + (c - 'a' + shift) % 26);
            else if (c is >= 'A' and <= 'Z')
                chars[i] = (char)('A' + (c - 'A' + shift) % 26);
        }

        return new string(chars);
    }
}
