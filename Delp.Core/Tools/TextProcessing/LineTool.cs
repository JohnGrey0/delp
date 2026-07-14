using System.Globalization;
using System.Text.RegularExpressions;

namespace Delp.Core.Tools.TextProcessing;

public enum SortMode
{
    None,
    Asc,
    Desc,
    Natural,
    Length,
    Numeric,
}

/// <summary>Options for <see cref="LineTool.Process"/>. Pipeline order: trim -&gt; remove empty -&gt;
/// sort -&gt; dedupe -&gt; reverse -&gt; shuffle (shuffle always applies last and wins over sort/reverse).</summary>
public sealed record LineToolOptions(
    SortMode Mode = SortMode.None,
    bool CaseInsensitive = false,
    bool Dedupe = false,
    bool TrimLines = false,
    bool RemoveEmpty = false,
    bool Reverse = false,
    bool Shuffle = false,
    int? Seed = null);

public sealed record LineResult(string Text, int Before, int After);

/// <summary>Sorts, dedupes, and cleans line-oriented text.</summary>
public static class LineTool
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);
    private static readonly Regex NaturalChunk = new(@"\d+|\D+", RegexOptions.None, Timeout);
    private static readonly Regex LeadingNumber = new(@"^\s*[+-]?(?:\d+\.?\d*|\.\d+)", RegexOptions.None, Timeout);

    public static LineResult Process(string? text, LineToolOptions options)
    {
        text ??= "";
        var lines = SplitLines(text);
        var before = lines.Count;

        if (options.TrimLines)
            lines = lines.Select(l => l.Trim()).ToList();

        if (options.RemoveEmpty)
            lines = lines.Where(l => l.Length > 0).ToList();

        if (options.Mode != SortMode.None)
            lines = Sort(lines, options.Mode, options.CaseInsensitive);

        if (options.Dedupe)
            lines = Dedupe(lines, options.CaseInsensitive);

        if (options.Reverse)
        {
            lines = new List<string>(lines);
            lines.Reverse();
        }

        if (options.Shuffle)
            lines = Shuffle(lines, options.Seed);

        return new LineResult(string.Join("\n", lines), before, lines.Count);
    }

    private static List<string> SplitLines(string text)
    {
        if (text.Length == 0)
            return [];
        return text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n').ToList();
    }

    private static List<string> Sort(List<string> lines, SortMode mode, bool ci) => mode switch
    {
        SortMode.Asc => lines.OrderBy(l => l, ci ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).ToList(),
        SortMode.Desc => lines.OrderByDescending(l => l, ci ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).ToList(),
        SortMode.Natural => SortNatural(lines, ci),
        SortMode.Length => lines.OrderBy(l => l.Length).ToList(),
        SortMode.Numeric => SortNumeric(lines),
        _ => lines,
    };

    /// <summary>
    /// Natural sort: each line's digit/non-digit chunks are extracted once up front
    /// (rather than re-running the chunk regex inside the comparer on every pairwise
    /// comparison during the O(n log n) sort) and compared chunk-by-chunk.
    /// </summary>
    private static List<string> SortNatural(List<string> lines, bool ci)
    {
        var comparer = Comparer<string[]>.Create((a, b) => CompareNaturalChunks(a, b, ci));
        return lines
            // LINQ's OrderBy is a stable sort (matching Asc/Desc below); List<T>.Sort is not.
            .OrderBy(l => NaturalChunk.Matches(l).Select(m => m.Value).ToArray(), comparer)
            .ToList();
    }

    private static List<string> SortNumeric(List<string> lines)
    {
        var numeric = new List<(string Line, double Value)>();
        var nonNumeric = new List<string>();
        foreach (var line in lines)
        {
            var m = LeadingNumber.Match(line);
            if (m.Success && double.TryParse(m.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                numeric.Add((line, value));
            else
                nonNumeric.Add(line);
        }
        return numeric.OrderBy(x => x.Value).Select(x => x.Line).Concat(nonNumeric).ToList();
    }

    private static int CompareNaturalChunks(string[] chunksA, string[] chunksB, bool ci)
    {
        var n = Math.Min(chunksA.Length, chunksB.Length);
        for (var i = 0; i < n; i++)
        {
            var ca = chunksA[i];
            var cb = chunksB[i];
            var isDigitA = char.IsDigit(ca[0]);
            var isDigitB = char.IsDigit(cb[0]);
            int cmp;
            if (isDigitA && isDigitB)
            {
                var na = ca.TrimStart('0');
                if (na.Length == 0) na = "0";
                var nb = cb.TrimStart('0');
                if (nb.Length == 0) nb = "0";
                cmp = na.Length != nb.Length
                    ? na.Length.CompareTo(nb.Length)
                    : string.CompareOrdinal(na, nb);
            }
            else
            {
                cmp = ci
                    ? string.Compare(ca, cb, StringComparison.OrdinalIgnoreCase)
                    : string.CompareOrdinal(ca, cb);
            }
            if (cmp != 0)
                return cmp;
        }
        return chunksA.Length.CompareTo(chunksB.Length);
    }

    private static List<string> Dedupe(List<string> lines, bool ci)
    {
        var seen = new HashSet<string>(ci ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var line in lines)
            if (seen.Add(line))
                result.Add(line);
        return result;
    }

    private static List<string> Shuffle(List<string> lines, int? seed)
    {
        var rnd = seed.HasValue ? new Random(seed.Value) : new Random();
        var arr = new List<string>(lines);
        for (var i = arr.Count - 1; i > 0; i--)
        {
            var j = rnd.Next(i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
        return arr;
    }
}
