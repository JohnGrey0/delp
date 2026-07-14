using Delp.Core.Tools.TextProcessing;

namespace Delp.Core.Tests.Tools.TextProcessing;

public class SlugToolTests
{
    [Fact]
    public void Make_ClassicExample_MatchesExpectedSlug()
    {
        var slug = SlugTool.Make("Hello, World & Friends!", new SlugOptions());
        Assert.Equal("hello-world-friends", slug);
    }

    [Theory]
    [InlineData("café", "cafe")]
    [InlineData("naïve", "naive")]
    [InlineData("Straße", "strasse")]
    [InlineData("Müller", "muller")]
    [InlineData("Ærø", "aero")]
    [InlineData("Đà Nẵng", "da-nang")]
    public void Make_DiacriticsTable_Transliterates(string input, string expected)
    {
        Assert.Equal(expected, SlugTool.Make(input, new SlugOptions()));
    }

    [Fact]
    public void Make_RemoveStopwords_DropsThemWordwise()
    {
        var slug = SlugTool.Make("The Quick Brown Fox and the Lazy Dog",
            new SlugOptions(RemoveStopwords: true));
        Assert.Equal("quick-brown-fox-lazy-dog", slug);
    }

    [Fact]
    public void Make_MaxLength_CutsAtWordBoundary()
    {
        var slug = SlugTool.Make("one two three four five", new SlugOptions(MaxLength: 13));
        Assert.Equal("one-two-three", slug);
        Assert.True(slug.Length <= 13);
    }

    [Fact]
    public void Make_UnderscoreSeparator_IsUsed()
    {
        var slug = SlugTool.Make("Hello World", new SlugOptions(Separator: '_'));
        Assert.Equal("hello_world", slug);
    }

    [Fact]
    public void Make_InvalidSeparator_Throws()
    {
        Assert.Throws<ArgumentException>(() => SlugTool.Make("x", new SlugOptions(Separator: '.')));
    }

    [Fact]
    public void Make_Cjk_PassesThroughUnchanged()
    {
        // CJK script has no case/diacritics to transform; characters are kept as their own word.
        var slug = SlugTool.Make("你好 world", new SlugOptions());
        Assert.Equal("你好-world", slug);
    }

    [Fact]
    public void Make_OnlyFirstLine_OfMultilineInput()
    {
        var slug = SlugTool.Make("First Line\nSecond Line", new SlugOptions());
        Assert.Equal("first-line", slug);
    }

    [Fact]
    public void Make_LowercaseOff_PreservesCase()
    {
        var slug = SlugTool.Make("Hello World", new SlugOptions(Lowercase: false));
        Assert.Equal("Hello-World", slug);
    }
}
