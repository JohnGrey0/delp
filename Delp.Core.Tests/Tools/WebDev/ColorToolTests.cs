using Delp.Core.Tools.WebDev;

namespace Delp.Core.Tests.Tools.WebDev;

public class ColorToolTests
{
    [Theory]
    [InlineData("#336699")]
    [InlineData("336699")]
    [InlineData("#369")]
    public void Parse_HexForms_ProduceExpectedRgb(string input)
    {
        var c = ColorTool.Parse(input);
        Assert.Equal(255, c.A);
        if (input.Replace("#", "").Length == 3)
        {
            Assert.Equal(0x33, c.R);
            Assert.Equal(0x66, c.G);
            Assert.Equal(0x99, c.B);
        }
        else
        {
            Assert.Equal(0x33, c.R);
            Assert.Equal(0x66, c.G);
            Assert.Equal(0x99, c.B);
        }
    }

    [Fact]
    public void Parse_HexWithAlpha_ParsesAlphaChannel()
    {
        var c = ColorTool.Parse("#33669980");
        Assert.Equal(0x80, c.A);

        var c2 = ColorTool.Parse("#3690"); // #3 6 9 0 -> expand each nibble
        Assert.Equal(0x00, c2.A);
    }

    [Fact]
    public void Parse_RgbFunction_CommaSeparated()
    {
        var c = ColorTool.Parse("rgb(51, 102, 153)");
        Assert.Equal((byte)51, c.R);
        Assert.Equal((byte)102, c.G);
        Assert.Equal((byte)153, c.B);
        Assert.Equal((byte)255, c.A);
    }

    [Fact]
    public void Parse_RgbFunction_PercentComponents()
    {
        var c = ColorTool.Parse("rgb(100%, 0%, 0%)");
        Assert.Equal((byte)255, c.R);
        Assert.Equal((byte)0, c.G);
        Assert.Equal((byte)0, c.B);
    }

    [Fact]
    public void Parse_RgbaFunction_AlphaAsFractionAndPercent()
    {
        var fraction = ColorTool.Parse("rgba(51,102,153,0.5)");
        Assert.Equal((byte)128, fraction.A);

        var percent = ColorTool.Parse("rgba(51,102,153,50%)");
        Assert.Equal((byte)128, percent.A);
    }

    [Fact]
    public void Parse_RgbFunction_SpaceSyntaxWithSlashAlpha()
    {
        var c = ColorTool.Parse("rgb(51 102 153 / 50%)");
        Assert.Equal((byte)51, c.R);
        Assert.Equal((byte)128, c.A);
    }

    [Fact]
    public void Parse_HslFunction_KnownTriple()
    {
        var c = ColorTool.Parse("hsl(210, 50%, 40%)");
        Assert.Equal((byte)51, c.R);
        Assert.Equal((byte)102, c.G);
        Assert.Equal((byte)153, c.B);
    }

    [Fact]
    public void Parse_HsbAndHsvFunctions_Equivalent()
    {
        var hsb = ColorTool.Parse("hsb(210, 66.7%, 60%)");
        var hsv = ColorTool.Parse("hsv(210, 66.7%, 60%)");
        Assert.Equal(hsb, hsv);
        Assert.Equal((byte)51, hsb.R);
        Assert.Equal((byte)102, hsb.G);
        Assert.Equal((byte)153, hsb.B);
    }

    [Fact]
    public void ToHsl_KnownTriple_336699_Is_210_50_40()
    {
        var c = ColorTool.Parse("#336699");
        var hsl = ColorTool.ToHsl(c);
        Assert.Equal(210.0, hsl.H, 1);
        Assert.Equal(50.0, hsl.S, 1);
        Assert.Equal(40.0, hsl.L, 1);
    }

    [Fact]
    public void ToHsb_KnownTriple_336699()
    {
        var c = ColorTool.Parse("#336699");
        var hsb = ColorTool.ToHsb(c);
        Assert.Equal(210.0, hsb.H, 1);
        Assert.Equal(66.7, hsb.S, 1);
        Assert.Equal(60.0, hsb.B, 1);
    }

    [Fact]
    public void FromHsl_RoundTrips_ToRgb()
    {
        var c = ColorTool.FromHsl(new HslColor(210, 50, 40));
        Assert.Equal((byte)51, c.R);
        Assert.Equal((byte)102, c.G);
        Assert.Equal((byte)153, c.B);
    }

    [Fact]
    public void ToHex_WithAndWithoutAlpha()
    {
        var c = ColorTool.Parse("rgba(51,102,153,0.5)");
        Assert.Equal("#336699", ColorTool.ToHex(c, alpha: false));
        Assert.Equal("#33669980", ColorTool.ToHex(c, alpha: true));
    }

    [Fact]
    public void ToRgbCss_And_ToHslCss_FormatOpaqueAndTransparent()
    {
        var opaque = ColorTool.Parse("#336699");
        Assert.Equal("rgb(51, 102, 153)", ColorTool.ToRgbCss(opaque));
        Assert.Equal("hsl(210.0, 50.0%, 40.0%)", ColorTool.ToHslCss(opaque));

        var withAlpha = ColorTool.Parse("rgba(51,102,153,0.5)");
        Assert.StartsWith("rgba(51, 102, 153, 0.5", ColorTool.ToRgbCss(withAlpha));
        Assert.StartsWith("hsla(210.0, 50.0%, 40.0%,", ColorTool.ToHslCss(withAlpha));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-color")]
    [InlineData("#12")]
    [InlineData("rgb(1,2)")]
    [InlineData("hsl(1,2,3,4,5)")]
    public void Parse_InvalidInput_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => ColorTool.Parse(input));
    }

    [Fact]
    public void ContrastRatio_BlackVsWhite_Is21()
    {
        var black = ColorTool.Parse("#000000");
        var white = ColorTool.Parse("#ffffff");
        Assert.Equal(21.0, ColorTool.ContrastRatio(black, white), 2);
        // Order-independent.
        Assert.Equal(21.0, ColorTool.ContrastRatio(white, black), 2);
    }

    [Fact]
    public void ContrastRatio_SameColor_Is1()
    {
        var c = ColorTool.Parse("#336699");
        Assert.Equal(1.0, ColorTool.ContrastRatio(c, c), 2);
    }

    /// <summary>RGB -&gt; HSL -&gt; RGB must round-trip (within 8-bit rounding) across a grid of colors,
    /// including the grayscale/achromatic edge (R==G==B, where hue is undefined) and pure primaries.</summary>
    [Theory]
    [MemberData(nameof(RgbGrid))]
    public void ToHsl_FromHsl_RoundTrips_AcrossGrid(byte r, byte g, byte b)
    {
        var original = new ParsedColor(r, g, b, 255);
        var hsl = ColorTool.ToHsl(original);
        var roundTripped = ColorTool.FromHsl(hsl);

        AssertCloseByte(original.R, roundTripped.R);
        AssertCloseByte(original.G, roundTripped.G);
        AssertCloseByte(original.B, roundTripped.B);
    }

    /// <summary>RGB -&gt; HSB -&gt; RGB must round-trip (within 8-bit rounding) across the same grid.</summary>
    [Theory]
    [MemberData(nameof(RgbGrid))]
    public void ToHsb_FromHsb_RoundTrips_AcrossGrid(byte r, byte g, byte b)
    {
        var original = new ParsedColor(r, g, b, 255);
        var hsb = ColorTool.ToHsb(original);
        var roundTripped = ColorTool.FromHsb(hsb);

        AssertCloseByte(original.R, roundTripped.R);
        AssertCloseByte(original.G, roundTripped.G);
        AssertCloseByte(original.B, roundTripped.B);
    }

    /// <summary>Every channel value stays within 0-100% saturation/lightness/brightness and 0-360deg hue.</summary>
    [Theory]
    [MemberData(nameof(RgbGrid))]
    public void ToHsl_ToHsb_StayWithinDocumentedRanges(byte r, byte g, byte b)
    {
        var c = new ParsedColor(r, g, b, 255);
        var hsl = ColorTool.ToHsl(c);
        var hsb = ColorTool.ToHsb(c);

        Assert.InRange(hsl.H, 0, 360);
        Assert.InRange(hsl.S, 0, 100);
        Assert.InRange(hsl.L, 0, 100);
        Assert.InRange(hsb.H, 0, 360);
        Assert.InRange(hsb.S, 0, 100);
        Assert.InRange(hsb.B, 0, 100);
    }

    private static void AssertCloseByte(byte expected, byte actual) =>
        Assert.True(Math.Abs(expected - actual) <= 1, $"Expected {expected}, got {actual} (diff > 1).");

    public static TheoryData<byte, byte, byte> RgbGrid()
    {
        var data = new TheoryData<byte, byte, byte>();
        // 17-step grid (0,17,...,255 -> 16 steps) over each channel, plus grayscale/primary edges are
        // naturally included at the 0/255 corners.
        byte[] steps = [0, 17, 34, 85, 128, 170, 221, 255];
        foreach (var r in steps)
            foreach (var g in steps)
                foreach (var b in steps)
                    data.Add(r, g, b);
        return data;
    }
}
