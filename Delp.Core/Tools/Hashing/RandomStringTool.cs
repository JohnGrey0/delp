using System.Security.Cryptography;
using System.Text;

namespace Delp.Core.Tools.Hashing;

public sealed record RandomStringOptions(
    int Length,
    bool Lower,
    bool Upper,
    bool Digits,
    bool Symbols,
    string? Custom = null,
    bool ExcludeAmbiguous = false);

/// <summary>Cryptographically secure random string generation with a configurable character set.</summary>
public static class RandomStringTool
{
    private const string LowerAlphabet = "abcdefghijklmnopqrstuvwxyz";
    private const string UpperAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string DigitAlphabet = "0123456789";
    private const string SymbolAlphabet = "!@#$%^&*()-_=+[]{}<>?/~|";

    /// <summary>Characters that are easy to confuse visually: capital I, lowercase l, digit 1, capital O, digit 0, lowercase o.</summary>
    private const string AmbiguousChars = "Il1O0o";

    /// <summary>Builds the effective, de-duplicated alphabet for the given options.</summary>
    public static string BuildAlphabet(RandomStringOptions options)
    {
        var sb = new StringBuilder();
        if (options.Lower) sb.Append(LowerAlphabet);
        if (options.Upper) sb.Append(UpperAlphabet);
        if (options.Digits) sb.Append(DigitAlphabet);
        if (options.Symbols) sb.Append(SymbolAlphabet);
        if (!string.IsNullOrEmpty(options.Custom)) sb.Append(options.Custom);

        IEnumerable<char> chars = sb.ToString();
        if (options.ExcludeAmbiguous)
            chars = chars.Where(c => !AmbiguousChars.Contains(c));

        return new string(chars.Distinct().ToArray());
    }

    /// <exception cref="ArgumentException">Length is not positive, or the resulting alphabet is empty.</exception>
    public static string Generate(RandomStringOptions options)
    {
        if (options.Length <= 0)
            throw new ArgumentException("Length must be a positive integer.", nameof(options));

        var alphabet = BuildAlphabet(options);
        if (alphabet.Length == 0)
            throw new ArgumentException("Select at least one character set, or provide a custom alphabet.", nameof(options));

        var result = new char[options.Length];
        for (var i = 0; i < result.Length; i++)
            result[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        return new string(result);
    }

    /// <summary>Shannon entropy of a string drawn uniformly from the effective alphabet: length x log2(alphabet size).</summary>
    public static double EntropyBits(RandomStringOptions options)
    {
        var alphabetSize = BuildAlphabet(options).Length;
        return alphabetSize <= 1 || options.Length <= 0 ? 0 : options.Length * Math.Log2(alphabetSize);
    }
}
