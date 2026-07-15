using System.Numerics;
using System.Text;

namespace Delp.Core.Tools.Encoding;

/// <summary>Alphabets supported by <see cref="BaseNTool"/>. Base64 (standard and URL-safe)
/// stays owned by <see cref="Base64Tool"/> — this class covers the rest.</summary>
public enum BaseNAlphabet
{
    Base32,
    Base32Crockford,
    Base58,
    Ascii85,
}

/// <summary>Text ↔ Base32 (RFC 4648 + Crockford), Base58 (Bitcoin), and Ascii85 conversions.
/// Every alphabet round-trips arbitrary UTF-8 text; see <see cref="Base64Tool"/> for
/// Base64/Base64-URL.</summary>
public static class BaseNTool
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const string CrockfordAlphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    public static string Encode(string text, BaseNAlphabet alphabet) =>
        EncodeBytes(System.Text.Encoding.UTF8.GetBytes(text), alphabet);

    /// <exception cref="FormatException">The input is not valid for the chosen alphabet.</exception>
    public static string Decode(string encoded, BaseNAlphabet alphabet) =>
        System.Text.Encoding.UTF8.GetString(DecodeBytes(encoded, alphabet));

    public static string EncodeBytes(byte[] data, BaseNAlphabet alphabet) => alphabet switch
    {
        BaseNAlphabet.Base32 => Base32Encode(data, Base32Alphabet, pad: true),
        BaseNAlphabet.Base32Crockford => Base32Encode(data, CrockfordAlphabet, pad: false),
        BaseNAlphabet.Base58 => Base58Encode(data),
        BaseNAlphabet.Ascii85 => Ascii85Encode(data),
        _ => throw new ArgumentOutOfRangeException(nameof(alphabet)),
    };

    /// <exception cref="FormatException">The input is not valid for the chosen alphabet.</exception>
    public static byte[] DecodeBytes(string encoded, BaseNAlphabet alphabet) => alphabet switch
    {
        BaseNAlphabet.Base32 => Base32Decode(encoded, Base32Alphabet, crockfordAliases: false),
        BaseNAlphabet.Base32Crockford => Base32Decode(encoded, CrockfordAlphabet, crockfordAliases: true),
        BaseNAlphabet.Base58 => Base58Decode(encoded),
        BaseNAlphabet.Ascii85 => Ascii85Decode(encoded),
        _ => throw new ArgumentOutOfRangeException(nameof(alphabet)),
    };

    // ---------------------------------------------------------------- Base32 (RFC 4648 / Crockford)

    private static string Base32Encode(byte[] data, string alphabet, bool pad)
    {
        if (data.Length == 0)
            return "";

        var sb = new StringBuilder((data.Length + 4) / 5 * 8);
        int bitBuffer = 0, bitCount = 0;
        foreach (var b in data)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitCount += 8;
            while (bitCount >= 5)
            {
                bitCount -= 5;
                sb.Append(alphabet[(bitBuffer >> bitCount) & 0x1F]);
            }
        }
        if (bitCount > 0)
            sb.Append(alphabet[(bitBuffer << (5 - bitCount)) & 0x1F]);

        if (pad)
            while (sb.Length % 8 != 0)
                sb.Append('=');

        return sb.ToString();
    }

    /// <summary>Case-insensitive; tolerates missing or extra '=' padding. Crockford mode maps
    /// the commonly-confused I/L → 1 and O → 0 before lookup.</summary>
    private static byte[] Base32Decode(string s, string alphabet, bool crockfordAliases)
    {
        var cleaned = new string(s.Where(c => !char.IsWhiteSpace(c)).ToArray()).TrimEnd('=').ToUpperInvariant();
        if (cleaned.Length == 0)
            return [];

        if (crockfordAliases)
            cleaned = cleaned.Replace('I', '1').Replace('L', '1').Replace('O', '0');

        var bytes = new List<byte>(cleaned.Length * 5 / 8);
        int bitBuffer = 0, bitCount = 0;
        foreach (var c in cleaned)
        {
            var index = alphabet.IndexOf(c);
            if (index < 0)
                throw new FormatException($"'{c}' is not a valid {(crockfordAliases ? "Crockford " : "")}Base32 character.");

            bitBuffer = (bitBuffer << 5) | index;
            bitCount += 5;
            if (bitCount >= 8)
            {
                bitCount -= 8;
                bytes.Add((byte)((bitBuffer >> bitCount) & 0xFF));
            }
        }
        return bytes.ToArray();
    }

    // ---------------------------------------------------------------------------- Base58 (Bitcoin)

    /// <summary>Leading zero bytes become leading '1' characters (index 0 in the alphabet), so
    /// e.g. <c>"\0\0abc"</c> round-trips instead of losing its leading zeros to BigInteger.</summary>
    private static string Base58Encode(byte[] data)
    {
        if (data.Length == 0)
            return "";

        var leadingZeros = 0;
        while (leadingZeros < data.Length && data[leadingZeros] == 0)
            leadingZeros++;

        var value = new BigInteger(data, isUnsigned: true, isBigEndian: true);
        var sb = new StringBuilder();
        while (value > 0)
        {
            value = BigInteger.DivRem(value, 58, out var remainder);
            sb.Insert(0, Base58Alphabet[(int)remainder]);
        }

        return new string('1', leadingZeros) + sb;
    }

    /// <exception cref="FormatException">The input contains a character outside the Base58
    /// alphabet.</exception>
    private static byte[] Base58Decode(string s)
    {
        var trimmed = s.Trim();
        if (trimmed.Length == 0)
            return [];

        var leadingOnes = 0;
        while (leadingOnes < trimmed.Length && trimmed[leadingOnes] == '1')
            leadingOnes++;

        var value = BigInteger.Zero;
        foreach (var c in trimmed)
        {
            var index = Base58Alphabet.IndexOf(c);
            if (index < 0)
                throw new FormatException($"'{c}' is not a valid Base58 character.");
            value = value * 58 + index;
        }

        var magnitude = value.IsZero ? [] : value.ToByteArray(isUnsigned: true, isBigEndian: true);
        var result = new byte[leadingOnes + magnitude.Length];
        Array.Copy(magnitude, 0, result, leadingOnes, magnitude.Length);
        return result;
    }

    // ------------------------------------------------------------------------------------ Ascii85

    /// <summary>Standard (Adobe) Ascii85 with the 'z' shorthand for an all-zero 4-byte group.
    /// Does not add the "&lt;~ ~&gt;" wrapper — <see cref="Ascii85Decode"/> strips it if
    /// present, so either form round-trips.</summary>
    private static string Ascii85Encode(byte[] data)
    {
        var sb = new StringBuilder((data.Length + 3) / 4 * 5);
        var i = 0;
        while (i < data.Length)
        {
            var n = Math.Min(4, data.Length - i);
            uint value = 0;
            for (var j = 0; j < 4; j++)
            {
                value <<= 8;
                if (j < n)
                    value |= data[i + j];
            }

            if (n == 4 && value == 0)
            {
                sb.Append('z');
            }
            else
            {
                var chars = new char[5];
                for (var k = 4; k >= 0; k--)
                {
                    chars[k] = (char)(value % 85 + 33);
                    value /= 85;
                }
                sb.Append(chars, 0, n + 1);
            }
            i += n;
        }
        return sb.ToString();
    }

    /// <exception cref="FormatException">The input has an invalid group length, an out-of-range
    /// character, or a 5-character group whose value overflows 32 bits.</exception>
    private static byte[] Ascii85Decode(string s)
    {
        var trimmed = s.Trim();
        if (trimmed.StartsWith("<~", StringComparison.Ordinal))
            trimmed = trimmed[2..];
        if (trimmed.EndsWith("~>", StringComparison.Ordinal))
            trimmed = trimmed[..^2];
        trimmed = new string(trimmed.Where(c => !char.IsWhiteSpace(c)).ToArray());

        if (trimmed.Length == 0)
            return [];

        var bytes = new List<byte>(trimmed.Length * 4 / 5 + 4);
        var i = 0;
        while (i < trimmed.Length)
        {
            if (trimmed[i] == 'z')
            {
                bytes.AddRange(new byte[4]);
                i++;
                continue;
            }

            var n = Math.Min(5, trimmed.Length - i);
            if (n == 1)
                throw new FormatException("Ascii85 input has a truncated final group (a lone character can't decode).");

            ulong value = 0;
            for (var j = 0; j < 5; j++)
            {
                var c = j < n ? trimmed[i + j] : 'u'; // pad with the max digit (84 = 'u') to complete the group
                var digit = c - 33;
                if (digit < 0 || digit > 84)
                    throw new FormatException($"'{c}' is not a valid Ascii85 character.");
                value = value * 85 + (ulong)digit;
            }
            if (value > uint.MaxValue)
                throw new FormatException("Ascii85 group decodes to a value that overflows 32 bits.");

            var group = new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };
            bytes.AddRange(group.Take(n - 1));
            i += n;
        }
        return bytes.ToArray();
    }
}
