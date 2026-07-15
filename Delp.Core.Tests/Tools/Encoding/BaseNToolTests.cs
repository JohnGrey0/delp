using Delp.Core.Tools.Encoding;

namespace Delp.Core.Tests.Tools.Encoding;

public class BaseNToolTests
{
    // ---------------------------------------------------------------- Base32 (RFC 4648 vectors)
    // https://www.rfc-editor.org/rfc/rfc4648#section-10

    [Theory]
    [InlineData("", "")]
    [InlineData("f", "MY======")]
    [InlineData("fo", "MZXQ====")]
    [InlineData("foo", "MZXW6===")]
    [InlineData("foob", "MZXW6YQ=")]
    [InlineData("fooba", "MZXW6YTB")]
    [InlineData("foobar", "MZXW6YTBOI======")]
    public void Base32_Rfc4648TestVectors(string plain, string expected)
    {
        Assert.Equal(expected, BaseNTool.Encode(plain, BaseNAlphabet.Base32));
        Assert.Equal(plain, BaseNTool.Decode(expected, BaseNAlphabet.Base32));
    }

    [Fact]
    public void Base32_Decode_IsCaseAndPaddingTolerant()
    {
        Assert.Equal("foobar", BaseNTool.Decode("mzxw6ytboi======", BaseNAlphabet.Base32));
        Assert.Equal("foobar", BaseNTool.Decode("MZXW6YTBOI", BaseNAlphabet.Base32));
        Assert.Equal("foobar", BaseNTool.Decode("mzxw6ytboi", BaseNAlphabet.Base32));
    }

    [Fact]
    public void Base32_Decode_InvalidCharacter_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => BaseNTool.Decode("1nvalid!!", BaseNAlphabet.Base32));
    }

    [Fact]
    public void Base32_RoundTrips_Unicode()
    {
        const string text = "héllo 世界 🚀";
        Assert.Equal(text, BaseNTool.Decode(BaseNTool.Encode(text, BaseNAlphabet.Base32), BaseNAlphabet.Base32));
    }

    // ------------------------------------------------------------------------- Base32 Crockford

    [Fact]
    public void Crockford_Encode_UsesNoPadding()
    {
        var encoded = BaseNTool.Encode("foobar", BaseNAlphabet.Base32Crockford);
        Assert.DoesNotContain('=', encoded);
        Assert.Equal("foobar", BaseNTool.Decode(encoded, BaseNAlphabet.Base32Crockford));
    }

    [Theory]
    [InlineData('I', '1')]
    [InlineData('L', '1')]
    [InlineData('O', '0')]
    public void Crockford_Decode_AliasesConfusableCharacters(char alias, char canonical)
    {
        // "AB" encodes to "8510", which contains both a '1' and a '0' to alias.
        const string text = "AB";
        var canonicalEncoded = BaseNTool.Encode(text, BaseNAlphabet.Base32Crockford);
        Assert.Equal("8510", canonicalEncoded);

        var withAlias = canonicalEncoded.Replace(canonical, alias);
        Assert.NotEqual(canonicalEncoded, withAlias); // sanity: the substitution actually changed something
        Assert.Equal(text, BaseNTool.Decode(withAlias, BaseNAlphabet.Base32Crockford));
    }

    [Fact]
    public void Crockford_Decode_IsCaseInsensitive()
    {
        var encoded = BaseNTool.Encode("test data", BaseNAlphabet.Base32Crockford);
        Assert.Equal("test data", BaseNTool.Decode(encoded.ToLowerInvariant(), BaseNAlphabet.Base32Crockford));
    }

    [Fact]
    public void Crockford_Decode_InvalidCharacter_ThrowsFormatException()
    {
        // 'U' is deliberately excluded from Crockford's alphabet (not an alias target).
        Assert.Throws<FormatException>(() => BaseNTool.Decode("U", BaseNAlphabet.Base32Crockford));
    }

    [Fact]
    public void Crockford_RoundTrips_Unicode()
    {
        const string text = "héllo 世界 🚀";
        Assert.Equal(text, BaseNTool.Decode(BaseNTool.Encode(text, BaseNAlphabet.Base32Crockford), BaseNAlphabet.Base32Crockford));
    }

    // -------------------------------------------------------------------------- Base58 (Bitcoin)

    [Fact]
    public void Base58_LeadingZeroBytes_BecomeLeadingOnes()
    {
        var data = new byte[] { 0, 0, (byte)'a', (byte)'b', (byte)'c' };
        var encoded = BaseNTool.EncodeBytes(data, BaseNAlphabet.Base58);
        Assert.StartsWith("11", encoded);
        Assert.Equal(data, BaseNTool.DecodeBytes(encoded, BaseNAlphabet.Base58));
    }

    [Fact]
    public void Base58_AllZeroInput_EncodesToAllOnes()
    {
        var data = new byte[] { 0, 0, 0 };
        var encoded = BaseNTool.EncodeBytes(data, BaseNAlphabet.Base58);
        Assert.Equal("111", encoded);
        Assert.Equal(data, BaseNTool.DecodeBytes(encoded, BaseNAlphabet.Base58));
    }

    [Fact]
    public void Base58_KnownVector_HelloWorld()
    {
        // "hello world" (UTF-8) is a commonly cited Base58 vector.
        Assert.Equal("StV1DL6CwTryKyV", BaseNTool.Encode("hello world", BaseNAlphabet.Base58));
        Assert.Equal("hello world", BaseNTool.Decode("StV1DL6CwTryKyV", BaseNAlphabet.Base58));
    }

    [Fact]
    public void Base58_Decode_InvalidCharacter_ThrowsFormatException()
    {
        // '0', 'O', 'I', 'l' are all excluded from the Base58 alphabet.
        Assert.Throws<FormatException>(() => BaseNTool.Decode("0OIl", BaseNAlphabet.Base58));
    }

    [Fact]
    public void Base58_RoundTrips_Unicode()
    {
        const string text = "héllo 世界 🚀";
        Assert.Equal(text, BaseNTool.Decode(BaseNTool.Encode(text, BaseNAlphabet.Base58), BaseNAlphabet.Base58));
    }

    // ---------------------------------------------------------------------------------- Ascii85

    [Fact]
    public void Ascii85_KnownVector_Man()
    {
        // The canonical Adobe Ascii85 example: "Man " -> "9jqo^".
        Assert.Equal("9jqo^", BaseNTool.Encode("Man ", BaseNAlphabet.Ascii85));
        Assert.Equal("Man ", BaseNTool.Decode("9jqo^", BaseNAlphabet.Ascii85));
    }

    [Fact]
    public void Ascii85_AllZeroGroup_UsesZShorthand()
    {
        var data = new byte[4];
        Assert.Equal("z", BaseNTool.EncodeBytes(data, BaseNAlphabet.Ascii85));
        Assert.Equal(data, BaseNTool.DecodeBytes("z", BaseNAlphabet.Ascii85));
    }

    [Fact]
    public void Ascii85_Decode_StripsAdobeWrapper()
    {
        var wrapped = "<~9jqo^~>";
        Assert.Equal("Man ", BaseNTool.Decode(wrapped, BaseNAlphabet.Ascii85));
    }

    [Fact]
    public void Ascii85_Decode_TruncatedFinalGroup_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => BaseNTool.Decode("9jqo^9", BaseNAlphabet.Ascii85));
    }

    [Fact]
    public void Ascii85_Decode_OverflowingGroup_ThrowsFormatException()
    {
        // "uuuuu" (all max digits) decodes to a value > uint.MaxValue.
        Assert.Throws<FormatException>(() => BaseNTool.Decode("uuuuu", BaseNAlphabet.Ascii85));
    }

    [Fact]
    public void Ascii85_Decode_InvalidCharacter_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => BaseNTool.Decode("bad", BaseNAlphabet.Ascii85));
    }

    [Fact]
    public void Ascii85_RoundTrips_Unicode()
    {
        const string text = "héllo 世界 🚀";
        Assert.Equal(text, BaseNTool.Decode(BaseNTool.Encode(text, BaseNAlphabet.Ascii85), BaseNAlphabet.Ascii85));
    }

    [Fact]
    public void Ascii85_RoundTrips_ArbitraryBinaryData()
    {
        var data = new byte[257];
        for (var i = 0; i < data.Length; i++)
            data[i] = (byte)(i * 37);

        var encoded = BaseNTool.EncodeBytes(data, BaseNAlphabet.Ascii85);
        Assert.Equal(data, BaseNTool.DecodeBytes(encoded, BaseNAlphabet.Ascii85));
    }
}
