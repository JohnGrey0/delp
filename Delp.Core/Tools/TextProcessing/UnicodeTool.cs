using System.Globalization;
using System.Text;

namespace Delp.Core.Tools.TextProcessing;

public sealed record UnicodeReport(
    int Utf16Units,
    int Codepoints,
    int Graphemes,
    int Utf8Bytes,
    IReadOnlyList<CharInfo> Chars);

public sealed record CharInfo(
    string Glyph,
    string CodepointHex,
    string Utf8Hex,
    UnicodeCategory Category,
    bool Invisible,
    string? Warning);

/// <summary>Inspects a string codepoint-by-codepoint and grapheme-by-grapheme, flagging
/// zero-width/bidi-control/invisible characters.</summary>
public static class UnicodeTool
{
    private static readonly Dictionary<int, string> InvisibleNames = new()
    {
        [0x200B] = "ZWSP",
        [0x200C] = "ZWNJ",
        [0x200D] = "ZWJ",
        [0x2060] = "WJ",
        [0xFEFF] = "BOM",
        [0x00A0] = "NBSP",
        [0x200E] = "LRM",
        [0x200F] = "RLM",
        [0x061C] = "ALM",
        [0x202A] = "LRE",
        [0x202B] = "RLE",
        [0x202C] = "PDF",
        [0x202D] = "LRO",
        [0x202E] = "RLO",
        [0x2066] = "LRI",
        [0x2067] = "RLI",
        [0x2068] = "FSI",
        [0x2069] = "PDI",
    };

    /// <summary>Default cap on how many codepoints get a detailed <see cref="CharInfo"/>
    /// row built (the UI table only ever shows this many). Aggregate counts
    /// (<see cref="UnicodeReport.Codepoints"/>, Graphemes, Utf8Bytes) always reflect the
    /// full input regardless of the cap, so pasting a huge document still yields correct
    /// totals without paying to materialize a detail row for every codepoint in it.</summary>
    public const int DefaultDisplayCap = 500;

    public static UnicodeReport Inspect(string text, int maxChars = DefaultDisplayCap)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegative(maxChars);

        var chars = new List<CharInfo>(Math.Min(text.Length, maxChars));
        var codepointCount = 0;
        var i = 0;
        while (i < text.Length)
        {
            int codepoint;
            int consumed;
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                codepoint = char.ConvertToUtf32(text[i], text[i + 1]);
                consumed = 2;
            }
            else
            {
                codepoint = text[i];
                consumed = 1;
            }

            codepointCount++;

            // The cap is enforced here, before a CharInfo row (glyph substring, UTF-8 hex
            // string, category lookup) is built — not after the fact via LINQ .Take() — so
            // pasting a huge document only pays the per-row allocation cost up to the cap.
            if (chars.Count < maxChars)
            {
                var glyph = codepoint is >= 0xD800 and <= 0xDFFF
                    ? text[i].ToString() // lone surrogate: not a valid scalar value
                    : char.ConvertFromUtf32(codepoint);
                var category = CharUnicodeInfo.GetUnicodeCategory(text, i);
                var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(glyph);
                var utf8Hex = string.Join(' ', utf8Bytes.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));
                var codepointHex = $"U+{codepoint:X4}";
                var (invisible, warning) = Classify(codepoint, category);
                chars.Add(new CharInfo(glyph, codepointHex, utf8Hex, category, invisible, warning));
            }

            i += consumed;
        }

        return new UnicodeReport(
            Utf16Units: text.Length,
            Codepoints: codepointCount,
            Graphemes: CountGraphemes(text),
            Utf8Bytes: System.Text.Encoding.UTF8.GetByteCount(text),
            Chars: chars);
    }

    private static (bool Invisible, string? Warning) Classify(int codepoint, UnicodeCategory category)
    {
        if (InvisibleNames.TryGetValue(codepoint, out var name))
            return (true, name);
        if (IsVariationSelector(codepoint))
            return (true, "VS");
        if (category == UnicodeCategory.Control && codepoint is not ('\n' or '\r' or '\t'))
            return (true, "Control character");
        return (false, null);
    }

    private static bool IsVariationSelector(int codepoint) =>
        codepoint is >= 0xFE00 and <= 0xFE0F || codepoint is >= 0xE0100 and <= 0xE01EF;

    private static int CountGraphemes(string text)
    {
        if (text.Length == 0)
            return 0;
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        var count = 0;
        while (enumerator.MoveNext())
            count++;
        return count;
    }
}
