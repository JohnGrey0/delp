using Delp.Core.Tools.Encoding;

namespace Delp.Core.Tests.Tools.Encoding;

public class BytesToolTests
{
    [Fact]
    public void Hi_ConvertsToKnownHexBinaryAndDecimal()
    {
        var bytes = BytesTool.FromText("Hi");

        Assert.Equal("48 69", BytesTool.ToHex(bytes, spaced: true, uppercase: false));
        Assert.Equal("01001000 01101001", BytesTool.ToBinary(bytes, spaced: true));
        Assert.Equal("72 105", BytesTool.ToDecimalBytes(bytes));
    }

    [Fact]
    public void FromHex_RoundTripsBackToText()
    {
        var bytes = BytesTool.FromHex("48 69");
        Assert.Equal("Hi", BytesTool.ToText(bytes));
    }

    [Fact]
    public void FromBinary_RoundTripsBackToText()
    {
        var bytes = BytesTool.FromBinary("01001000 01101001");
        Assert.Equal("Hi", BytesTool.ToText(bytes));
    }

    [Fact]
    public void FromDecimalBytes_RoundTripsBackToText()
    {
        var bytes = BytesTool.FromDecimalBytes("72,105");
        Assert.Equal("Hi", BytesTool.ToText(bytes));
    }

    [Fact]
    public void FromHex_StripsPrefixesAndCommas()
    {
        var bytes = BytesTool.FromHex("0x48, 0x69");
        Assert.Equal("Hi", BytesTool.ToText(bytes));
    }

    [Fact]
    public void FromHex_OddLength_Throws()
    {
        Assert.Throws<FormatException>(() => BytesTool.FromHex("486"));
    }

    [Fact]
    public void FromHex_InvalidCharacter_Throws()
    {
        Assert.Throws<FormatException>(() => BytesTool.FromHex("4G"));
    }

    [Fact]
    public void FromBinary_InvalidLength_Throws()
    {
        Assert.Throws<FormatException>(() => BytesTool.FromBinary("0100100"));
    }

    [Fact]
    public void FromBinary_InvalidCharacter_Throws()
    {
        Assert.Throws<FormatException>(() => BytesTool.FromBinary("01001002"));
    }

    [Fact]
    public void FromDecimalBytes_OutOfRange_Throws()
    {
        Assert.Throws<FormatException>(() => BytesTool.FromDecimalBytes("256"));
        Assert.Throws<FormatException>(() => BytesTool.FromDecimalBytes("-1"));
    }

    [Fact]
    public void ToHex_UppercaseOption()
    {
        var bytes = new byte[] { 0xAB, 0xCD };
        Assert.Equal("ABCD", BytesTool.ToHex(bytes, spaced: false, uppercase: true));
        Assert.Equal("abcd", BytesTool.ToHex(bytes, spaced: false, uppercase: false));
    }

    [Fact]
    public void RoundTrip_UnicodeEmoji()
    {
        const string text = "café 🚀";
        var bytes = BytesTool.FromText(text);
        Assert.Equal(text, BytesTool.ToText(bytes));
        Assert.Equal(text, BytesTool.ToText(BytesTool.FromHex(BytesTool.ToHex(bytes, true, false))));
    }
}
