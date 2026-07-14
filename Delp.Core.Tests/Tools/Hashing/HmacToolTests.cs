using Delp.Core.Tools.Hashing;

namespace Delp.Core.Tests.Tools.Hashing;

public class HmacToolTests
{
    // RFC 4231 test case 2.
    private const string Rfc4231Case2Key = "Jefe";
    private const string Rfc4231Case2Message = "what do ya want for nothing?";
    private const string Rfc4231Case2Sha256Hex = "5bdcc146bf60754e6a042426089575c75a003f089d2739839dec58b964ec3843";

    [Fact]
    public void Compute_Rfc4231TestCase2_Sha256_MatchesKnownDigest()
    {
        var key = System.Text.Encoding.UTF8.GetBytes(Rfc4231Case2Key);
        var message = System.Text.Encoding.UTF8.GetBytes(Rfc4231Case2Message);

        var hash = HmacTool.Compute("SHA256", key, message);

        Assert.Equal(Rfc4231Case2Sha256Hex, Convert.ToHexString(hash).ToLowerInvariant());
    }

    [Fact]
    public void ParseInput_Hex_ProducesSameKeyAsUtf8_ForAsciiText()
    {
        var utf8Key = HmacTool.ParseInput("Jefe", InputInterpretation.Utf8);
        var hexKey = HmacTool.ParseInput("4a656665", InputInterpretation.Hex);

        Assert.Equal(utf8Key, hexKey);

        var message = System.Text.Encoding.UTF8.GetBytes(Rfc4231Case2Message);
        var hashViaHexKey = HmacTool.Compute("SHA256", hexKey, message);
        Assert.Equal(Rfc4231Case2Sha256Hex, Convert.ToHexString(hashViaHexKey).ToLowerInvariant());
    }

    [Fact]
    public void ParseInput_Hex_ToleratesWhitespaceAnd0xPrefix()
    {
        var a = HmacTool.ParseInput("0x4a 65 66 65", InputInterpretation.Hex);
        var b = HmacTool.ParseInput("4a656665", InputInterpretation.Hex);
        Assert.Equal(b, a);
    }

    [Fact]
    public void ParseInput_Hex_OddLength_Throws()
    {
        Assert.Throws<FormatException>(() => HmacTool.ParseInput("abc", InputInterpretation.Hex));
    }

    [Fact]
    public void ParseInput_Base64_TolerantOfMissingPadding()
    {
        var withPadding = HmacTool.ParseInput("SmVmZQ==", InputInterpretation.Base64);
        var withoutPadding = HmacTool.ParseInput("SmVmZQ", InputInterpretation.Base64);

        Assert.Equal(withPadding, withoutPadding);
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes("Jefe"), withPadding);
    }

    [Fact]
    public void ParseInput_InvalidBase64_Throws()
    {
        Assert.Throws<FormatException>(() => HmacTool.ParseInput("not-base64!!", InputInterpretation.Base64));
    }

    [Fact]
    public void Compute_OutputAsBase64_MatchesConvertedHex()
    {
        var key = System.Text.Encoding.UTF8.GetBytes(Rfc4231Case2Key);
        var message = System.Text.Encoding.UTF8.GetBytes(Rfc4231Case2Message);
        var hash = HmacTool.Compute("SHA256", key, message);

        var expectedBytes = Convert.FromHexString(Rfc4231Case2Sha256Hex);
        Assert.Equal(Convert.ToBase64String(expectedBytes), Convert.ToBase64String(hash));
    }

    [Fact]
    public void Compute_EmptyKey_Allowed()
    {
        var hash = HmacTool.Compute("SHA256", [], System.Text.Encoding.UTF8.GetBytes("data"));
        Assert.Equal(32, hash.Length);
    }

    [Fact]
    public void Compute_UnknownAlgorithm_Throws()
    {
        Assert.Throws<ArgumentException>(() => HmacTool.Compute("SHA3-999", [1], [2]));
    }
}
