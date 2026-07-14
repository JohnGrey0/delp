using System.Text;
using System.Text.RegularExpressions;

namespace Delp.Core.Tools.Encoding;

/// <summary>ITU International Morse Code translator.</summary>
public static class MorseTool
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);
    private static readonly Regex WhitespaceSplitRegex = new(@"\s+", RegexOptions.None, RegexTimeout);
    private static readonly Regex WordSeparatorRegex = new(@"\s*[/|]\s*|\s{3,}", RegexOptions.None, RegexTimeout);

    private static readonly IReadOnlyDictionary<char, string> CharToMorse = new Dictionary<char, string>
    {
        ['A'] = ".-", ['B'] = "-...", ['C'] = "-.-.", ['D'] = "-..", ['E'] = ".", ['F'] = "..-.",
        ['G'] = "--.", ['H'] = "....", ['I'] = "..", ['J'] = ".---", ['K'] = "-.-", ['L'] = ".-..",
        ['M'] = "--", ['N'] = "-.", ['O'] = "---", ['P'] = ".--.", ['Q'] = "--.-", ['R'] = ".-.",
        ['S'] = "...", ['T'] = "-", ['U'] = "..-", ['V'] = "...-", ['W'] = ".--", ['X'] = "-..-",
        ['Y'] = "-.--", ['Z'] = "--..",
        ['0'] = "-----", ['1'] = ".----", ['2'] = "..---", ['3'] = "...--", ['4'] = "....-",
        ['5'] = ".....", ['6'] = "-....", ['7'] = "--...", ['8'] = "---..", ['9'] = "----.",
        ['.'] = ".-.-.-", [','] = "--..--", ['?'] = "..--..", ['\''] = ".----.", ['!'] = "-.-.--",
        ['/'] = "-..-.", ['('] = "-.--.", [')'] = "-.--.-", ['&'] = ".-...", [':'] = "---...",
        [';'] = "-.-.-.", ['='] = "-...-", ['+'] = ".-.-.", ['-'] = "-....-", ['_'] = "..--.-",
        ['\"'] = ".-..-.", ['$'] = "...-..-", ['@'] = ".--.-.",
    };

    private static readonly IReadOnlyDictionary<string, char> MorseToChar =
        CharToMorse.ToDictionary(kv => kv.Value, kv => kv.Key);

    /// <summary>Encodes text to Morse: letters within a word are separated by a single space, words by " / ".</summary>
    /// <exception cref="FormatException">A character has no Morse mapping and <paramref name="skipUnknown"/> is false.</exception>
    public static string Encode(string text, bool skipUnknown)
    {
        var trimmed = (text ?? "").Trim();
        if (trimmed.Length == 0)
            return "";

        var words = WhitespaceSplitRegex.Split(trimmed);
        var encodedWords = new List<string>();
        foreach (var word in words)
        {
            var groups = new List<string>();
            foreach (var ch in word)
            {
                var upper = char.ToUpperInvariant(ch);
                if (CharToMorse.TryGetValue(upper, out var code))
                    groups.Add(code);
                else if (!skipUnknown)
                    throw new FormatException($"Unsupported character '{ch}' has no Morse mapping.");
            }

            if (groups.Count > 0)
                encodedWords.Add(string.Join(" ", groups));
        }

        return string.Join(" / ", encodedWords);
    }

    /// <summary>Decodes Morse to uppercase text. Word separators may be "/", "|", or 3+ spaces.</summary>
    /// <exception cref="FormatException">An unrecognized Morse group is found.</exception>
    public static string Decode(string morse)
    {
        var trimmed = (morse ?? "").Trim();
        if (trimmed.Length == 0)
            return "";

        var words = WordSeparatorRegex.Split(trimmed);
        var decodedWords = new List<string>();
        foreach (var word in words)
        {
            var groups = WhitespaceSplitRegex.Split(word.Trim());
            var sb = new StringBuilder();
            foreach (var group in groups)
            {
                if (group.Length == 0)
                    continue;
                if (!MorseToChar.TryGetValue(group, out var ch))
                    throw new FormatException($"Unknown Morse group '{group}'.");
                sb.Append(ch);
            }

            if (sb.Length > 0)
                decodedWords.Add(sb.ToString());
        }

        return string.Join(" ", decodedWords);
    }
}
