using System.Text;

namespace Delp.Core.Tools.TextProcessing;

public enum LineEnding
{
    /// <summary>Leave each line's original ending untouched.</summary>
    None,
    Lf,
    CrLf,
}

public sealed record WhitespaceCleanOptions(
    bool TrimTrailing = false,
    bool TrimLeading = false,
    bool CollapseSpaces = false,
    bool TabsToSpaces = false,
    int TabWidth = 4,
    bool SpacesToTabs = false,
    bool RemoveEmptyLines = false,
    bool CollapseEmptyLines = false,
    LineEnding Normalize = LineEnding.None,
    bool StripZeroWidth = false);

public sealed record WhitespaceCleanResult(string Text, int Changes);

/// <summary>Makes whitespace visible and offers a deterministic whitespace cleanup pass.</summary>
public static class WhitespaceTool
{
    // Every non-ASCII character used by this class is built from its codepoint (not typed
    // as a literal glyph) so the source file never embeds ambiguous/invisible characters.
    private const char Middot = (char)0x00B7;         // ·  visible-space glyph
    private const char Arrow = (char)0x2192;          // →  visible-tab glyph
    private const char SymCr = (char)0x240D;          // ␍
    private const char SymLf = (char)0x240A;          // ␊
    private const char SymNbsp = (char)0x237D;         // ⍽
    private const char GuillemetOpen = (char)0x2039;   // ‹
    private const char GuillemetClose = (char)0x203A;  // ›

    private const char Nbsp = (char)0x00A0;
    private const char Zwsp = (char)0x200B;
    private const char Zwnj = (char)0x200C;
    private const char Zwj = (char)0x200D;
    private const char WordJoiner = (char)0x2060;
    private const char Bom = (char)0xFEFF;

    private static readonly Dictionary<char, string> ZeroWidthNames = new()
    {
        [Zwsp] = Bracket("ZWSP"),
        [Zwnj] = Bracket("ZWNJ"),
        [Zwj] = Bracket("ZWJ"),
        [WordJoiner] = Bracket("WJ"),
        [Bom] = Bracket("BOM"),
    };

    private static string Bracket(string name) => GuillemetOpen + name + GuillemetClose;

    /// <summary>Replaces whitespace with visible glyphs. Real CR/LF characters are kept
    /// alongside their glyph so the result still renders as multiple lines.</summary>
    public static string Visualize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            switch (c)
            {
                case ' ': sb.Append(Middot); break;
                case '\t': sb.Append(Arrow); break;
                case '\r': sb.Append(SymCr).Append('\r'); break;
                case '\n': sb.Append(SymLf).Append('\n'); break;
                case Nbsp: sb.Append(SymNbsp); break;
                default:
                    if (ZeroWidthNames.TryGetValue(c, out var marker))
                        sb.Append(marker);
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>Applies the requested cleanup operations, per line, in a fixed order:
    /// strip zero-width → (tabs↔spaces) → collapse spaces → trim leading/trailing →
    /// remove/collapse empty lines → normalize line endings. Returns the cleaned text and
    /// a count of individual whitespace edits that were actually made.</summary>
    public static WhitespaceCleanResult Clean(string text, WhitespaceCleanOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        var changes = 0;
        var working = text;

        if (options.StripZeroWidth)
        {
            var sb = new StringBuilder(working.Length);
            foreach (var c in working)
            {
                if (ZeroWidthNames.ContainsKey(c))
                    changes++;
                else
                    sb.Append(c);
            }
            working = sb.ToString();
        }

        var lines = SplitLines(working, out var endings);

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (options.TabsToSpaces)
            {
                var expanded = ExpandTabs(line, options.TabWidth);
                if (expanded != line) changes++;
                line = expanded;
            }
            else if (options.SpacesToTabs)
            {
                var condensed = CondenseSpacesToTabs(line, options.TabWidth);
                if (condensed != line) changes++;
                line = condensed;
            }

            if (options.CollapseSpaces)
            {
                var collapsed = CollapseRepeatedSpaces(line);
                if (collapsed != line) changes++;
                line = collapsed;
            }

            if (options.TrimLeading)
            {
                var trimmed = line.TrimStart(' ', '\t');
                if (trimmed != line) changes++;
                line = trimmed;
            }

            if (options.TrimTrailing)
            {
                var trimmed = line.TrimEnd(' ', '\t');
                if (trimmed != line) changes++;
                line = trimmed;
            }

            lines[i] = line;
        }

        if (options.RemoveEmptyLines)
        {
            var kept = new List<string>();
            var keptEndings = new List<string>();
            for (var i = 0; i < lines.Count; i++)
            {
                if (lines[i].Length == 0)
                {
                    changes++;
                    continue;
                }
                kept.Add(lines[i]);
                keptEndings.Add(endings[i]);
            }
            lines = kept;
            endings = keptEndings;
        }
        else if (options.CollapseEmptyLines)
        {
            var kept = new List<string>();
            var keptEndings = new List<string>();
            var previousWasEmpty = false;
            for (var i = 0; i < lines.Count; i++)
            {
                var isEmpty = lines[i].Length == 0;
                if (isEmpty && previousWasEmpty)
                {
                    changes++;
                    continue;
                }
                kept.Add(lines[i]);
                keptEndings.Add(endings[i]);
                previousWasEmpty = isEmpty;
            }
            lines = kept;
            endings = keptEndings;
        }

        var result = Join(lines, endings, options.Normalize, ref changes);
        return new WhitespaceCleanResult(result, changes);
    }

    private static List<string> SplitLines(string text, out List<string> endings)
    {
        var lines = new List<string>();
        endings = new List<string>();
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r')
            {
                var hasLf = i + 1 < text.Length && text[i + 1] == '\n';
                lines.Add(text[start..i]);
                endings.Add(hasLf ? "\r\n" : "\r");
                if (hasLf) i++;
                start = i + 1;
            }
            else if (text[i] == '\n')
            {
                lines.Add(text[start..i]);
                endings.Add("\n");
                start = i + 1;
            }
        }
        lines.Add(text[start..]);
        endings.Add(""); // final fragment: no trailing line ending recorded
        return lines;
    }

    private static string Join(List<string> lines, List<string> endings, LineEnding normalize, ref int changes)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            sb.Append(lines[i]);
            var originalEnding = endings[i];
            if (originalEnding.Length == 0)
                continue; // nothing followed this fragment in the source text

            var ending = normalize switch
            {
                LineEnding.Lf => "\n",
                LineEnding.CrLf => "\r\n",
                _ => originalEnding,
            };
            if (ending != originalEnding)
                changes++;
            sb.Append(ending);
        }
        return sb.ToString();
    }

    private static string ExpandTabs(string line, int tabWidth)
    {
        if (tabWidth <= 0) tabWidth = 4;
        var sb = new StringBuilder(line.Length);
        var col = 0;
        foreach (var c in line)
        {
            if (c == '\t')
            {
                var spaces = tabWidth - (col % tabWidth);
                sb.Append(' ', spaces);
                col += spaces;
            }
            else
            {
                sb.Append(c);
                col++;
            }
        }
        return sb.ToString();
    }

    private static string CondenseSpacesToTabs(string line, int tabWidth)
    {
        if (tabWidth <= 0) tabWidth = 4;
        var spacesRun = new string(' ', tabWidth);
        return line.Replace(spacesRun, "\t");
    }

    private static string CollapseRepeatedSpaces(string line)
    {
        var sb = new StringBuilder(line.Length);
        var i = 0;
        while (i < line.Length)
        {
            var c = line[i];
            if (c == ' ')
            {
                sb.Append(' ');
                while (i < line.Length && line[i] == ' ') i++;
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }
        return sb.ToString();
    }
}
