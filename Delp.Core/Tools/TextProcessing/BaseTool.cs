using System.Numerics;
using System.Text;

namespace Delp.Core.Tools.TextProcessing;

/// <summary>Arbitrary-precision integer base conversion (radix 2-36), hand-rolled since
/// <see cref="BigInteger"/> has no base-N formatter.</summary>
public static class BaseTool
{
    private const string LowerAlphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
    private const string UpperAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>Parses an integer literal. Recognizes <c>0x</c>/<c>0b</c>/<c>0o</c>
    /// prefixes (case-insensitive, overriding <paramref name="baseHint"/>); otherwise uses
    /// <paramref name="baseHint"/> (default 10). Underscore and space digit separators and
    /// an optional leading sign are tolerated.</summary>
    /// <exception cref="FormatException">The input is empty or contains a digit invalid
    /// for the resolved radix.</exception>
    public static BigInteger Parse(string input, int? baseHint = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        var s = input.Trim();
        if (s.Length == 0)
            throw new FormatException("Input is empty.");

        var negative = false;
        var i = 0;
        if (s[0] is '+' or '-')
        {
            negative = s[0] == '-';
            i = 1;
        }

        var rest = s[i..].Replace("_", "").Replace(" ", "");
        if (rest.Length == 0)
            throw new FormatException("Input has no digits.");

        int radix;
        string digits;
        if (rest.Length >= 2 && rest[0] == '0' && (rest[1] == 'x' || rest[1] == 'X'))
        {
            radix = 16;
            digits = rest[2..];
        }
        else if (rest.Length >= 2 && rest[0] == '0' && (rest[1] == 'b' || rest[1] == 'B'))
        {
            radix = 2;
            digits = rest[2..];
        }
        else if (rest.Length >= 2 && rest[0] == '0' && (rest[1] == 'o' || rest[1] == 'O'))
        {
            radix = 8;
            digits = rest[2..];
        }
        else
        {
            radix = baseHint ?? 10;
            digits = rest;
        }

        if (radix is < 2 or > 36)
            throw new FormatException($"Radix {radix} is out of the supported range 2-36.");
        if (digits.Length == 0)
            throw new FormatException("Input has no digits after the base prefix.");

        var value = BigInteger.Zero;
        for (var pos = 0; pos < digits.Length; pos++)
        {
            var d = DigitValue(digits[pos]);
            if (d < 0 || d >= radix)
                throw new FormatException($"Invalid digit '{digits[pos]}' for base {radix} at position {pos}.");
            value = value * radix + d;
        }

        return negative ? -value : value;
    }

    /// <summary>Formats <paramref name="value"/> in the given radix (2-36). Set
    /// <paramref name="groupSize"/> &gt; 0 to insert <c>_</c> separators every N digits,
    /// counted from the least-significant digit.</summary>
    public static string ToBase(BigInteger value, int radix, bool uppercase = false, int groupSize = 0)
    {
        if (radix is < 2 or > 36)
            throw new ArgumentOutOfRangeException(nameof(radix), radix, "Radix must be between 2 and 36.");

        var negative = value.Sign < 0;
        var v = BigInteger.Abs(value);
        var alphabet = uppercase ? UpperAlphabet : LowerAlphabet;

        string digits;
        if (v.IsZero)
        {
            digits = "0";
        }
        else
        {
            var chars = new List<char>();
            while (v > 0)
            {
                chars.Add(alphabet[(int)(v % radix)]);
                v /= radix;
            }
            chars.Reverse();
            digits = new string(chars.ToArray());
        }

        var grouped = groupSize > 0 ? Group(digits, groupSize) : digits;
        return negative ? "-" + grouped : grouped;
    }

    /// <summary>Bit length and minimum byte count needed to represent the magnitude of
    /// <paramref name="value"/> (sign excluded).</summary>
    public static (int BitLength, int ByteCount) Measure(BigInteger value)
    {
        var abs = BigInteger.Abs(value);
        var bits = abs.IsZero ? 0 : (int)abs.GetBitLength();
        var bytes = (bits + 7) / 8;
        return (bits, bytes);
    }

    private static int DigitValue(char c)
    {
        if (c is >= '0' and <= '9') return c - '0';
        if (c is >= 'a' and <= 'z') return c - 'a' + 10;
        if (c is >= 'A' and <= 'Z') return c - 'A' + 10;
        return -1;
    }

    private static string Group(string digits, int groupSize)
    {
        var firstGroupLen = digits.Length % groupSize;
        var sb = new StringBuilder(digits.Length + digits.Length / groupSize);
        var idx = 0;
        if (firstGroupLen > 0)
        {
            sb.Append(digits, 0, firstGroupLen);
            idx = firstGroupLen;
        }
        for (; idx < digits.Length; idx += groupSize)
        {
            if (sb.Length > 0)
                sb.Append('_');
            sb.Append(digits, idx, groupSize);
        }
        return sb.ToString();
    }
}
