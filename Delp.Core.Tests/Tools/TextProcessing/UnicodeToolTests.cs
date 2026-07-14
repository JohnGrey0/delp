using System.Globalization;
using Delp.Core.Tools.TextProcessing;

namespace Delp.Core.Tests.Tools.TextProcessing;

public class UnicodeToolTests
{
    [Fact]
    public void Inspect_EmptyString_AllZero()
    {
        var report = UnicodeTool.Inspect("");
        Assert.Equal(0, report.Utf16Units);
        Assert.Equal(0, report.Codepoints);
        Assert.Equal(0, report.Graphemes);
        Assert.Equal(0, report.Utf8Bytes);
        Assert.Empty(report.Chars);
    }

    [Fact]
    public void Inspect_AstralEmoji_CountsAsOneCodepointTwoUtf16Units()
    {
        const string grinning = "\U0001F600"; // U+1F600, surrogate pair in UTF-16
        var report = UnicodeTool.Inspect(grinning);
        Assert.Equal(2, report.Utf16Units);
        Assert.Equal(1, report.Codepoints);
        Assert.Equal(1, report.Graphemes);
        Assert.Equal("U+1F600", report.Chars[0].CodepointHex);
        Assert.Equal("F0 9F 98 80", report.Chars[0].Utf8Hex);
    }

    [Fact]
    public void Inspect_ZwjFamilyEmoji_OneGraphemeManyCodepoints()
    {
        // man + ZWJ + woman + ZWJ + girl + ZWJ + boy = 4 emoji + 3 ZWJ = 7 codepoints, 1 grapheme.
        var zwj = "‍";
        var family = "\U0001F468" + zwj + "\U0001F469" + zwj + "\U0001F467" + zwj + "\U0001F466";
        var report = UnicodeTool.Inspect(family);
        Assert.Equal(7, report.Codepoints);
        Assert.Equal(1, report.Graphemes);
    }

    [Fact]
    public void Inspect_ZwspAndRlo_AreFlaggedInvisible()
    {
        var text = "a" + "​" + "b" + "‮" + "c";
        var report = UnicodeTool.Inspect(text);

        var zwsp = report.Chars.Single(c => c.CodepointHex == "U+200B");
        Assert.True(zwsp.Invisible);
        Assert.Equal("ZWSP", zwsp.Warning);

        var rlo = report.Chars.Single(c => c.CodepointHex == "U+202E");
        Assert.True(rlo.Invisible);
        Assert.Equal("RLO", rlo.Warning);

        var a = report.Chars.Single(c => c.CodepointHex == "U+0061");
        Assert.False(a.Invisible);
    }

    [Fact]
    public void Inspect_RegularAsciiLetter_NotInvisible()
    {
        var report = UnicodeTool.Inspect("A");
        Assert.False(report.Chars[0].Invisible);
        Assert.Equal(UnicodeCategory.UppercaseLetter, report.Chars[0].Category);
    }

    [Fact]
    public void Inspect_Utf8ByteCount_MatchesEncoding()
    {
        const string text = "héllo";
        var report = UnicodeTool.Inspect(text);
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(text), report.Utf8Bytes);
    }
}
