using System.Globalization;

namespace Delp.Core.Tools.WebDev;

/// <summary>
/// Formats an RGB sample (e.g. picked from the screen) into common CSS color
/// notations. Deliberately self-contained: does not depend on any other
/// color tool in the app.
/// </summary>
public static class ScreenColorTool
{
    /// <summary>Hex, rgb() and hsl() text for a single RGB sample.</summary>
    public sealed record ColorFormats(string Hex, string Rgb, string Hsl);

    public static ColorFormats Formats(byte r, byte g, byte b)
    {
        var hex = string.Create(CultureInfo.InvariantCulture, $"#{r:X2}{g:X2}{b:X2}");
        var rgb = string.Create(CultureInfo.InvariantCulture, $"rgb({r}, {g}, {b})");
        var (h, s, l) = ToHsl(r, g, b);
        var hsl = string.Create(CultureInfo.InvariantCulture,
            $"hsl({h.ToString("0.0", CultureInfo.InvariantCulture)}, {s.ToString("0.0", CultureInfo.InvariantCulture)}%, {l.ToString("0.0", CultureInfo.InvariantCulture)}%)");
        return new ColorFormats(hex, rgb, hsl);
    }

    /// <summary>Converts 8-bit RGB to HSL (H in degrees 0-360, S and L in percent 0-100).</summary>
    public static (double H, double S, double L) ToHsl(byte r, byte g, byte b)
    {
        double rf = r / 255.0;
        double gf = g / 255.0;
        double bf = b / 255.0;

        double max = Math.Max(rf, Math.Max(gf, bf));
        double min = Math.Min(rf, Math.Min(gf, bf));
        double l = (max + min) / 2.0;

        if (Math.Abs(max - min) < 1e-9)
            return (0, 0, Math.Round(l * 100, 1, MidpointRounding.AwayFromZero));

        double d = max - min;
        double s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);

        double h;
        if (max == rf)
            h = (gf - bf) / d + (gf < bf ? 6 : 0);
        else if (max == gf)
            h = (bf - rf) / d + 2;
        else
            h = (rf - gf) / d + 4;
        h *= 60;

        return (
            Math.Round(h, 1, MidpointRounding.AwayFromZero),
            Math.Round(s * 100, 1, MidpointRounding.AwayFromZero),
            Math.Round(l * 100, 1, MidpointRounding.AwayFromZero));
    }
}
