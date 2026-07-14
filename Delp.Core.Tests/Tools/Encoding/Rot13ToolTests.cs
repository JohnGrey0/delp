using Delp.Core.Tools.Encoding;

namespace Delp.Core.Tests.Tools.Encoding;

public class Rot13ToolTests
{
    [Fact]
    public void Rot13_IsItsOwnInverse()
    {
        const string text = "The Quick Brown Fox";
        Assert.Equal(text, Rot13Tool.Shift(Rot13Tool.Shift(text, 13), 13));
    }

    [Fact]
    public void Shift_AbcByThree_ProducesDef()
    {
        Assert.Equal("def", Rot13Tool.Shift("abc", 3));
    }

    [Fact]
    public void Shift_NegativeAmount_WrapsCorrectly()
    {
        Assert.Equal(Rot13Tool.Shift("hello", 13), Rot13Tool.Shift("hello", -13));
    }

    [Fact]
    public void Shift_DigitsAndPunctuation_Untouched()
    {
        Assert.Equal("123 !@# xyz", Rot13Tool.Shift("123 !@# klm", 13));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(26)]
    public void Shift_ZeroOrTwentySix_IsIdentity(int n)
    {
        const string text = "Hello, World!";
        Assert.Equal(text, Rot13Tool.Shift(text, n));
    }

    [Fact]
    public void Shift_PreservesCase()
    {
        Assert.Equal("Nop", Rot13Tool.Shift("Abc", 13));
    }
}
