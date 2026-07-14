using Delp.Core.Tools.Encoding;

namespace Delp.Core.Tests.Tools.Encoding;

public class UnicodeEscapeToolTests
{
    [Fact]
    public void RoundTrip_EmojiAndNewline()
    {
        const string text = "hi 😀\nthere";
        var escaped = UnicodeEscapeTool.Escape(text, nonAsciiOnly: true);
        Assert.Equal(text, UnicodeEscapeTool.Unescape(escaped));
    }

    [Fact]
    public void Escape_NonAsciiOnly_KeepsPlainAsciiUnescaped()
    {
        Assert.Equal("abc", UnicodeEscapeTool.Escape("abc", nonAsciiOnly: true));
    }

    [Fact]
    public void Escape_NotNonAsciiOnly_EscapesEveryCharacter()
    {
        Assert.Equal("\\u0061\\u0062\\u0063", UnicodeEscapeTool.Escape("abc", nonAsciiOnly: false));
    }

    [Fact]
    public void Escape_AstralChar_ProducesTwoUtf16Escapes()
    {
        Assert.Equal("\\ud83d\\ude00", UnicodeEscapeTool.Escape("😀", nonAsciiOnly: true));
    }

    [Fact]
    public void Unescape_EightDigitCodepoint_ProducesEmoji()
    {
        Assert.Equal("😀", UnicodeEscapeTool.Unescape("\\U0001F600"));
    }

    [Fact]
    public void Unescape_HexByteEscape()
    {
        Assert.Equal("A", UnicodeEscapeTool.Unescape("\\x41"));
    }

    [Fact]
    public void Unescape_ShortForms()
    {
        Assert.Equal("a\nb\rc\td\\e\"f'", UnicodeEscapeTool.Unescape("a\\nb\\rc\\td\\\\e\\\"f\\'"));
    }

    [Fact]
    public void Unescape_TrailingBackslash_ThrowsWithPosition()
    {
        var ex = Assert.Throws<FormatException>(() => UnicodeEscapeTool.Unescape("abc\\"));
        Assert.Contains("3", ex.Message);
    }

    [Fact]
    public void Unescape_InvalidHexDigits_Throws()
    {
        Assert.Throws<FormatException>(() => UnicodeEscapeTool.Unescape("\\uZZZZ"));
    }

    [Fact]
    public void Unescape_CodepointAboveMax_Throws()
    {
        Assert.Throws<FormatException>(() => UnicodeEscapeTool.Unescape("\\U00110000"));
    }

    [Fact]
    public void Unescape_UnknownEscape_Throws()
    {
        Assert.Throws<FormatException>(() => UnicodeEscapeTool.Unescape("\\q"));
    }
}
