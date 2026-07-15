namespace Delp.Core.Tools.WebDev;

/// <summary>Pass/fail against the four WCAG 2.x contrast thresholds for a given contrast ratio.</summary>
public readonly record struct WcagBadges(bool AaNormal, bool AaLarge, bool AaaNormal, bool AaaLarge);

/// <summary>
/// Evaluates a WCAG contrast ratio (from <see cref="ColorTool.ContrastRatio"/>) against the
/// standard success-criterion thresholds: AA normal ≥ 4.5, AA large ≥ 3, AAA normal ≥ 7,
/// AAA large ≥ 4.5 (WCAG 2.x SC 1.4.3 / 1.4.6).
/// </summary>
public static class WcagContrastTool
{
    public static WcagBadges Evaluate(double ratio) => new(
        AaNormal: ratio >= 4.5,
        AaLarge: ratio >= 3.0,
        AaaNormal: ratio >= 7.0,
        AaaLarge: ratio >= 4.5);
}

/// <summary>The kinds of color vision deficiency this tool can simulate.</summary>
public enum ColorVisionKind
{
    Protanopia,
    Deuteranopia,
    Tritanopia,
    Achromatopsia,
}

/// <summary>
/// Simulates how a color appears under common color vision deficiencies.
///
/// Protanopia and deuteranopia use the single-matrix method of
/// Viénot, F., Brettel, H., &amp; Mollon, J. D. (1999), "Digital video colourmaps for checking
/// the legibility of displays by dichromats", Color Research &amp; Application 24(4), 243-252.
/// The combined 3x3 matrices below were recomputed from that paper's intermediate equations
/// and match the constants shipped in libDaltonLens (public domain,
/// https://github.com/DaltonLens/libDaltonLens) to ~2e-5.
///
/// Tritanopia is NOT covered by the 1999 paper (it strictly needs the two-plane method of
/// Brettel, Viénot &amp; Mollon 1997, JOSA A 14(10), 2647-2655); the matrix used here is the
/// widely-reproduced single-plane extension of the same technique, as also shipped by
/// libDaltonLens — a good approximation, but less accurate than the full 1997 algorithm.
///
/// All matrices apply to LINEAR RGB: sRGB gamma is decoded first and re-encoded after, per
/// the original paper (skipping linearization is a common bug in older simulators).
/// Achromatopsia is rendered as the Rec. 709 relative-luminance gray of the linearized color.
/// </summary>
public static class ColorVisionTool
{
    // Viénot/Brettel/Mollon 1999, verified against libDaltonLens (row-major, applied as M · [R G B]ᵀ).
    private static readonly double[,] Protanopia =
    {
        { 0.11238, 0.88762,  0.00000 },
        { 0.11238, 0.88762,  0.00000 },
        { 0.00401, -0.00401, 1.00000 },
    };

    private static readonly double[,] Deuteranopia =
    {
        {  0.29275, 0.70725, 0.00000 },
        {  0.29275, 0.70725, 0.00000 },
        { -0.02234, 0.02234, 1.00000 },
    };

    // Unofficial single-matrix tritanopia extension (see class comment).
    private static readonly double[,] Tritanopia =
    {
        { 1.00000, 0.14461, -0.14461 },
        { 0.00000, 0.85924,  0.14076 },
        { 0.00000, 0.85924,  0.14076 },
    };

    /// <summary>Simulates <paramref name="color"/> as seen with <paramref name="kind"/>. Alpha is preserved.</summary>
    public static ParsedColor Simulate(ParsedColor color, ColorVisionKind kind)
    {
        var r = SrgbToLinear(color.R);
        var g = SrgbToLinear(color.G);
        var b = SrgbToLinear(color.B);

        double outR, outG, outB;
        if (kind == ColorVisionKind.Achromatopsia)
        {
            // Rec. 709 relative luminance in linear space — the same coefficients ColorTool.Luminance uses.
            var y = 0.2126 * r + 0.7152 * g + 0.0722 * b;
            outR = outG = outB = y;
        }
        else
        {
            var m = kind switch
            {
                ColorVisionKind.Protanopia => Protanopia,
                ColorVisionKind.Deuteranopia => Deuteranopia,
                ColorVisionKind.Tritanopia => Tritanopia,
                _ => throw new ArgumentOutOfRangeException(nameof(kind)),
            };
            outR = m[0, 0] * r + m[0, 1] * g + m[0, 2] * b;
            outG = m[1, 0] * r + m[1, 1] * g + m[1, 2] * b;
            outB = m[2, 0] * r + m[2, 1] * g + m[2, 2] * b;
        }

        return new ParsedColor(LinearToSrgb(outR), LinearToSrgb(outG), LinearToSrgb(outB), color.A);
    }

    /// <summary>IEC 61966-2-1 sRGB decoding: 8-bit channel to linear-light [0,1].</summary>
    private static double SrgbToLinear(byte channel)
    {
        var v = channel / 255.0;
        return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
    }

    /// <summary>IEC 61966-2-1 sRGB encoding: linear-light to 8-bit channel, clamped.</summary>
    private static byte LinearToSrgb(double linear)
    {
        var v = Math.Clamp(linear, 0.0, 1.0);
        var s = v <= 0.0031308 ? 12.92 * v : 1.055 * Math.Pow(v, 1.0 / 2.4) - 0.055;
        return (byte)Math.Clamp(Math.Round(s * 255.0, MidpointRounding.AwayFromZero), 0, 255);
    }
}
