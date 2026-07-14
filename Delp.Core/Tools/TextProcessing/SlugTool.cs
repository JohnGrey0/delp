using System.Globalization;
using System.Text;

namespace Delp.Core.Tools.TextProcessing;

public sealed record SlugOptions(
    char Separator = '-',
    bool Lowercase = true,
    int? MaxLength = null,
    bool RemoveStopwords = false);

/// <summary>Turns arbitrary text into a URL-friendly slug.</summary>
public static class SlugTool
{
    // Characters that Unicode NFD decomposition does not split into base + combining mark,
    // so they need an explicit transliteration.
    private static readonly Dictionary<char, string> CharMap = new()
    {
        ['ß'] = "ss",
        ['æ'] = "ae",
        ['Æ'] = "AE",
        ['ø'] = "o",
        ['Ø'] = "O",
        ['đ'] = "d",
        ['Đ'] = "D",
        ['ð'] = "d",
        ['Ð'] = "D",
        ['þ'] = "th",
        ['Þ'] = "TH",
    };

    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "but", "of", "in", "on", "at", "to", "for",
        "with", "by", "from", "as", "is", "are", "was", "were", "be", "been",
        "being", "this", "that", "these", "those", "it", "its", "into", "than",
    };

    /// <exception cref="ArgumentException"><paramref name="options"/>.Separator is not
    /// '-' or '_'.</exception>
    public static string Make(string text, SlugOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);
        if (options.Separator != '-' && options.Separator != '_')
            throw new ArgumentException("Separator must be '-' or '_'.", nameof(options));

        var firstLine = FirstLine(text);

        var mapped = new StringBuilder(firstLine.Length);
        foreach (var c in firstLine)
            mapped.Append(CharMap.TryGetValue(c, out var repl) ? repl : c.ToString());

        var normalized = mapped.ToString().Normalize(NormalizationForm.FormD);
        var stripped = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            stripped.Append(c);
        }

        var working = stripped.ToString();
        if (options.Lowercase)
            working = working.ToLowerInvariant();

        var words = SplitWords(working);

        if (options.RemoveStopwords)
            words = words.Where(w => !Stopwords.Contains(w)).ToList();

        if (options.MaxLength is { } max && max > 0)
            words = TakeWithinLength(words, max);

        return string.Join(options.Separator, words);
    }

    private static string FirstLine(string text)
    {
        var idx = text.IndexOfAny(['\r', '\n']);
        return idx < 0 ? text : text[..idx];
    }

    /// <summary>Splits on anything that is not a Unicode letter or digit. Letters from any
    /// script (including CJK) are kept as-is — only Latin diacritics get transliterated.</summary>
    private static List<string> SplitWords(string text)
    {
        var words = new List<string>();
        var current = new StringBuilder();
        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c))
            {
                current.Append(c);
            }
            else if (current.Length > 0)
            {
                words.Add(current.ToString());
                current.Clear();
            }
        }
        if (current.Length > 0)
            words.Add(current.ToString());
        return words;
    }

    private static List<string> TakeWithinLength(List<string> words, int max)
    {
        var result = new List<string>();
        var length = 0;
        foreach (var w in words)
        {
            var addLength = (result.Count == 0 ? 0 : 1) + w.Length;
            if (length + addLength > max)
                break;
            result.Add(w);
            length += addLength;
        }
        return result;
    }
}
