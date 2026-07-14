using System.Text.RegularExpressions;

namespace Delp.Core.Tools.TextProcessing;

/// <summary>One entry of the "top words" ranking.</summary>
public sealed record WordCount(string Word, int Count);

/// <summary>Full statistical breakdown of a piece of text.</summary>
public sealed record TextStats(
    int Chars,
    int CharsNoSpaces,
    int Words,
    int UniqueWords,
    int Lines,
    int NonEmptyLines,
    int Sentences,
    int Paragraphs,
    long Utf8Bytes,
    double AvgWordLength,
    double ReadingTimeSeconds,
    IReadOnlyList<WordCount> TopWords);

/// <summary>Computes word/char/sentence/paragraph statistics and a stopword-filtered top-words list.</summary>
public static class TextStatsTool
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);
    private const double WordsPerMinute = 200.0;
    private const int DefaultTopWordsCount = 10;

    private static readonly Regex WordRegex =
        new(@"[\p{L}\p{N}]+(?:['’][\p{L}\p{N}]+)*", RegexOptions.None, Timeout);

    private static readonly Regex SentenceEnd =
        new(@"[.!?]+(?=\s+|$)", RegexOptions.None, Timeout);

    private static readonly Regex TrailingWord =
        new(@"(\S+)$", RegexOptions.None, Timeout);

    private static readonly Regex ParagraphSplit =
        new(@"(?:\r?\n)[ \t]*(?:\r?\n)+", RegexOptions.None, Timeout);

    // Common abbreviations whose trailing '.' must not be treated as a sentence end.
    private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "e.g", "i.e", "etc", "mr", "mrs", "ms", "dr", "prof", "sr", "jr",
        "st", "vs", "approx", "no", "fig", "vol", "op", "cf", "al", "gen",
    };

    // ~40 common English stopwords, excluded from the top-words ranking.
    private static readonly HashSet<string> Stopwords = new(StringComparer.Ordinal)
    {
        "a", "an", "the", "and", "or", "but", "of", "to", "in", "on", "at",
        "for", "with", "is", "are", "was", "were", "be", "been", "being",
        "this", "that", "these", "those", "it", "its", "as", "by", "from",
        "not", "no", "so", "if", "than", "then", "there", "here",
        "i", "you", "he", "she", "we", "they", "his", "her", "their", "our", "your", "my",
    };

    public static TextStats Analyze(string? text, int topWordsCount = DefaultTopWordsCount)
    {
        text ??= "";

        var wordMatches = WordRegex.Matches(text);
        var words = wordMatches.Count;
        var uniqueWords = wordMatches
            .Select(m => m.Value.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .Count();

        var lines = SplitLines(text);
        var nonEmptyLines = lines.Count(l => l.Trim().Length > 0);

        var avgWordLength = words == 0 ? 0 : wordMatches.Average(m => (double)m.Value.Length);
        var readingTimeSeconds = words / WordsPerMinute * 60.0;

        return new TextStats(
            Chars: text.Length,
            CharsNoSpaces: text.Count(c => !char.IsWhiteSpace(c)),
            Words: words,
            UniqueWords: uniqueWords,
            Lines: lines.Count,
            NonEmptyLines: nonEmptyLines,
            Sentences: CountSentences(text),
            Paragraphs: CountParagraphs(text),
            Utf8Bytes: System.Text.Encoding.UTF8.GetByteCount(text),
            AvgWordLength: avgWordLength,
            ReadingTimeSeconds: readingTimeSeconds,
            TopWords: TopWords(text, topWordsCount));
    }

    public static int CountSentences(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var count = 0;
        var lastEnd = 0;
        foreach (Match m in SentenceEnd.Matches(text))
        {
            var segment = text[..m.Index];
            var wm = TrailingWord.Match(segment);
            var lastWord = wm.Success ? wm.Value.Trim('.', ',', ';', ':', '"', '\'', '(', ')') : "";
            if (Abbreviations.Contains(lastWord))
                continue; // not a real sentence boundary, keep accumulating

            count++;
            lastEnd = m.Index + m.Length;
        }

        if (lastEnd < text.Length && text[lastEnd..].Trim().Length > 0)
            count++; // trailing clause with no terminal punctuation still counts

        return count;
    }

    public static int CountParagraphs(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return ParagraphSplit.Split(text).Count(p => p.Trim().Length > 0);
    }

    /// <summary>Top <paramref name="n"/> non-stopword words by frequency, ties broken by first appearance.</summary>
    public static IReadOnlyList<WordCount> TopWords(string? text, int n)
    {
        if (string.IsNullOrWhiteSpace(text) || n <= 0)
            return [];

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var order = new List<string>();
        foreach (Match m in WordRegex.Matches(text))
        {
            var w = m.Value.ToLowerInvariant();
            if (Stopwords.Contains(w))
                continue;
            if (counts.TryGetValue(w, out var c))
                counts[w] = c + 1;
            else
            {
                counts[w] = 1;
                order.Add(w);
            }
        }

        return order
            .Select(w => new WordCount(w, counts[w]))
            .OrderByDescending(wc => wc.Count)
            .Take(n)
            .ToList();
    }

    private static List<string> SplitLines(string text)
    {
        if (text.Length == 0)
            return [];
        return text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n').ToList();
    }
}
