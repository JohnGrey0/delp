using Delp.Core.Tools.WebDev;

namespace Delp.Core.Tests.Tools.WebDev;

public class PlaceholderToolTests
{
    private static PlaceholderOptions Options(int w = 600, int h = 400, string bg = "#2A2E34", string fg = "#8A919B", string? label = null) =>
        new(w, h, bg, fg, label, ImageKind.Png);

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(4097)]
    public void Validate_WidthOutOfRange_Throws(int width)
    {
        Assert.Throws<ArgumentException>(() => PlaceholderTool.Validate(Options(w: width)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5000)]
    public void Validate_HeightOutOfRange_Throws(int height)
    {
        Assert.Throws<ArgumentException>(() => PlaceholderTool.Validate(Options(h: height)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4096)]
    public void Validate_BoundaryDimensions_DoNotThrow(int dimension)
    {
        var ex = Record.Exception(() => PlaceholderTool.Validate(Options(w: dimension, h: dimension)));
        Assert.Null(ex);
    }

    [Fact]
    public void DefaultLabel_IsWidthTimesHeight()
    {
        Assert.Equal("600×400", PlaceholderTool.DefaultLabel(600, 400));
    }

    [Theory]
    [InlineData("#fff")]
    [InlineData("#FFFFFF")]
    [InlineData("2A2E34")]
    public void Validate_ValidHexColors_DoNotThrow(string color)
    {
        var ex = Record.Exception(() => PlaceholderTool.Validate(Options(bg: color)));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#12")]
    [InlineData("#gg0000")]
    [InlineData("")]
    public void Validate_InvalidHexColors_Throw(string color)
    {
        Assert.Throws<ArgumentException>(() => PlaceholderTool.Validate(Options(bg: color)));
    }
}
