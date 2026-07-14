using Delp.Core.Tools.WebDev;

namespace Delp.Core.Tests.Tools.WebDev;

public class ScreenColorToolTests
{
    [Fact]
    public void Formats_PureWhite_ReturnsExpectedNotations()
    {
        var f = ScreenColorTool.Formats(255, 255, 255);
        Assert.Equal("#FFFFFF", f.Hex);
        Assert.Equal("rgb(255, 255, 255)", f.Rgb);
        Assert.Equal("hsl(0.0, 0.0%, 100.0%)", f.Hsl);
    }

    [Fact]
    public void Formats_PureBlack_ReturnsExpectedNotations()
    {
        var f = ScreenColorTool.Formats(0, 0, 0);
        Assert.Equal("#000000", f.Hex);
        Assert.Equal("rgb(0, 0, 0)", f.Rgb);
        Assert.Equal("hsl(0.0, 0.0%, 0.0%)", f.Hsl);
    }

    [Fact]
    public void Formats_336699_MatchesKnownHsl()
    {
        var f = ScreenColorTool.Formats(0x33, 0x66, 0x99);
        Assert.Equal("#336699", f.Hex);
        Assert.Equal("rgb(51, 102, 153)", f.Rgb);
        Assert.Equal("hsl(210.0, 50.0%, 40.0%)", f.Hsl);
    }

    [Theory]
    [InlineData((byte)255, (byte)0, (byte)0, 0, 100, 50)]
    [InlineData((byte)0, (byte)255, (byte)0, 120, 100, 50)]
    [InlineData((byte)0, (byte)0, (byte)255, 240, 100, 50)]
    public void ToHsl_PrimaryColors_MatchKnownValues(byte r, byte g, byte b, double h, double s, double l)
    {
        var (hh, ss, ll) = ScreenColorTool.ToHsl(r, g, b);
        Assert.Equal(h, hh, 1);
        Assert.Equal(s, ss, 1);
        Assert.Equal(l, ll, 1);
    }

    [Fact]
    public void Formats_Gray_HasZeroSaturation()
    {
        var f = ScreenColorTool.Formats(128, 128, 128);
        Assert.Equal("#808080", f.Hex);
        Assert.StartsWith("hsl(0.0, 0.0%,", f.Hsl);
    }

    [Fact]
    public void ToHsl_RoundsToOneDecimal()
    {
        var f = ScreenColorTool.Formats(10, 20, 30);
        Assert.Matches(@"^hsl\(\d+\.\d, \d+\.\d%, \d+\.\d%\)$", f.Hsl);
    }
}
