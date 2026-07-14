using System.Text.RegularExpressions;

namespace Delp.Core.Tools.TextProcessing;

/// <summary>
/// Zero-dependency text pipeline: tokenization, stopword removal, Porter
/// stemming, frequency counting and n-grams.
/// </summary>
public static class NlpTool
{
    public sealed record NlpOptions(
        bool Lowercase,
        bool RemoveStopwords,
        bool RemovePunctuation,
        bool RemoveNumbers,
        bool Stem,
        string? ExtraStopwords = null);

    public sealed record NlpResult(
        string ProcessedText,
        IReadOnlyList<string> Tokens,
        IReadOnlyList<(string Term, int Count)> Frequencies,
        int SentenceCount);

    private static readonly Regex WordPattern = new(
        @"[\p{L}\p{Nd}]+(?:'[\p{L}\p{Nd}]+)*",
        RegexOptions.None,
        TimeSpan.FromSeconds(2));

    private static readonly Regex SentenceTerminator = new(
        @"[.!?]+",
        RegexOptions.None,
        TimeSpan.FromSeconds(2));

    /// <summary>
    /// Runs the pipeline in order: lowercase, strip punctuation/numbers,
    /// remove stopwords, then stem. <see cref="NlpResult.ProcessedText"/>
    /// keeps the original line breaks; each line becomes the surviving
    /// tokens for that line joined by single spaces.
    /// </summary>
    public static NlpResult Process(string? text, NlpOptions options)
    {
        text ??= "";

        var sentenceCount = CountSentences(text);
        var extra = ParseExtraStopwords(options.ExtraStopwords);

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var processedLines = new List<string>(lines.Length);
        var allTokens = new List<string>();

        foreach (var line in lines)
        {
            var lineTokens = new List<string>();
            foreach (Match match in WordPattern.Matches(line))
            {
                var token = match.Value;

                if (options.Lowercase)
                    token = token.ToLowerInvariant();

                if (options.RemovePunctuation)
                    token = token.Replace("'", "");

                if (token.Length == 0)
                    continue;

                if (options.RemoveNumbers && IsAllDigits(token))
                    continue;

                if (options.RemoveStopwords)
                {
                    var key = token.ToLowerInvariant();
                    if (NlpStopwords.Words.Contains(key) || extra.Contains(key))
                        continue;
                }

                if (options.Stem)
                    token = PorterStemmer.Stem(token);

                lineTokens.Add(token);
            }

            processedLines.Add(string.Join(' ', lineTokens));
            allTokens.AddRange(lineTokens);
        }

        var processedText = string.Join('\n', processedLines);

        var frequencies = allTokens
            .GroupBy(t => t, StringComparer.Ordinal)
            .Select(g => (Term: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Term, StringComparer.Ordinal)
            .ToList();

        return new NlpResult(processedText, allTokens, frequencies, sentenceCount);
    }

    /// <summary>Top n-grams (n = 2 or 3) over an already-tokenized sequence, most frequent first.</summary>
    public static IReadOnlyList<(string Gram, int Count)> Ngrams(IReadOnlyList<string> tokens, int n)
    {
        if (n is not (2 or 3))
            throw new ArgumentException("n must be 2 or 3.", nameof(n));
        if (tokens.Count < n)
            return Array.Empty<(string, int)>();

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i <= tokens.Count - n; i++)
        {
            var gram = string.Join(' ', Enumerable.Range(i, n).Select(j => tokens[j]));
            counts[gram] = counts.TryGetValue(gram, out var c) ? c + 1 : 1;
        }

        return counts
            .Select(kv => (Gram: kv.Key, Count: kv.Value))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Gram, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsAllDigits(string token)
    {
        foreach (var c in token)
            if (!char.IsDigit(c))
                return false;
        return true;
    }

    private static HashSet<string> ParseExtraStopwords(string? extra)
    {
        if (string.IsNullOrWhiteSpace(extra))
            return new HashSet<string>(StringComparer.Ordinal);

        return extra
            .Split([',', ' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(w => w.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
    }

    private static int CountSentences(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var matches = SentenceTerminator.Matches(text);
        var count = matches.Count;

        var lastEnd = matches.Count > 0 ? matches[^1].Index + matches[^1].Length : 0;
        var tail = text[lastEnd..];
        foreach (var c in tail)
        {
            if (char.IsLetterOrDigit(c))
            {
                count++;
                break;
            }
        }

        return count;
    }
}
