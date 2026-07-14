using Delp.Core.Tools.Hashing;

namespace Delp.Core.Tests.Tools.Hashing;

public class NanoIdToolTests
{
    [Fact]
    public void Generate_Default_Has21CharsFromDefaultAlphabet()
    {
        var id = NanoIdTool.Generate();
        Assert.Equal(21, id.Length);
        Assert.All(id, c => Assert.Contains(c, NanoIdTool.DefaultAlphabet));
    }

    [Fact]
    public void Generate_CustomSize_RespectsSize()
    {
        var id = NanoIdTool.Generate(size: 10);
        Assert.Equal(10, id.Length);
    }

    [Fact]
    public void Generate_CustomAlphabet_UsesOnlyThoseChars()
    {
        var id = NanoIdTool.Generate(size: 100, alphabet: "ABC");
        Assert.All(id, c => Assert.True(c is 'A' or 'B' or 'C'));
    }

    [Fact]
    public void Generate_SingleCharAlphabet_RepeatsThatChar()
    {
        var id = NanoIdTool.Generate(size: 5, alphabet: "Z");
        Assert.Equal("ZZZZZ", id);
    }

    [Fact]
    public void Generate_1000Ids_AreUniqueSanityCheck()
    {
        var ids = Enumerable.Range(0, 1000).Select(_ => NanoIdTool.Generate()).ToList();
        Assert.Equal(1000, ids.Distinct().Count());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Generate_NonPositiveSize_Throws(int size)
    {
        Assert.Throws<ArgumentException>(() => NanoIdTool.Generate(size));
    }

    [Fact]
    public void Generate_AlphabetOver256Chars_Throws()
    {
        var alphabet = new string('a', 257);
        Assert.Throws<ArgumentException>(() => NanoIdTool.Generate(10, alphabet));
    }

    [Fact]
    public void Generate_EmptyAlphabet_Throws()
    {
        Assert.Throws<ArgumentException>(() => NanoIdTool.Generate(10, ""));
    }

    [Fact]
    public void YearsFor1PercentCollision_GrowsWithSize()
    {
        var small = NanoIdTool.YearsFor1PercentCollision(size: 5, alphabetLength: 64, idsPerHour: 1000);
        var large = NanoIdTool.YearsFor1PercentCollision(size: 21, alphabetLength: 64, idsPerHour: 1000);
        Assert.True(large > small);
        Assert.True(double.IsFinite(large));
    }
}
