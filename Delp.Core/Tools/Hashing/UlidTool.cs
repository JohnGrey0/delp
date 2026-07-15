using System.Security.Cryptography;

namespace Delp.Core.Tools.Hashing;

/// <summary>
/// ULID (Universally Unique Lexicographically Sortable Identifier) generation and decoding.
/// Spec: https://github.com/ulid/spec — a 128-bit value encoded as 26 Crockford Base32
/// characters: a 48-bit millisecond Unix timestamp (10 chars) followed by 80 bits of
/// randomness (16 chars). Crypto-quality randomness via <see cref="RandomNumberGenerator"/>.
/// </summary>
public static class UlidTool
{
    /// <summary>Crockford's Base32 alphabet: excludes I, L, O, U to avoid visual ambiguity.</summary>
    public const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public const int MaxBatch = 1000;

    /// <summary>Generates a single ULID for the current instant.</summary>
    public static string Generate() => Encode((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), RandomNumberGenerator.GetBytes(10));

    /// <summary>
    /// Generates <paramref name="count"/> ULIDs. Per the spec's "monotonic" extension: when two
    /// or more IDs land in the same millisecond, each subsequent one reuses that millisecond and
    /// increments the previous randomness by 1 (with carry) instead of drawing fresh random bits,
    /// so lexical order within the batch always matches generation order even at high throughput.
    /// On the astronomically rare full-randomness overflow, the millisecond is bumped by one
    /// instead so monotonicity never breaks.
    /// </summary>
    /// <exception cref="ArgumentException">Count is outside 1..1000.</exception>
    public static IReadOnlyList<string> GenerateBatch(int count)
    {
        if (count is < 1 or > MaxBatch)
            throw new ArgumentException($"Count must be between 1 and {MaxBatch}.", nameof(count));

        var results = new List<string>(count);
        ulong lastMs = 0;
        byte[]? lastRandom = null;

        for (var i = 0; i < count; i++)
        {
            var ms = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            byte[] randomness;

            if (lastRandom is not null && ms <= lastMs)
            {
                ms = lastMs;
                var incremented = TryIncrement(lastRandom);
                if (incremented is null)
                {
                    ms = lastMs + 1;
                    randomness = RandomNumberGenerator.GetBytes(10);
                }
                else
                {
                    randomness = incremented;
                }
            }
            else
            {
                randomness = RandomNumberGenerator.GetBytes(10);
            }

            lastMs = ms;
            lastRandom = randomness;
            results.Add(Encode(ms, randomness));
        }

        return results;
    }

    /// <summary>Decodes a ULID's embedded 48-bit timestamp and 80-bit randomness.</summary>
    /// <exception cref="FormatException">The text is not a well-formed 26-character Crockford Base32 ULID.</exception>
    public static (ulong TimestampMs, byte[] Randomness) Decode(string ulid)
    {
        ArgumentNullException.ThrowIfNull(ulid);
        var s = ulid.Trim();
        if (s.Length != 26)
            throw new FormatException("A ULID must be exactly 26 characters.");

        ulong ms = 0;
        foreach (var c in s[..10])
            ms = (ms << 5) | (uint)CharValue(c);

        var randomness = DecodeBits(s[10..], 10);
        return (ms, randomness);
    }

    /// <summary>True when <paramref name="text"/> is 26 characters, all drawn from the Crockford Base32 alphabet.</summary>
    public static bool IsWellFormed(string text)
    {
        var s = text.Trim();
        if (s.Length != 26)
            return false;
        foreach (var c in s)
        {
            if (CharValueOrDefault(c) < 0)
                return false;
        }
        return true;
    }

    private static string Encode(ulong ms, byte[] randomness)
    {
        Span<char> chars = stackalloc char[26];
        for (var i = 9; i >= 0; i--)
        {
            chars[i] = Alphabet[(int)(ms & 0x1F)];
            ms >>= 5;
        }
        EncodeBits(randomness, chars[10..]);
        return new string(chars);
    }

    /// <summary>Packs a byte string into 5-bit Crockford Base32 groups (bytes*8 must equal dest.Length*5).</summary>
    private static void EncodeBits(ReadOnlySpan<byte> data, Span<char> dest)
    {
        int bitBuffer = 0, bitCount = 0, byteIndex = 0, charIndex = 0;
        while (charIndex < dest.Length)
        {
            while (bitCount < 5)
            {
                bitBuffer = (bitBuffer << 8) | data[byteIndex++];
                bitCount += 8;
            }
            var shift = bitCount - 5;
            dest[charIndex++] = Alphabet[(bitBuffer >> shift) & 0x1F];
            bitCount -= 5;
            bitBuffer &= (1 << bitCount) - 1;
        }
    }

    /// <summary>Unpacks 5-bit Crockford Base32 characters back into <paramref name="byteLength"/> bytes.</summary>
    private static byte[] DecodeBits(string chars, int byteLength)
    {
        var bytes = new byte[byteLength];
        int bitBuffer = 0, bitCount = 0, charIndex = 0, outIndex = 0;
        while (outIndex < byteLength)
        {
            while (bitCount < 8)
            {
                bitBuffer = (bitBuffer << 5) | CharValue(chars[charIndex++]);
                bitCount += 5;
            }
            var shift = bitCount - 8;
            bytes[outIndex++] = (byte)((bitBuffer >> shift) & 0xFF);
            bitCount -= 8;
            bitBuffer &= (1 << bitCount) - 1;
        }
        return bytes;
    }

    /// <summary>Increments an 80-bit big-endian byte string by 1. Returns null on overflow (all bits were 1).</summary>
    private static byte[]? TryIncrement(byte[] value)
    {
        var copy = (byte[])value.Clone();
        for (var i = copy.Length - 1; i >= 0; i--)
        {
            if (copy[i] == 0xFF)
            {
                copy[i] = 0;
                continue;
            }
            copy[i]++;
            return copy;
        }
        return null;
    }

    private static int CharValue(char c)
    {
        var v = CharValueOrDefault(c);
        if (v < 0)
            throw new FormatException($"'{c}' is not a valid Crockford Base32 character.");
        return v;
    }

    private static int CharValueOrDefault(char c) => Alphabet.IndexOf(char.ToUpperInvariant(c));
}
