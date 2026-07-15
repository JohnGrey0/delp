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

/// <summary>Off = no filtering. Keep = only lines matching <see cref="LineToolOptions.FilterPattern"/>
/// survive. Remove = matching lines are dropped.</summary>
public enum LineFilterMode
{
    Off,
    Keep,
    Remove,
}

/// <summary>Options for <see cref="LineTool.Process"/>. Pipeline order: trim -&gt; remove empty -&gt;
/// filter -&gt; dedupe -&gt; sort -&gt; reverse -&gt; shuffle (shuffle always applies last and wins over
/// sort/reverse) -&gt; number. All filter/number fields default to off so existing callers are
/// unaffected.</summary>
public sealed record LineToolOptions(
    SortMode Mode = SortMode.None,
    bool CaseInsensitive = false,
    bool Dedupe = false,
    bool TrimLines = false,
    bool RemoveEmpty = false,
    bool Reverse = false,
    bool Shuffle = false,
    int? Seed = null,
    LineFilterMode Filter = LineFilterMode.Off,
    string? FilterPattern = null,
    bool FilterRegex = false,
    bool NumberLines = false,
    int NumberStart = 1,
    int NumberStep = 1,
    int NumberPad = 0);

/// <summary><see cref="FilteredKept"/>/<see cref="FilteredTotal"/> are null unless filtering ran;
/// when present they describe how many of the pre-filter lines survived, for a "kept N of M" status.</summary>
public sealed record LineResult(string Text, int Before, int After, int? FilteredKept = null, int? FilteredTotal = null);

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

        int? filteredKept = null;
        int? filteredTotal = null;
        if (options.Filter != LineFilterMode.Off && !string.IsNullOrEmpty(options.FilterPattern))
        {
            filteredTotal = lines.Count;
            lines = Filter(lines, options.Filter, options.FilterPattern, options.FilterRegex, options.CaseInsensitive);
            filteredKept = lines.Count;
        }

        if (options.Dedupe)
            lines = Dedupe(lines, options.CaseInsensitive);

        if (options.Mode != SortMode.None)
            lines = Sort(lines, options.Mode, options.CaseInsensitive);

        if (options.Reverse)
        {
            lines = new List<string>(lines);
            lines.Reverse();
        }

        if (options.Shuffle)
            lines = Shuffle(lines, options.Seed);

        if (options.NumberLines)
            lines = Number(lines, options.NumberStart, options.NumberStep, options.NumberPad);

        return new LineResult(string.Join("\n", lines), before, lines.Count, filteredKept, filteredTotal);
    }

    /// <summary>Keeps or drops lines matching <paramref name="pattern"/>. Plain mode does a
    /// substring search; regex mode compiles the pattern with a 2 s match timeout (the shared
    /// <see cref="Timeout"/>) and surfaces bad patterns / runaway matches as a <see cref="FormatException"/>.</summary>
    private static List<string> Filter(List<string> lines, LineFilterMode mode, string pattern, bool useRegex, bool ci)
    {
        Func<string, bool> isMatch;
        if (useRegex)
        {
            Regex compiled;
            try
            {
                compiled = new Regex(pattern, ci ? RegexOptions.IgnoreCase : RegexOptions.None, Timeout);
            }
            catch (ArgumentException ex)
            {
                throw new FormatException($"Invalid filter pattern — {ex.Message}");
            }

            isMatch = line =>
            {
                try
                {
                    return compiled.IsMatch(line);
                }
                catch (RegexMatchTimeoutException)
                {
                    throw new FormatException("Filter pattern timed out (2s) — it may be catastrophically backtracking.");
                }
            };
        }
        else
        {
            var comparison = ci ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            isMatch = line => line.Contains(pattern, comparison);
        }

        var keepOnMatch = mode == LineFilterMode.Keep;
        return lines.Where(l => isMatch(l) == keepOnMatch).ToList();
    }

    /// <summary>Prefixes each line with a "start, start+step, …" counter (optionally zero-padded
    /// to <paramref name="pad"/> digits), joined by ". " as the last pipeline step.</summary>
    private static List<string> Number(List<string> lines, int start, int step, int pad)
    {
        var result = new List<string>(lines.Count);
        var n = start;
        foreach (var line in lines)
        {
            var label = n.ToString(CultureInfo.InvariantCulture);
            if (pad > 0 && label.Length < pad)
                label = label.PadLeft(pad, '0');
            result.Add($"{label}. {line}");
            n += step;
        }
        return result;
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
