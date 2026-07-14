using Delp.Core.Tools.Encoding;

namespace Delp.Core.Tests.Tools.Encoding;

public class Base64ToolTests
{
    [Fact]
    public void Encode_ProducesStandardBase64()
    {
        Assert.Equal("aGVsbG8gd29ybGQ=", Base64Tool.Encode("hello world"));
    }

    [Fact]
    public void RoundTrip_PreservesUnicode()
    {
        const string text = "héllo 世界 🚀";
        Assert.Equal(text, Base64Tool.Decode(Base64Tool.Encode(text)));
    }

    [Fact]
    public void Encode_UrlSafe_UsesUrlAlphabetWithoutPadding()
    {
        var encoded = Base64Tool.Encode("subjects?_d=1", urlSafe: true);
        Assert.DoesNotContain('+', encoded);
        Assert.DoesNotContain('/', encoded);
        Assert.DoesNotContain('=', encoded);
        Assert.Equal("subjects?_d=1", Base64Tool.Decode(encoded, urlSafe: true));
    }

    [Fact]
    public void Decode_ToleratesMissingPadding()
    {
        Assert.Equal("hello world", Base64Tool.Decode("aGVsbG8gd29ybGQ"));
    }

    [Fact]
    public void Decode_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal("", Base64Tool.Decode("   "));
    }

    [Fact]
    public void Decode_InvalidInput_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => Base64Tool.Decode("not!!valid##"));
    }
}
