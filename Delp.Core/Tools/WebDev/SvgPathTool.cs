namespace Delp.Core.Tools.WebDev;

/// <summary>
/// Result of tokenizing/validating an SVG path "d" attribute. Core only tokenizes — bounds
/// (MinX/MinY/Width/Height) require WPF's <c>Geometry.Parse(d).Bounds</c> and are filled in by
/// the App layer (they default to 0 here so this type stays fully testable without WPF).
/// </summary>
public sealed record SvgPathInfo(
    double MinX,
    double MinY,
    double Width,
    double Height,
    int CommandCount,
    IReadOnlyList<string> Commands);

/// <summary>Light hand-rolled tokenizer for the SVG path mini-language (same grammar WPF's Geometry.Parse uses).</summary>
public static class SvgPathTool
{
    private const string CommandLetters = "MmLlHhVvCcSsQqTtAaZz";

    /// <exception cref="FormatException">The path contains an unknown command letter or a malformed number.</exception>
    public static SvgPathInfo Analyze(string d)
    {
        var commands = Tokenize(d);
        return new SvgPathInfo(0, 0, 0, 0, commands.Count, commands);
    }

    /// <summary>Splits a path string into "&lt;command&gt; &lt;args…&gt;" tokens, one per command letter.</summary>
    /// <exception cref="FormatException">The path contains an unknown command letter or a malformed number.</exception>
    public static IReadOnlyList<string> Tokenize(string d)
    {
        d ??= "";
        var result = new List<string>();
        var i = 0;
        var n = d.Length;

        while (i < n)
        {
            var c = d[i];
            if (char.IsWhiteSpace(c) || c == ',')
            {
                i++;
                continue;
            }

            if (CommandLetters.IndexOf(c) < 0)
                throw new FormatException($"Invalid path command '{c}' at position {i}.");

            i++;
            var args = new List<string>();
            while (i < n)
            {
                while (i < n && (char.IsWhiteSpace(d[i]) || d[i] == ','))
                    i++;
                // Any letter ends this command's argument run — hand back to the outer loop,
                // which validates it as the next command (and names it if it's not a real one).
                if (i >= n || char.IsLetter(d[i]))
                    break;

                var numStart = i;
                i = ParseNumber(d, i);
                if (i == numStart)
                    throw new FormatException($"Invalid number near position {numStart} in SVG path.");
                args.Add(d[numStart..i]);
            }

            result.Add(args.Count > 0 ? c + " " + string.Join(" ", args) : c.ToString());
        }

        return result;
    }

    /// <summary>Parses one number (sign, integer/fraction digits, optional exponent incl. "1e-5") starting at <paramref name="i"/>.</summary>
    /// <returns>The index right after the number, or <paramref name="i"/> unchanged if no number starts there.</returns>
    private static int ParseNumber(string s, int i)
    {
        var start = i;
        var n = s.Length;

        if (i < n && (s[i] == '+' || s[i] == '-'))
            i++;

        var intDigits = 0;
        while (i < n && char.IsAsciiDigit(s[i])) { i++; intDigits++; }

        var fracDigits = 0;
        if (i < n && s[i] == '.')
        {
            i++;
            while (i < n && char.IsAsciiDigit(s[i])) { i++; fracDigits++; }
        }

        if (intDigits == 0 && fracDigits == 0)
            return start;

        if (i < n && (s[i] == 'e' || s[i] == 'E'))
        {
            var j = i + 1;
            if (j < n && (s[j] == '+' || s[j] == '-'))
                j++;
            var expDigits = 0;
            while (j < n && char.IsAsciiDigit(s[j])) { j++; expDigits++; }
            if (expDigits > 0)
                i = j;
        }

        return i;
    }
}
