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
}
