using System.Numerics;
using Delp.Core.Tools.TextProcessing;

namespace Delp.Core.Tests.Tools.TextProcessing;

public class BaseToolTests
{
    [Theory]
    [InlineData("42", 10, "42")]
    [InlineData("0x2A", null, "42")]
    [InlineData("0b101010", null, "42")]
    [InlineData("0o52", null, "42")]
    [InlineData("1010", 2, "10")] // parse binary "1010" -> 10 -> decimal "10"
    public void Parse_KnownValues_MatchDecimal(string input, int? baseHint, string expectedDecimal)
    {
        var value = BaseTool.Parse(input, baseHint);
        Assert.Equal(expectedDecimal, BaseTool.ToBase(value, 10));
    }

    [Fact]
    public void CrossConversion_255_MatchesAllBases()
    {
        var value = BaseTool.Parse("255", 10);
        Assert.Equal("11111111", BaseTool.ToBase(value, 2));
        Assert.Equal("377", BaseTool.ToBase(value, 8));
        Assert.Equal("ff", BaseTool.ToBase(value, 16));
        Assert.Equal("FF", BaseTool.ToBase(value, 16, uppercase: true));
    }

    [Fact]
    public void Parse_UnderscoreAndSpaceSeparators_AreIgnored()
    {
        Assert.Equal(BaseTool.Parse("1234", 10), BaseTool.Parse("1_234", 10));
        Assert.Equal(BaseTool.Parse("1234", 10), BaseTool.Parse("1 234", 10));
    }

    [Fact]
    public void Parse_NegativeNumber_PreservesSign()
    {
        var value = BaseTool.Parse("-2A", 16);
        Assert.Equal(new BigInteger(-42), value);
        Assert.Equal("-101010", BaseTool.ToBase(value, 2));
    }

    [Fact]
    public void ToBase_GroupsDigitsFromTheRight()
    {
        Assert.Equal("1_2345_6789", BaseTool.ToBase(new BigInteger(123456789), 10, groupSize: 4));
        Assert.Equal("1010_1010", BaseTool.ToBase(new BigInteger(170), 2, groupSize: 4));
    }

    [Fact]
    public void Parse_InvalidDigitForRadix_ThrowsWithDigitAndPosition()
    {
        var ex = Assert.Throws<FormatException>(() => BaseTool.Parse("12a9", 10));
        Assert.Contains("'a'", ex.Message);
        Assert.Contains("position 2", ex.Message);
    }

    [Fact]
    public void Parse_EmptyInput_Throws()
    {
        Assert.Throws<FormatException>(() => BaseTool.Parse("", 10));
    }

    [Fact]
    public void ToBase_Radix36_UsesFullAlphabet()
    {
        Assert.Equal("z", BaseTool.ToBase(new BigInteger(35), 36));
        Assert.Equal("10", BaseTool.ToBase(new BigInteger(36), 36));
    }

    [Fact]
    public void RoundTrip_Big256BitValue()
    {
        var big = BigInteger.Pow(2, 256) - 1;
        var hex = BaseTool.ToBase(big, 16);
        Assert.Equal(big, BaseTool.Parse(hex, 16));
        Assert.Equal(256, BaseTool.Measure(big).BitLength);
        Assert.Equal(32, BaseTool.Measure(big).ByteCount);
    }

    [Fact]
    public void ToBase_InvalidRadix_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BaseTool.ToBase(BigInteger.One, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BaseTool.ToBase(BigInteger.One, 37));
    }

    [Fact]
    public void Measure_Zero_IsZeroBitsOneByteOrLess()
    {
        var (bits, bytes) = BaseTool.Measure(BigInteger.Zero);
        Assert.Equal(0, bits);
        Assert.Equal(0, bytes);
    }
}
