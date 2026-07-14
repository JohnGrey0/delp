using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class AsciiArtToolTests
{
    [Theory]
    [InlineData("Standard")]
    [InlineData("Block")]
    [InlineData("Big")]
    public void Render_Hi_ProducesNonEmptyMultiLineOutput(string font)
    {
        var art = AsciiArtTool.Render("Hi", font);
        Assert.False(string.IsNullOrWhiteSpace(art));
        var lines = art.Split('\n');
        Assert.True(lines.Length > 1);
    }

    [Fact]
    public void Render_UnknownFont_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => AsciiArtTool.Render("Hi", "NotAFont"));
    }

    [Fact]
    public void Render_EmptyText_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => AsciiArtTool.Render("", "Standard"));
    }

    [Fact]
    public void FontNames_AllResolveWithoutThrowing()
    {
        Assert.NotEmpty(AsciiArtTool.FontNames);
        foreach (var font in AsciiArtTool.FontNames)
        {
            var art = AsciiArtTool.Render("Hi", font);
            Assert.False(string.IsNullOrWhiteSpace(art));
        }
    }

    [Fact]
    public void Render_IsCaseInsensitiveToFontName()
    {
        var lower = AsciiArtTool.Render("Hi", "standard");
        var upper = AsciiArtTool.Render("Hi", "STANDARD");
        Assert.Equal(lower, upper);
    }

    [Fact]
    public void Render_LowercasesInputToUppercaseGlyphs()
    {
        var lower = AsciiArtTool.Render("hi", "Standard");
        var upper = AsciiArtTool.Render("HI", "Standard");
        Assert.Equal(upper, lower);
    }

    [Fact]
    public void Render_UnknownCharacter_RendersBlankGlyphInsteadOfThrowing()
    {
        var art = AsciiArtTool.Render("A€B", "Standard"); // Euro sign has no glyph
        Assert.False(string.IsNullOrWhiteSpace(art));
    }

    [Fact]
    public void Render_Small_ProducesFewerRowsThanStandard()
    {
        var standard = AsciiArtTool.Render("Hi", "Standard").Split('\n');
        var small = AsciiArtTool.Render("Hi", "Small").Split('\n');
        Assert.True(small.Length < standard.Length);
    }

    [Fact]
    public void Render_Block_UsesSolidBlockCharacter()
    {
        var art = AsciiArtTool.Render("I", "Block");
        Assert.Contains('█', art);
        Assert.DoesNotContain('#', art);
    }

    [Fact]
    public void Render_Digits_ProduceNonEmptyOutput()
    {
        var art = AsciiArtTool.Render("2026", "Standard");
        Assert.False(string.IsNullOrWhiteSpace(art));
    }

    [Fact]
    public void Render_Small_KeepsClosingStrokeSoLettersStayDistinguishable()
    {
        // Regression test: a naive "every other row starting at 0" downsample drops the last row entirely, which
        // erases I's bottom bar (making it look like a "T") and L's foot (making it look like a bare "|"). The
        // fix keeps the first, middle, and last row so those closing strokes survive.
        var i = AsciiArtTool.Render("I", "Small").Split('\n');
        var t = AsciiArtTool.Render("T", "Small").Split('\n');
        Assert.NotEqual(t, i);
        Assert.Equal(3, i.Length);
        Assert.EndsWith("#####", i[0].TrimEnd());
        Assert.EndsWith("#####", i[2].TrimEnd());

        var l = AsciiArtTool.Render("L", "Small").Split('\n');
        Assert.Contains('#', l[2][1..]); // the foot: something lit beyond the leading stem column
    }

    [Fact]
    public void Render_Block_IsNotJustStandardWithASwappedInkCharacter()
    {
        // Standard and Block must be genuinely distinct glyph shapes, not the same bitmap with a different fill
        // character (Block is bold/width-doubled).
        var standard = AsciiArtTool.Render("HI", "Standard");
        var block = AsciiArtTool.Render("HI", "Block").Replace('█', '#');
        Assert.NotEqual(standard, block);
    }
}
