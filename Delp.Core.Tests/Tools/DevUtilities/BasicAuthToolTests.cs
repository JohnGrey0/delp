using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class BasicAuthToolTests
{
    [Fact]
    public void Encode_RfcExample_MatchesKnownDigest()
    {
        var result = BasicAuthTool.Encode("Aladdin", "open sesame");
        Assert.Equal("QWxhZGRpbjpvcGVuIHNlc2FtZQ==", result.Base64);
        Assert.Equal("Authorization: Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==", result.Header);
        Assert.Equal("curl -H \"Authorization: Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==\"", result.CurlHeaderFlag);
        Assert.Equal("curl -u \"Aladdin:open sesame\"", result.CurlUserFlag);
    }

    [Fact]
    public void Encode_ColonInUsername_Throws()
    {
        Assert.Throws<FormatException>(() => BasicAuthTool.Encode("ala:din", "secret"));
    }

    [Fact]
    public void Decode_FullHeader_ReturnsCredentials()
    {
        var creds = BasicAuthTool.Decode("Authorization: Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==");
        Assert.Equal("Aladdin", creds.Username);
        Assert.Equal("open sesame", creds.Password);
    }

    [Fact]
    public void Decode_BasicPrefixOnly_ReturnsCredentials()
    {
        var creds = BasicAuthTool.Decode("Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==");
        Assert.Equal("Aladdin", creds.Username);
        Assert.Equal("open sesame", creds.Password);
    }

    [Fact]
    public void Decode_BareBase64_ReturnsCredentials()
    {
        var creds = BasicAuthTool.Decode("QWxhZGRpbjpvcGVuIHNlc2FtZQ==");
        Assert.Equal("Aladdin", creds.Username);
        Assert.Equal("open sesame", creds.Password);
    }

    [Fact]
    public void Decode_InvalidBase64_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => BasicAuthTool.Decode("not valid base64!!"));
    }

    [Fact]
    public void Decode_NoColonInDecoded_ThrowsFormatException()
    {
        // Base64 of "nocolonhere" — valid base64, but no ':' once decoded.
        var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("nocolonhere"));
        Assert.Throws<FormatException>(() => BasicAuthTool.Decode(b64));
    }

    [Fact]
    public void RoundTrip_UnicodePassword()
    {
        var encoded = BasicAuthTool.Encode("user", "pässwörd é世界");
        var creds = BasicAuthTool.Decode(encoded.Header);
        Assert.Equal("user", creds.Username);
        Assert.Equal("pässwörd é世界", creds.Password);
    }
}
