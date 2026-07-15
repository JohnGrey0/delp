using Delp.Core.Tools.WebDev;

namespace Delp.Core.Tests.Tools.WebDev;

public class ColorVisionToolTests
{
    // Reference outputs computed from the Viénot/Brettel/Mollon 1999 matrices (protan/deutan,
    // verified against libDaltonLens) and the common single-matrix tritanopia extension,
    // applied in linear RGB with sRGB gamma decode/encode — the same pipeline the tool uses.
    [Theory]
    [InlineData(ColorVisionKind.Protanopia, (byte)255, (byte)0, (byte)0, 94, 94, 13)]
    [InlineData(ColorVisionKind.Protanopia, (byte)0, (byte)255, (byte)0, 242, 242, 0)]
    [InlineData(ColorVisionKind.Protanopia, (byte)0, (byte)0, (byte)255, 0, 0, 255)]
    [InlineData(ColorVisionKind.Deuteranopia, (byte)255, (byte)0, (byte)0, 147, 147, 0)]
    [InlineData(ColorVisionKind.Deuteranopia, (byte)0, (byte)255, (byte)0, 219, 219, 41)]
    [InlineData(ColorVisionKind.Deuteranopia, (byte)0, (byte)0, (byte)255, 0, 0, 255)]
    [InlineData(ColorVisionKind.Tritanopia, (byte)255, (byte)0, (byte)0, 255, 0, 0)]
    [InlineData(ColorVisionKind.Tritanopia, (byte)0, (byte)255, (byte)0, 106, 239, 239)]
    [InlineData(ColorVisionKind.Tritanopia, (byte)0, (byte)0, (byte)255, 0, 105, 105)]
    public void Simulate_PurePrimaries_MatchReferenceOutputs(
        ColorVisionKind kind, byte r, byte g, byte b, int expR, int expG, int expB)
    {
        var result = ColorVisionTool.Simulate(new ParsedColor(r, g, b, 255), kind);
        AssertClose(expR, result.R);
        AssertClose(expG, result.G);
        AssertClose(expB, result.B);
    }

    [Theory]
    [InlineData((byte)255, (byte)0, (byte)0)]
    [InlineData((byte)0, (byte)255, (byte)0)]
    [InlineData((byte)0, (byte)0, (byte)255)]
    [InlineData((byte)0x33, (byte)0x66, (byte)0x99)]
    public void Simulate_Achromatopsia_IsLuminanceGray(byte r, byte g, byte b)
    {
        var color = new ParsedColor(r, g, b, 255);
        var result = ColorVisionTool.Simulate(color, ColorVisionKind.Achromatopsia);

        // Must be a pure gray...
        Assert.Equal(result.R, result.G);
        Assert.Equal(result.G, result.B);

        // ...whose level is the sRGB encoding of the color's relative luminance (reusing
        // ColorTool.Luminance as the reference for the linear-space luminance value).
        var y = ColorTool.Luminance(color);
        var expected = y <= 0.0031308 ? 12.92 * y : 1.055 * Math.Pow(y, 1.0 / 2.4) - 0.055;
        AssertClose((int)Math.Round(expected * 255.0), result.R);
    }

    [Theory]
    [InlineData(ColorVisionKind.Protanopia)]
    [InlineData(ColorVisionKind.Deuteranopia)]
    [InlineData(ColorVisionKind.Tritanopia)]
    [InlineData(ColorVisionKind.Achromatopsia)]
    public void Simulate_WhiteAndBlack_AreStable(ColorVisionKind kind)
    {
        var white = ColorVisionTool.Simulate(new ParsedColor(255, 255, 255, 255), kind);
        AssertClose(255, white.R);
        AssertClose(255, white.G);
        AssertClose(255, white.B);

        var black = ColorVisionTool.Simulate(new ParsedColor(0, 0, 0, 255), kind);
        AssertClose(0, black.R);
        AssertClose(0, black.G);
        AssertClose(0, black.B);
    }

    [Fact]
    public void Simulate_PreservesAlpha()
    {
        var result = ColorVisionTool.Simulate(new ParsedColor(10, 200, 40, 128), ColorVisionKind.Deuteranopia);
        Assert.Equal(128, result.A);
    }

    [Fact]
    public void Simulate_ProtanopiaAndDeuteranopia_CollapseRedGreenChannels()
    {
        // Both red-green deficiencies map R and G to the same output value by construction
        // (their matrix rows 0 and 1 are identical).
        foreach (var kind in new[] { ColorVisionKind.Protanopia, ColorVisionKind.Deuteranopia })
        {
            var result = ColorVisionTool.Simulate(new ParsedColor(200, 60, 120, 255), kind);
            Assert.Equal(result.R, result.G);
        }
    }

    /// <summary>Spec tolerance: ±2 per channel against the published reference outputs.</summary>
    private static void AssertClose(int expected, byte actual) =>
        Assert.True(Math.Abs(expected - actual) <= 2, $"Expected {expected}±2, got {actual}.");
}

public class WcagContrastToolTests
{
    [Theory]
    // Exactly at each threshold — inclusive per WCAG ("at least").
    [InlineData(4.5, true, true, false, true)]
    [InlineData(3.0, false, true, false, false)]
    [InlineData(7.0, true, true, true, true)]
    // Just below each threshold.
    [InlineData(4.49, false, true, false, false)]
    [InlineData(2.99, false, false, false, false)]
    [InlineData(6.99, true, true, false, true)]
    // Extremes.
    [InlineData(1.0, false, false, false, false)]
    [InlineData(21.0, true, true, true, true)]
    public void Evaluate_BoundaryRatios_ProduceExpectedBadges(
        double ratio, bool aaNormal, bool aaLarge, bool aaaNormal, bool aaaLarge)
    {
        var badges = WcagContrastTool.Evaluate(ratio);
        Assert.Equal(aaNormal, badges.AaNormal);
        Assert.Equal(aaLarge, badges.AaLarge);
        Assert.Equal(aaaNormal, badges.AaaNormal);
        Assert.Equal(aaaLarge, badges.AaaLarge);
    }

    [Fact]
    public void Evaluate_BlackOnWhite_PassesEverything()
    {
        var ratio = ColorTool.ContrastRatio(new ParsedColor(0, 0, 0, 255), new ParsedColor(255, 255, 255, 255));
        var badges = WcagContrastTool.Evaluate(ratio);
        Assert.True(badges is { AaNormal: true, AaLarge: true, AaaNormal: true, AaaLarge: true });
    }
}
