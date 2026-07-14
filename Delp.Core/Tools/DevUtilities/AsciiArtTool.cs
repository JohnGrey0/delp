using System.Text;

namespace Delp.Core.Tools.DevUtilities;

/// <summary>
/// Renders single-line text as large ASCII-art banners.
/// </summary>
/// <remarks>
/// The spec for this tool calls for mapping font names onto <c>Figgle.FiggleFonts</c> properties (the Figgle
/// package's bundled FIGlet font bank). That font bank ships in the separate <c>Figgle.Fonts</c> NuGet package,
/// which is <em>not</em> among Delp's referenced packages (only the core <c>Figgle</c> parser/renderer is
/// referenced, and CONVENTIONS.md forbids adding packages). Per CONVENTIONS.md's fallback rule ("if something
/// seems missing, implement it by hand"), this tool instead hand-rolls a small 5x6 bitmap glyph bank (A-Z, 0-9,
/// space, and common punctuation) and derives several distinct "fonts" from it programmatically
/// (block ink, 2x scale, row-downsampled, drop-shadow, slant). It does not use the Figgle package at all.
/// </remarks>
public static class AsciiArtTool
{
    public const string StandardFont = "Standard";
    public const string BlockFont = "Block";
    public const string BigFont = "Big";
    public const string SmallFont = "Small";
    public const string ShadowFont = "Shadow";
    public const string SlantFont = "Slant";

    /// <summary>Font names for the UI's font picker; every entry is guaranteed to render.</summary>
    public static IReadOnlyList<string> FontNames { get; } = new[]
    {
        StandardFont, BlockFont, BigFont, SmallFont, ShadowFont, SlantFont,
    };

    private const int GlyphHeight = 6;

    private static readonly string[] FallbackGlyph = { "   ", "   ", "   ", "   ", "   ", "   " };

    private static readonly IReadOnlyDictionary<char, string[]> Glyphs = BuildGlyphs();

    /// <summary>Renders <paramref name="text"/> (uppercased) using the named font.</summary>
    /// <exception cref="FormatException">Text is empty, or the font name is not one of <see cref="FontNames"/>.</exception>
    public static string Render(string text, string fontName)
    {
        if (string.IsNullOrEmpty(text))
            throw new FormatException("Enter some text to render.");

        var canonical = FontNames.FirstOrDefault(f => string.Equals(f, fontName, StringComparison.OrdinalIgnoreCase));
        if (canonical is null)
        {
            throw new FormatException(
                $"Unknown font '{fontName}'. Available fonts: {string.Join(", ", FontNames)}.");
        }

        var rows = BuildBaseRows(text.ToUpperInvariant());

        return canonical switch
        {
            StandardFont => Compose(rows, '#'),
            BlockFont => Compose(rows, '█'),
            BigFont => Compose(Scale(rows, 2), '#'),
            SmallFont => Compose(Downsample(rows), '#'),
            ShadowFont => ComposeShadow(rows),
            SlantFont => Compose(Slant(rows), '#'),
            _ => throw new FormatException($"Unknown font '{fontName}'."),
        };
    }

    private static List<string> BuildBaseRows(string text)
    {
        var rows = new List<string>(GlyphHeight);
        for (var r = 0; r < GlyphHeight; r++)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < text.Length; i++)
            {
                var glyph = Glyphs.TryGetValue(text[i], out var g) ? g : FallbackGlyph;
                sb.Append(glyph[r]);
                if (i < text.Length - 1)
                    sb.Append(' ');
            }
            rows.Add(sb.ToString());
        }
        return rows;
    }

    private static string Compose(List<string> rows, char ink) =>
        string.Join('\n', rows.Select(r => r.Replace('#', ink)));

    private static List<string> Scale(List<string> rows, int factor)
    {
        var scaled = new List<string>(rows.Count * factor);
        foreach (var row in rows)
        {
            var wideRow = string.Concat(row.Select(c => new string(c, factor)));
            for (var i = 0; i < factor; i++)
                scaled.Add(wideRow);
        }
        return scaled;
    }

    /// <summary>Compact variant: takes every other row of the base glyphs (6 rows -> 3 rows).</summary>
    private static List<string> Downsample(List<string> rows)
    {
        var result = new List<string>();
        for (var i = 0; i < rows.Count; i += 2)
            result.Add(rows[i]);
        return result;
    }

    private static List<string> Slant(List<string> rows)
    {
        var result = new List<string>(rows.Count);
        for (var i = 0; i < rows.Count; i++)
            result.Add(new string(' ', i) + rows[i]);
        return result;
    }

    private static string ComposeShadow(List<string> rows)
    {
        var height = rows.Count + 1;
        var width = rows.Count == 0 ? 1 : rows.Max(r => r.Length) + 1;
        var canvas = new char[height, width];
        for (var r = 0; r < height; r++)
            for (var c = 0; c < width; c++)
                canvas[r, c] = ' ';

        for (var r = 0; r < rows.Count; r++)
            for (var c = 0; c < rows[r].Length; c++)
                if (rows[r][c] == '#')
                    canvas[r + 1, c + 1] = '.';

        for (var r = 0; r < rows.Count; r++)
            for (var c = 0; c < rows[r].Length; c++)
                if (rows[r][c] == '#')
                    canvas[r, c] = '#';

        var sb = new StringBuilder();
        for (var r = 0; r < height; r++)
        {
            for (var c = 0; c < width; c++)
                sb.Append(canvas[r, c]);
            if (r < height - 1)
                sb.Append('\n');
        }
        return sb.ToString();
    }

    private static Dictionary<char, string[]> BuildGlyphs() => new()
    {
        ['A'] = new[] { " ### ", "#   #", "#   #", "#####", "#   #", "#   #" },
        ['B'] = new[] { "#### ", "#   #", "#### ", "#   #", "#   #", "#### " },
        ['C'] = new[] { " ####", "#    ", "#    ", "#    ", "#    ", " ####" },
        ['D'] = new[] { "#### ", "#   #", "#   #", "#   #", "#   #", "#### " },
        ['E'] = new[] { "#####", "#    ", "#### ", "#    ", "#    ", "#####" },
        ['F'] = new[] { "#####", "#    ", "#### ", "#    ", "#    ", "#    " },
        ['G'] = new[] { " ####", "#    ", "# ###", "#   #", "#   #", " ####" },
        ['H'] = new[] { "#   #", "#   #", "#####", "#   #", "#   #", "#   #" },
        ['I'] = new[] { "#####", "  #  ", "  #  ", "  #  ", "  #  ", "#####" },
        ['J'] = new[] { "  ###", "   # ", "   # ", "   # ", "#  # ", " ##  " },
        ['K'] = new[] { "#   #", "#  # ", "###  ", "#  # ", "#   #", "#   #" },
        ['L'] = new[] { "#    ", "#    ", "#    ", "#    ", "#    ", "#####" },
        ['M'] = new[] { "#   #", "## ##", "# # #", "#   #", "#   #", "#   #" },
        ['N'] = new[] { "#   #", "##  #", "# # #", "#  ##", "#   #", "#   #" },
        ['O'] = new[] { " ### ", "#   #", "#   #", "#   #", "#   #", " ### " },
        ['P'] = new[] { "#### ", "#   #", "#### ", "#    ", "#    ", "#    " },
        ['Q'] = new[] { " ### ", "#   #", "#   #", "# # #", "#  # ", " ## #" },
        ['R'] = new[] { "#### ", "#   #", "#### ", "#  # ", "#   #", "#   #" },
        ['S'] = new[] { " ####", "#    ", " ### ", "    #", "    #", "#### " },
        ['T'] = new[] { "#####", "  #  ", "  #  ", "  #  ", "  #  ", "  #  " },
        ['U'] = new[] { "#   #", "#   #", "#   #", "#   #", "#   #", " ### " },
        ['V'] = new[] { "#   #", "#   #", "#   #", "#   #", " # # ", "  #  " },
        ['W'] = new[] { "#   #", "#   #", "#   #", "# # #", "## ##", "#   #" },
        ['X'] = new[] { "#   #", " # # ", "  #  ", "  #  ", " # # ", "#   #" },
        ['Y'] = new[] { "#   #", " # # ", "  #  ", "  #  ", "  #  ", "  #  " },
        ['Z'] = new[] { "#####", "   # ", "  #  ", " #   ", "#    ", "#####" },
        ['0'] = new[] { " ### ", "#   #", "#  ##", "# # #", "##  #", " ### " },
        ['1'] = new[] { "  #  ", " ##  ", "  #  ", "  #  ", "  #  ", "#####" },
        ['2'] = new[] { " ### ", "#   #", "   # ", "  #  ", " #   ", "#####" },
        ['3'] = new[] { " ### ", "#   #", "  ## ", "    #", "#   #", " ### " },
        ['4'] = new[] { "   # ", "  ## ", " # # ", "#  # ", "#####", "   # " },
        ['5'] = new[] { "#####", "#    ", "#### ", "    #", "#   #", " ### " },
        ['6'] = new[] { "  ## ", " #   ", "#### ", "#   #", "#   #", " ### " },
        ['7'] = new[] { "#####", "    #", "   # ", "  #  ", " #   ", " #   " },
        ['8'] = new[] { " ### ", "#   #", " ### ", "#   #", "#   #", " ### " },
        ['9'] = new[] { " ### ", "#   #", " ####", "    #", "   # ", " ##  " },
        [' '] = new[] { "   ", "   ", "   ", "   ", "   ", "   " },
        ['!'] = new[] { " # ", " # ", " # ", " # ", "   ", " # " },
        ['?'] = new[] { " ### ", "#   #", "   # ", "  #  ", "     ", "  #  " },
        ['.'] = new[] { "   ", "   ", "   ", "   ", "   ", " # " },
        [','] = new[] { "   ", "   ", "   ", "   ", " # ", "#  " },
        ['-'] = new[] { "     ", "     ", "#####", "     ", "     ", "     " },
        ['\''] = new[] { "##", "##", "  ", "  ", "  ", "  " },
        [':'] = new[] { "   ", " # ", "   ", "   ", " # ", "   " },
    };
}
