using System.Globalization;
using System.Text.RegularExpressions;

namespace Delp.Core.Tools.WebDev;

/// <summary>An RGBA color with 8-bit channels.</summary>
public readonly record struct ParsedColor(byte R, byte G, byte B, byte A)
{
    public double AlphaFraction => A / 255.0;
}

/// <summary>Hue in degrees [0,360); Saturation/Lightness as percentages [0,100].</summary>
public readonly record struct HslColor(double H, double S, double L);

/// <summary>Hue in degrees [0,360); Saturation/Brightness as percentages [0,100].</summary>
public readonly record struct HsbColor(double H, double S, double B);

/// <summary>
/// Parses and converts CSS-style colors (hex, rgb/rgba, hsl/hsla, hsb/hsv). All math is
/// implemented once here (RGB is the pivot format) so every other converter stays consistent.
/// </summary>
public static class ColorTool
{
    private static readonly Regex FunctionPattern = new(
        @"^(rgba?|hsla?|hsba?|hsva?)\s*\(([^()]*)\)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromSeconds(2));

    /// <summary>
    /// Parses <c>#rgb</c>, <c>#rgba</c>, <c>#rrggbb</c>, <c>#rrggbbaa</c> (with or without leading
    /// <c>#</c>), <c>rgb()</c>/<c>rgba()</c>, <c>hsl()</c>/<c>hsla()</c>, and <c>hsb()</c>/<c>hsv()</c>
    /// (with optional alpha) color strings.
    /// </summary>
    /// <exception cref="FormatException">The input does not match any supported color syntax.</exception>
    public static ParsedColor Parse(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var s = input.Trim();
        if (s.Length == 0)
            throw new FormatException("Color value is empty.");

        var hexBody = s.StartsWith('#') ? s[1..] : s;
        if (hexBody.Length is 3 or 4 or 6 or 8 && IsHex(hexBody))
            return ParseHex(hexBody);

        var match = FunctionPattern.Match(s);
        if (match.Success)
            return ParseFunction(match.Groups[1].Value.ToLowerInvariant(), match.Groups[2].Value);

        throw new FormatException($"Unrecognized color format: '{input}'.");
    }

    /// <summary>Formats as <c>#rrggbb</c> or, when <paramref name="alpha"/> is true, <c>#rrggbbaa</c>.</summary>
    public static string ToHex(ParsedColor c, bool alpha) =>
        alpha ? $"#{c.R:x2}{c.G:x2}{c.B:x2}{c.A:x2}" : $"#{c.R:x2}{c.G:x2}{c.B:x2}";

    public static string ToRgbCss(ParsedColor c) =>
        c.A == 255
            ? string.Format(CultureInfo.InvariantCulture, "rgb({0}, {1}, {2})", c.R, c.G, c.B)
            : string.Format(CultureInfo.InvariantCulture, "rgba({0}, {1}, {2}, {3})", c.R, c.G, c.B, FormatAlphaFraction(c.A));

    public static string ToHslCss(ParsedColor c)
    {
        var hsl = ToHsl(c);
        return c.A == 255
            ? string.Format(CultureInfo.InvariantCulture, "hsl({0}, {1}%, {2}%)", F1(hsl.H), F1(hsl.S), F1(hsl.L))
            : string.Format(CultureInfo.InvariantCulture, "hsla({0}, {1}%, {2}%, {3})", F1(hsl.H), F1(hsl.S), F1(hsl.L), FormatAlphaFraction(c.A));
    }

    public static string ToHsbCss(ParsedColor c)
    {
        var hsb = ToHsb(c);
        return c.A == 255
            ? string.Format(CultureInfo.InvariantCulture, "hsb({0}, {1}%, {2}%)", F1(hsb.H), F1(hsb.S), F1(hsb.B))
            : string.Format(CultureInfo.InvariantCulture, "hsb({0}, {1}%, {2}%, {3})", F1(hsb.H), F1(hsb.S), F1(hsb.B), FormatAlphaFraction(c.A));
    }

    public static HslColor ToHsl(ParsedColor c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;
        var l = (max + min) / 2.0;

        double h, s;
        if (delta == 0)
        {
            h = 0;
            s = 0;
        }
        else
        {
            s = delta / (1 - Math.Abs(2 * l - 1));
            h = Hue(r, g, b, max, delta);
        }

        return new HslColor(Round1(h), Round1(s * 100), Round1(l * 100));
    }

    public static HsbColor ToHsb(ParsedColor c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        var h = delta == 0 ? 0 : Hue(r, g, b, max, delta);
        var s = max == 0 ? 0 : delta / max;

        return new HsbColor(Round1(h), Round1(s * 100), Round1(max * 100));
    }

    public static ParsedColor FromHsl(HslColor hsl, byte a = 255)
    {
        var h = NormalizeHue(hsl.H);
        var s = Math.Clamp(hsl.S, 0, 100) / 100.0;
        var l = Math.Clamp(hsl.L, 0, 100) / 100.0;

        var chroma = (1 - Math.Abs(2 * l - 1)) * s;
        var x = chroma * (1 - Math.Abs(h / 60.0 % 2 - 1));
        var m = l - chroma / 2;

        var (r1, g1, b1) = HuePlane(h, chroma, x);
        return new ParsedColor(ClampByte((r1 + m) * 255), ClampByte((g1 + m) * 255), ClampByte((b1 + m) * 255), a);
    }

    public static ParsedColor FromHsb(HsbColor hsb, byte a = 255)
    {
        var h = NormalizeHue(hsb.H);
        var s = Math.Clamp(hsb.S, 0, 100) / 100.0;
        var v = Math.Clamp(hsb.B, 0, 100) / 100.0;

        var chroma = v * s;
        var x = chroma * (1 - Math.Abs(h / 60.0 % 2 - 1));
        var m = v - chroma;

        var (r1, g1, b1) = HuePlane(h, chroma, x);
        return new ParsedColor(ClampByte((r1 + m) * 255), ClampByte((g1 + m) * 255), ClampByte((b1 + m) * 255), a);
    }

    /// <summary>WCAG 2.x relative luminance, in [0,1].</summary>
    public static double Luminance(ParsedColor c)
    {
        double Linear(byte channel)
        {
            var v = channel / 255.0;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Linear(c.R) + 0.7152 * Linear(c.G) + 0.0722 * Linear(c.B);
    }

    /// <summary>WCAG contrast ratio between two colors, in [1,21].</summary>
    public static double ContrastRatio(ParsedColor a, ParsedColor b)
    {
        var l1 = Luminance(a);
        var l2 = Luminance(b);
        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    // ---- shared RGB<->HSx math ----

    private static double Hue(double r, double g, double b, double max, double delta)
    {
        double h;
        if (max == r)
            h = 60 * Mod(((g - b) / delta), 6);
        else if (max == g)
            h = 60 * (((b - r) / delta) + 2);
        else
            h = 60 * (((r - g) / delta) + 4);

        if (h < 0) h += 360;
        if (h >= 360) h -= 360;
        return h;
    }

    private static (double R, double G, double B) HuePlane(double h, double chroma, double x) => h switch
    {
        < 60 => (chroma, x, 0),
        < 120 => (x, chroma, 0),
        < 180 => (0, chroma, x),
        < 240 => (0, x, chroma),
        < 300 => (x, 0, chroma),
        _ => (chroma, 0, x),
    };

    private static double Mod(double x, double m) => ((x % m) + m) % m;

    private static double NormalizeHue(double h) => Mod(h, 360);

    // ---- parsing ----

    private static ParsedColor ParseFunction(string fn, string argsText)
    {
        var parts = SplitArgs(argsText);
        var isRgb = fn.StartsWith("rgb", StringComparison.Ordinal);
        var isHsl = fn.StartsWith("hsl", StringComparison.Ordinal);

        if (parts.Count != 3 && parts.Count != 4)
            throw new FormatException($"{fn}() expects 3 or 4 components, got {parts.Count}.");

        var alpha = parts.Count == 4 ? ToAlphaByte(parts[3]) : (byte)255;

        if (isRgb)
        {
            var r = ToChannelByte(parts[0]);
            var g = ToChannelByte(parts[1]);
            var b = ToChannelByte(parts[2]);
            return new ParsedColor(r, g, b, alpha);
        }

        var h = ToHueDegrees(parts[0]);
        var p2 = ToPercent(parts[1]);
        var p3 = ToPercent(parts[2]);

        return isHsl
            ? FromHsl(new HslColor(h, p2, p3), alpha)
            : FromHsb(new HsbColor(h, p2, p3), alpha);
    }

    private static List<string> SplitArgs(string args)
    {
        var trimmed = args.Trim();
        if (trimmed.Length == 0)
            return [];

        if (trimmed.Contains(','))
            return trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        // CSS Color 4 space syntax, e.g. "255 0 0 / 50%".
        return trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t != "/")
            .ToList();
    }

    private static ParsedColor ParseHex(string hex) => hex.Length switch
    {
        3 => new ParsedColor(ExpandHex(hex[0]), ExpandHex(hex[1]), ExpandHex(hex[2]), 255),
        4 => new ParsedColor(ExpandHex(hex[0]), ExpandHex(hex[1]), ExpandHex(hex[2]), ExpandHex(hex[3])),
        6 => new ParsedColor(HexByte(hex, 0), HexByte(hex, 2), HexByte(hex, 4), 255),
        8 => new ParsedColor(HexByte(hex, 0), HexByte(hex, 2), HexByte(hex, 4), HexByte(hex, 6)),
        _ => throw new FormatException($"Hex color must be 3, 4, 6, or 8 digits, got {hex.Length}."),
    };

    private static bool IsHex(string s) => s.Length > 0 && s.All(Uri.IsHexDigit);

    private static byte ExpandHex(char c)
    {
        var v = HexNibble(c);
        return (byte)(v * 16 + v);
    }

    private static byte HexByte(string hex, int index) => (byte)(HexNibble(hex[index]) * 16 + HexNibble(hex[index + 1]));

    private static int HexNibble(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => throw new FormatException($"Invalid hex digit '{c}'."),
    };

    private static (double Value, bool IsPercent) ParseToken(string token)
    {
        var t = token.Trim();
        if (t.Length == 0)
            throw new FormatException("Color component is empty.");

        var isPercent = t.EndsWith('%');
        if (isPercent)
            t = t[..^1];
        else if (t.EndsWith("deg", StringComparison.OrdinalIgnoreCase))
            t = t[..^3];

        if (!double.TryParse(t, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v))
            throw new FormatException($"Invalid color component '{token}'.");

        return (v, isPercent);
    }

    private static byte ToChannelByte(string token)
    {
        var (v, isPercent) = ParseToken(token);
        return ClampByte(isPercent ? v / 100.0 * 255.0 : v);
    }

    private static byte ToAlphaByte(string token)
    {
        var (v, isPercent) = ParseToken(token);
        return ClampByte(isPercent ? v / 100.0 * 255.0 : v * 255.0);
    }

    private static double ToHueDegrees(string token)
    {
        var (v, _) = ParseToken(token);
        return NormalizeHue(v);
    }

    private static double ToPercent(string token)
    {
        var (v, _) = ParseToken(token);
        return Math.Clamp(v, 0, 100);
    }

    private static byte ClampByte(double v) => (byte)Math.Clamp(Math.Round(v, MidpointRounding.AwayFromZero), 0, 255);

    private static double Round1(double v) => Math.Round(v, 1, MidpointRounding.AwayFromZero);

    private static string F1(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);

    private static string FormatAlphaFraction(byte a) => Math.Round(a / 255.0, 2).ToString(CultureInfo.InvariantCulture);
}
