using Delp.Core.Tools.Hashing;

namespace Delp.Core.Tests.Tools.Hashing;

public class RandomStringToolTests
{
    [Fact]
    public void Generate_RespectsLength()
    {
        var s = RandomStringTool.Generate(new RandomStringOptions(32, true, true, true, false));
        Assert.Equal(32, s.Length);
    }

    [Fact]
    public void Generate_OnlyUsesSelectedCharacterSet()
    {
        var s = RandomStringTool.Generate(new RandomStringOptions(500, true, false, false, false));
        Assert.All(s, c => Assert.True(c is >= 'a' and <= 'z'));
    }

    [Fact]
    public void Generate_DigitsOnly_ProducesOnlyDigits()
    {
        var s = RandomStringTool.Generate(new RandomStringOptions(200, false, false, true, false));
        Assert.All(s, c => Assert.True(char.IsAsciiDigit(c)));
    }

    [Fact]
    public void BuildAlphabet_ExcludeAmbiguous_RemovesAmbiguousChars()
    {
        var options = new RandomStringOptions(0, true, true, true, false, ExcludeAmbiguous: true);
        var alphabet = RandomStringTool.BuildAlphabet(options);
        foreach (var c in "Il1O0o")
            Assert.DoesNotContain(c, alphabet);
    }

    [Fact]
    public void Generate_ExcludeAmbiguous_NeverProducesAmbiguousChars()
    {
        var options = new RandomStringOptions(500, true, true, true, false, ExcludeAmbiguous: true);
        var s = RandomStringTool.Generate(options);
        foreach (var c in "Il1O0o")
            Assert.DoesNotContain(c, s);
    }

    [Fact]
    public void Generate_CustomAlphabet_UsesOnlyCustomChars()
    {
        var options = new RandomStringOptions(100, false, false, false, false, Custom: "XY");
        var s = RandomStringTool.Generate(options);
        Assert.All(s, c => Assert.True(c is 'X' or 'Y'));
    }

    [Fact]
    public void EntropyBits_MatchesLengthTimesLog2AlphabetSize()
    {
        var options = new RandomStringOptions(10, false, false, true, false); // 10 digits alphabet
        var bits = RandomStringTool.EntropyBits(options);
        Assert.Equal(10 * Math.Log2(10), bits, precision: 6);
    }

    [Fact]
    public void EntropyBits_EmptyAlphabet_IsZero()
    {
        var options = new RandomStringOptions(10, false, false, false, false);
        Assert.Equal(0, RandomStringTool.EntropyBits(options));
    }

    [Fact]
    public void Generate_NoCharsetSelected_Throws()
    {
        var options = new RandomStringOptions(10, false, false, false, false);
        Assert.Throws<ArgumentException>(() => RandomStringTool.Generate(options));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Generate_NonPositiveLength_Throws(int length)
    {
        var options = new RandomStringOptions(length, true, false, false, false);
        Assert.Throws<ArgumentException>(() => RandomStringTool.Generate(options));
    }
}
