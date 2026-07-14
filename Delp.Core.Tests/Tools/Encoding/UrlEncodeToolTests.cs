using Delp.Core.Tools.Encoding;

namespace Delp.Core.Tests.Tools.Encoding;

public class UrlEncodeToolTests
{
    [Theory]
    [InlineData(UrlEncodeMode.Component)]
    [InlineData(UrlEncodeMode.FormData)]
    [InlineData(UrlEncodeMode.PreserveUriChars)]
    public void RoundTrip_PerMode(UrlEncodeMode mode)
    {
        const string text = "hello world/path?a=1&b=2";
        Assert.Equal(text, UrlEncodeTool.Decode(UrlEncodeTool.Encode(text, mode), mode));
    }

    [Fact]
    public void Encode_Component_EncodesSpaceAsPercent20()
    {
        Assert.Equal("a%20b", UrlEncodeTool.Encode("a b", UrlEncodeMode.Component));
    }

    [Fact]
    public void Encode_FormData_EncodesSpaceAsPlus()
    {
        Assert.Equal("a+b", UrlEncodeTool.Encode("a b", UrlEncodeMode.FormData));
    }

    [Fact]
    public void Encode_PreserveUriChars_KeepsReservedCharsUnescaped()
    {
        const string reserved = ":/?#[]@!$&'()*+,;=";
        Assert.Equal(reserved, UrlEncodeTool.Encode(reserved, UrlEncodeMode.PreserveUriChars));
    }

    [Fact]
    public void Encode_Component_EscapesReservedChars()
    {
        var encoded = UrlEncodeTool.Encode(":/?#", UrlEncodeMode.Component);
        Assert.DoesNotContain(':', encoded);
        Assert.DoesNotContain('/', encoded);
        Assert.DoesNotContain('?', encoded);
        Assert.DoesNotContain('#', encoded);
    }

    [Fact]
    public void Decode_MalformedPercentSequence_ThrowsWithPosition()
    {
        var ex = Assert.Throws<FormatException>(() => UrlEncodeTool.Decode("abcdefghijkl%q1", UrlEncodeMode.Component));
        Assert.Contains("%q1", ex.Message);
        Assert.Contains("12", ex.Message);
    }

    [Fact]
    public void Decode_TruncatedPercentSequence_Throws()
    {
        Assert.Throws<FormatException>(() => UrlEncodeTool.Decode("100%", UrlEncodeMode.Component));
    }

    [Fact]
    public void RoundTrip_Emoji()
    {
        const string text = "hello 🚀 world";
        Assert.Equal(text, UrlEncodeTool.Decode(UrlEncodeTool.Encode(text, UrlEncodeMode.Component), UrlEncodeMode.Component));
    }

    [Fact]
    public void EmptyInput_RoundTripsToEmpty()
    {
        Assert.Equal("", UrlEncodeTool.Encode("", UrlEncodeMode.Component));
        Assert.Equal("", UrlEncodeTool.Decode("", UrlEncodeMode.Component));
    }
}
