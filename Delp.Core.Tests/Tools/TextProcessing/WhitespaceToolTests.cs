using Delp.Core.Tools.TextProcessing;

namespace Delp.Core.Tests.Tools.TextProcessing;

public class WhitespaceToolTests
{
    [Fact]
    public void Visualize_MapsGlyphs()
    {
        var result = WhitespaceTool.Visualize("a b\tc\r\n");
        Assert.Equal("a·b→c␍\r␊\n", result);
    }

    [Fact]
    public void Visualize_MarksNbspAndZeroWidth()
    {
        var withNbsp = "a" + (char)0x00A0 + "b";
        Assert.Equal("a⍽b", WhitespaceTool.Visualize(withNbsp));

        var withZwsp = "a" + (char)0x200B + "b";
        Assert.Equal("a‹ZWSP›b", WhitespaceTool.Visualize(withZwsp));
    }

    [Fact]
    public void Clean_TrimTrailing_Isolated()
    {
        var result = WhitespaceTool.Clean("hello   \nworld  ", new WhitespaceCleanOptions(TrimTrailing: true));
        Assert.Equal("hello\nworld", result.Text);
        Assert.True(result.Changes > 0);
    }

    [Fact]
    public void Clean_TrimLeading_Isolated()
    {
        var result = WhitespaceTool.Clean("   hello\n  world", new WhitespaceCleanOptions(TrimLeading: true));
        Assert.Equal("hello\nworld", result.Text);
    }

    [Fact]
    public void Clean_CollapseSpaces_Isolated()
    {
        var result = WhitespaceTool.Clean("a    b     c", new WhitespaceCleanOptions(CollapseSpaces: true));
        Assert.Equal("a b c", result.Text);
    }

    [Fact]
    public void Clean_TabsToSpaces_And_SpacesToTabs_RoundTripAtWidth4()
    {
        const string original = "\t\tfoo";
        var expanded = WhitespaceTool.Clean(original, new WhitespaceCleanOptions(TabsToSpaces: true, TabWidth: 4));
        Assert.Equal("        foo", expanded.Text);

        var condensed = WhitespaceTool.Clean(expanded.Text, new WhitespaceCleanOptions(SpacesToTabs: true, TabWidth: 4));
        Assert.Equal(original, condensed.Text);
    }

    [Fact]
    public void Clean_RemoveEmptyLines_Isolated()
    {
        var result = WhitespaceTool.Clean("a\n\nb\n\n\nc", new WhitespaceCleanOptions(RemoveEmptyLines: true));
        Assert.Equal("a\nb\nc", result.Text);
    }

    [Fact]
    public void Clean_CollapseEmptyLines_Isolated()
    {
        var result = WhitespaceTool.Clean("a\n\n\n\nb", new WhitespaceCleanOptions(CollapseEmptyLines: true));
        Assert.Equal("a\n\nb", result.Text);
    }

    [Fact]
    public void Clean_StripZeroWidth_Isolated()
    {
        var input = "a" + (char)0x200B + "b" + (char)0xFEFF + "c";
        var result = WhitespaceTool.Clean(input, new WhitespaceCleanOptions(StripZeroWidth: true));
        Assert.Equal("abc", result.Text);
        Assert.Equal(2, result.Changes);
    }

    [Fact]
    public void Clean_NormalizeMixedLineEndings_ToLf()
    {
        var result = WhitespaceTool.Clean("a\r\nb\rc\nd", new WhitespaceCleanOptions(Normalize: LineEnding.Lf));
        Assert.Equal("a\nb\nc\nd", result.Text);
    }

    [Fact]
    public void Clean_NormalizeMixedLineEndings_ToCrLf()
    {
        var result = WhitespaceTool.Clean("a\nb\rc", new WhitespaceCleanOptions(Normalize: LineEnding.CrLf));
        Assert.Equal("a\r\nb\r\nc", result.Text);
    }

    [Fact]
    public void Clean_NoOptions_ReportsZeroChanges()
    {
        var result = WhitespaceTool.Clean("unchanged text", new WhitespaceCleanOptions());
        Assert.Equal("unchanged text", result.Text);
        Assert.Equal(0, result.Changes);
    }
}
