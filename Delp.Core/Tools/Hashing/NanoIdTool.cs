using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Delp.Core.Tools.Hashing;

/// <summary>Nano ID generation: crypto RNG, unbiased via rejection sampling (the standard nanoid mask technique).</summary>
public static class NanoIdTool
{
    public const string DefaultAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-";
    public const int DefaultSize = 21;

    /// <exception cref="ArgumentException">Size is not positive, or the alphabet is empty or exceeds 256 characters.</exception>
    public static string Generate(int size = DefaultSize, string? alphabet = null)
    {
        alphabet ??= DefaultAlphabet;
        if (size <= 0)
            throw new ArgumentException("Size must be a positive integer.", nameof(size));
        if (alphabet.Length == 0)
            throw new ArgumentException("Alphabet must not be empty.", nameof(alphabet));
        if (alphabet.Length > 256)
            throw new ArgumentException("Alphabet must be at most 256 characters.", nameof(alphabet));
        if (alphabet.Length == 1)
            return new string(alphabet[0], size);

        var mask = Mask(alphabet.Length);
        var step = (int)Math.Ceiling(1.6 * mask * size / alphabet.Length);
        var id = new StringBuilder(size);
        var buffer = new byte[step];

        while (true)
        {
            RandomNumberGenerator.Fill(buffer);
            for (var i = 0; i < step; i++)
            {
                var index = buffer[i] & mask;
                if (index >= alphabet.Length)
                    continue;
                id.Append(alphabet[index]);
                if (id.Length == size)
                    return id.ToString();
            }
        }
    }

    /// <summary>Smallest (2^n - 1) bitmask that covers the alphabet's index range, per the nanoid rejection-sampling algorithm.</summary>
    private static int Mask(int alphabetLength)
    {
        var bits = 32 - BitOperations.LeadingZeroCount((uint)(alphabetLength - 1));
        return (1 << bits) - 1;
    }

    /// <summary>
    /// Canned "years until a 1% collision probability" estimate at the given generation rate, using the
    /// standard birthday-bound approximation (as nanoid's own docs do): the id count at which collision
    /// probability p is reached is roughly sqrt(2 * total * p) for small p.
    /// </summary>
    public static double YearsFor1PercentCollision(int size, int alphabetLength, double idsPerHour)
    {
        if (alphabetLength <= 1 || size <= 0 || idsPerHour <= 0)
            return 0;

        var totalIds = Math.Pow(alphabetLength, size);
        var idsFor1Percent = Math.Sqrt(2 * totalIds * 0.01);
        var hours = idsFor1Percent / idsPerHour;
        return hours / (24 * 365.25);
    }
}
