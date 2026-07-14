using Delp.Core.Tools.Encoding;

namespace Delp.Core.Tests.Tools.Encoding;

public class HtmlEntityToolTests
{
    [Fact]
    public void RoundTrip_BasicMarkup()
    {
        const string text = "<div class=\"a\">";
        var encoded = HtmlEntityTool.Encode(text, nonAsciiToNumeric: false);
        Assert.Equal(text, HtmlEntityTool.Decode(encoded));
    }

    [Fact]
    public void Decode_NamedEntities()
    {
        Assert.Equal(" ", HtmlEntityTool.Decode("&nbsp;"));
        Assert.Equal("—", HtmlEntityTool.Decode("&mdash;"));
    }

    [Fact]
    public void Encode_DoubleEncodingIsVisible()
    {
        Assert.Equal("&amp;amp;", HtmlEntityTool.Encode("&amp;", nonAsciiToNumeric: false));
    }

    [Fact]
    public void Encode_NonAsciiNumeric_EncodesAstralCharAsSingleEntity()
    {
        var encoded = HtmlEntityTool.Encode("😀", nonAsciiToNumeric: true);
        Assert.Equal("&#x1F600;", encoded);
    }

    [Fact]
    public void Encode_NonAsciiNumeric_LeavesAsciiAlone()
    {
        Assert.Equal("abc", HtmlEntityTool.Encode("abc", nonAsciiToNumeric: true));
    }

    [Fact]
    public void Decode_UnknownEntity_PassesThroughUnchanged()
    {
        Assert.Equal("&foo;", HtmlEntityTool.Decode("&foo;"));
    }

    [Fact]
    public void Encode_WithoutNonAsciiFlag_UsesWebUtilityDefaultDecimalEntity()
    {
        // WebUtility.HtmlEncode itself numeric-encodes non-ASCII characters; the nonAsciiToNumeric
        // flag only controls decimal vs. hex formatting, not whether encoding happens at all.
        Assert.Equal("h&#233;llo", HtmlEntityTool.Encode("héllo", nonAsciiToNumeric: false));
    }

    [Fact]
    public void Encode_WithNonAsciiFlag_UsesHexEntityInstead()
    {
        Assert.Equal("h&#xE9;llo", HtmlEntityTool.Encode("héllo", nonAsciiToNumeric: true));
    }
}
