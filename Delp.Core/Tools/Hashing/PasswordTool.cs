using System.Globalization;
using System.Security.Cryptography;

namespace Delp.Core.Tools.Hashing;

public enum PasswordStrength
{
    Weak,
    Fair,
    Strong,
    Excellent,
}

public sealed record PasswordOptions(
    int Length,
    bool Lower,
    bool Upper,
    bool Digits,
    bool Symbols,
    bool ExcludeAmbiguous,
    bool RequireEachClass);

public sealed record PassphraseOptions(
    int Words,
    char Separator,
    bool Capitalize,
    bool AppendNumber);

/// <summary>Random password and Diceware-style passphrase generation with entropy scoring.</summary>
public static class PasswordTool
{
    private const string LowerChars = "abcdefghijklmnopqrstuvwxyz";
    private const string UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string DigitChars = "0123456789";
    private const string SymbolChars = "!@#$%^&*()-_=+[]{};:,.<>?/";
    private const string AmbiguousChars = "Il1O0o";
    private const int MaxAttempts = 1000;

    public static string Generate(PasswordOptions options)
    {
        if (options.Length <= 0)
            throw new ArgumentException("Length must be at least 1.", nameof(options));

        var classes = BuildClasses(options);
        if (classes.Count == 0)
            throw new ArgumentException("Select at least one character class.", nameof(options));

        var alphabet = string.Concat(classes);
        if (options.RequireEachClass && options.Length < classes.Count)
            throw new ArgumentException(
                $"Length must be at least {classes.Count} to include every selected character class.", nameof(options));

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var chars = new char[options.Length];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];

            if (!options.RequireEachClass || SatisfiesAllClasses(chars, classes))
                return new string(chars);
        }

        throw new ArgumentException(
            "Could not generate a password satisfying every selected character class after 1000 attempts; increase the length.",
            nameof(options));
    }

    public static string GeneratePassphrase(PassphraseOptions options)
    {
        if (options.Words <= 0)
            throw new ArgumentException("Word count must be at least 1.", nameof(options));

        var parts = new List<string>(options.Words + 1);
        for (var i = 0; i < options.Words; i++)
        {
            var word = PassphraseWords.List[RandomNumberGenerator.GetInt32(PassphraseWords.List.Count)];
            parts.Add(options.Capitalize ? Capitalize(word) : word);
        }

        if (options.AppendNumber)
            parts.Add(RandomNumberGenerator.GetInt32(100).ToString("D2", CultureInfo.InvariantCulture));

        return string.Join(options.Separator, parts);
    }

    public static double EntropyBits(PasswordOptions options)
    {
        if (options.Length <= 0)
            return 0;
        var classes = BuildClasses(options);
        var alphabetSize = classes.Sum(c => c.Length);
        return alphabetSize == 0 ? 0 : options.Length * Math.Log2(alphabetSize);
    }

    public static double EntropyBits(PassphraseOptions options)
    {
        if (options.Words <= 0)
            return 0;
        var bits = options.Words * Math.Log2(PassphraseWords.List.Count);
        if (options.AppendNumber)
            bits += Math.Log2(100);
        return bits;
    }

    public static PasswordStrength StrengthLabel(double entropyBits) => entropyBits switch
    {
        < 50 => PasswordStrength.Weak,
        < 70 => PasswordStrength.Fair,
        < 90 => PasswordStrength.Strong,
        _ => PasswordStrength.Excellent,
    };

    private static List<string> BuildClasses(PasswordOptions options)
    {
        var classes = new List<string>();
        if (options.Lower)
            classes.Add(Strip(LowerChars, options.ExcludeAmbiguous));
        if (options.Upper)
            classes.Add(Strip(UpperChars, options.ExcludeAmbiguous));
        if (options.Digits)
            classes.Add(Strip(DigitChars, options.ExcludeAmbiguous));
        if (options.Symbols)
            classes.Add(Strip(SymbolChars, options.ExcludeAmbiguous));
        return classes.Where(c => c.Length > 0).ToList();
    }

    private static string Strip(string source, bool excludeAmbiguous) =>
        excludeAmbiguous ? new string(source.Where(c => !AmbiguousChars.Contains(c)).ToArray()) : source;

    private static bool SatisfiesAllClasses(char[] chars, List<string> classes) =>
        classes.All(cls => chars.Any(cls.Contains));

    private static string Capitalize(string word) =>
        word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..];
}
