using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Delp.Core.Tools.TextProcessing;

public enum SplitMode
{
    Words,
    Lines,
    Delimiter,
}

public enum ListFormat
{
    PythonList,
    JsonArray,
    JsArray,
    CSharpArray,
    CsvLine,
    CsvColumn,
    SqlIn,
    PlainLines,
    SpaceJoined,
}

/// <summary>Quote style for formats that support a choice (PythonList, JsArray). Formats that
/// mandate a specific quoting convention (JsonArray, CSharpArray, CsvLine/Column, SqlIn,
/// PlainLines, SpaceJoined) ignore this option entirely.</summary>
public enum QuoteChar
{
    Double,
    Single,
    None,
}

public sealed record TextListOptions(bool Trim, bool RemoveEmpty, bool Dedupe, bool Lowercase, bool StripPunctuation);

/// <summary>Splits free text into a list of items (by words, lines, or a delimiter) and renders
/// that list back out in a dozen common source/data formats, each with correct escaping.</summary>
public static class TextListTool
{
    // Unicode word: a run of letters/digits, optionally continuing across a single internal
    // apostrophe or hyphen (so "It's" and "well-known" stay one token, but leading/trailing
    // punctuation never gets pulled in).
    private static readonly Regex WordPattern =
        new(@"[\p{L}\p{Nd}]+(?:['\-][\p{L}\p{Nd}]+)*", RegexOptions.None, TimeSpan.FromSeconds(2));

    private static readonly JsonSerializerOptions JsonOptions =
        new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static List<string> Split(string text, SplitMode mode, string? delimiter, TextListOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        IEnumerable<string> items = mode switch
        {
            SplitMode.Words => SplitWords(text),
            SplitMode.Lines => SplitLines(text),
            SplitMode.Delimiter => SplitDelimiter(text, delimiter),
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };

        if (options.Trim)
            items = items.Select(s => s.Trim());

        // Word tokens never carry leading/trailing punctuation to begin with (the word pattern
        // already excludes it), so this only meaningfully applies to Lines/Delimiter items.
        if (options.StripPunctuation && mode != SplitMode.Words)
            items = items.Select(StripPunctuationEdges);

        if (options.RemoveEmpty)
            items = items.Where(s => s.Length > 0);

        if (options.Lowercase)
            items = items.Select(s => s.ToLowerInvariant());

        var list = items.ToList();
        if (!options.Dedupe)
            return list;

        // Applying Lowercase before this step means dedupe is naturally case-insensitive when
        // Lowercase is on (everything's already normalized) and ordinal otherwise.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var deduped = new List<string>(list.Count);
        foreach (var item in list)
            if (seen.Add(item))
                deduped.Add(item);
        return deduped;
    }

    public static string Format(IReadOnlyList<string> items, ListFormat format, QuoteChar quote)
    {
        ArgumentNullException.ThrowIfNull(items);

        return format switch
        {
            ListFormat.PythonList => Bracketed(items, "[", "]", ResolveQuote(quote)),
            ListFormat.JsArray => Bracketed(items, "[", "]", ResolveQuote(quote)),
            ListFormat.JsonArray => JsonSerializer.Serialize(items, JsonOptions),
            ListFormat.CSharpArray => FormatCSharpArray(items),
            ListFormat.CsvLine => string.Join(",", items.Select(CsvField)),
            ListFormat.CsvColumn => string.Join("\n", items.Select(CsvField)),
            ListFormat.SqlIn => FormatSqlIn(items),
            ListFormat.PlainLines => string.Join("\n", items),
            ListFormat.SpaceJoined => string.Join(" ", items),
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
    }

    // ---------------------------------------------------------------- splitting

    private static IEnumerable<string> SplitWords(string text) =>
        WordPattern.Matches(text).Select(m => m.Value);

    private static IEnumerable<string> SplitLines(string text) =>
        text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

    private static IEnumerable<string> SplitDelimiter(string text, string? delimiter)
    {
        var d = string.IsNullOrEmpty(delimiter) ? "," : delimiter;
        return text.Split([d], StringSplitOptions.None);
    }

    private static string StripPunctuationEdges(string s)
    {
        var start = 0;
        var end = s.Length;
        while (start < end && char.IsPunctuation(s[start]))
            start++;
        while (end > start && char.IsPunctuation(s[end - 1]))
            end--;
        return s[start..end];
    }

    // ---------------------------------------------------------------- formatting

    private static char? ResolveQuote(QuoteChar quote) => quote switch
    {
        QuoteChar.Double => '"',
        QuoteChar.Single => '\'',
        QuoteChar.None => null,
        _ => throw new ArgumentOutOfRangeException(nameof(quote)),
    };

    private static string Bracketed(IReadOnlyList<string> items, string open, string close, char? quote) =>
        open + string.Join(", ", items.Select(i => QuoteItem(i, quote))) + close;

    /// <summary>Wraps <paramref name="s"/> in <paramref name="quote"/> (or leaves it bare when
    /// null), backslash-escaping any backslash, any occurrence of the active quote, and any raw
    /// line-terminator/tab character. The latter matters: an unescaped newline or CR embedded
    /// directly in a Python/JS/C# single-line string literal is a syntax error in that target
    /// language, not just a cosmetic issue — so control characters must become `\n`/`\r`/`\t`
    /// escapes rather than passing through verbatim.</summary>
    private static string QuoteItem(string s, char? quote)
    {
        if (quote is null)
            return s;

        var q = quote.Value;
        var sb = new StringBuilder(s.Length + 2);
        sb.Append(q);
        foreach (var c in s)
        {
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (c == q)
                        sb.Append('\\').Append(c);
                    else
                        sb.Append(c);
                    break;
            }
        }
        sb.Append(q);
        return sb.ToString();
    }

    // `new[] { }` has no elements to infer an element type from, so it is not valid C#
    // (CS0826) — the empty case needs an explicitly-typed empty array instead.
    private static string FormatCSharpArray(IReadOnlyList<string> items) =>
        items.Count == 0
            ? "Array.Empty<string>()"
            : "new[] { " + string.Join(", ", items.Select(i => QuoteItem(i, '"'))) + " }";

    private static string FormatSqlIn(IReadOnlyList<string> items) =>
        "(" + string.Join(", ", items.Select(i => "'" + i.Replace("'", "''") + "'")) + ")";

    /// <summary>RFC 4180 quoting: only wrap in quotes when the field contains a comma, quote,
    /// or newline, doubling any embedded quotes.</summary>
    private static string CsvField(string s)
    {
        var needsQuoting = s.IndexOfAny(['"', ',', '\n', '\r']) >= 0;
        return needsQuoting ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
    }
}
