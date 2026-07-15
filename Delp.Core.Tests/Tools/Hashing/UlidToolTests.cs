using Delp.Core.Tools.Hashing;

namespace Delp.Core.Tests.Tools.Hashing;

public class UlidToolTests
{
    [Fact]
    public void Decode_SpecVector_ExtractsKnownTimestamp()
    {
        // Canonical example from the ULID spec (https://github.com/ulid/spec):
        // 01AN4Z07BY79KA1307SR9X4MV3 -> 2016-06-13T13:25:20.894Z.
        var (ms, randomness) = UlidTool.Decode("01AN4Z07BY79KA1307SR9X4MV3");
        Assert.Equal(1465824320894UL, ms);
        Assert.Equal(10, randomness.Length);

        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)ms);
        Assert.Equal(new DateTimeOffset(2016, 6, 13, 13, 25, 20, 894, TimeSpan.Zero), timestamp);
    }

    [Fact]
    public void Generate_ProducesA26CharCrockfordString()
    {
        var id = UlidTool.Generate();
        Assert.Equal(26, id.Length);
        Assert.All(id, c => Assert.Contains(c, UlidTool.Alphabet));
    }

    [Fact]
    public void GenerateBatch_1000Ids_AreUniqueAndLexicallyMonotonic()
    {
        var ids = UlidTool.GenerateBatch(1000);
        Assert.Equal(1000, ids.Count);
        Assert.Equal(1000, ids.Distinct().Count());

        for (var i = 1; i < ids.Count; i++)
            Assert.True(string.CompareOrdinal(ids[i - 1], ids[i]) < 0, $"'{ids[i - 1]}' should sort before '{ids[i]}'.");
    }

    [Fact]
    public void GenerateBatch_RoundTripsThroughDecode()
    {
        var ids = UlidTool.GenerateBatch(5);
        foreach (var id in ids)
        {
            var (ms, randomness) = UlidTool.Decode(id);
            Assert.Equal(10, randomness.Length);
            Assert.True(ms > 0);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void GenerateBatch_OutOfRangeCount_Throws(int count)
    {
        Assert.Throws<ArgumentException>(() => UlidTool.GenerateBatch(count));
    }

    [Fact]
    public void IsWellFormed_ValidUlid_ReturnsTrue()
    {
        Assert.True(UlidTool.IsWellFormed("01AN4Z07BY79KA1307SR9X4MV3"));
    }

    [Theory]
    [InlineData("too-short")]
    [InlineData("01AN4Z07BY79KA1307SR9X4MV3XX")] // too long
    public void IsWellFormed_InvalidInputs_ReturnsFalse(string text)
    {
        Assert.False(UlidTool.IsWellFormed(text));
    }

    [Fact]
    public void IsWellFormed_RightLengthButExcludedLetters_ReturnsFalse()
    {
        // 'I' (and L, O, U) are excluded from the Crockford alphabet even though the length is right.
        Assert.False(UlidTool.IsWellFormed(new string('I', 26)));
    }

    [Fact]
    public void Decode_LowercaseInput_IsAcceptedCaseInsensitively()
    {
        var (upperMs, _) = UlidTool.Decode("01AN4Z07BY79KA1307SR9X4MV3");
        var (lowerMs, _) = UlidTool.Decode("01an4z07by79ka1307sr9x4mv3");
        Assert.Equal(upperMs, lowerMs);
    }

    [Fact]
    public void Decode_WrongLength_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => UlidTool.Decode("TOOSHORT"));
    }

    [Fact]
    public void Decode_InvalidCharacter_ThrowsFormatException()
    {
        // 'I' is excluded from the Crockford alphabet.
        Assert.Throws<FormatException>(() => UlidTool.Decode("IIAN4Z07BY79KA1307SR9X4MV3"));
    }
}
