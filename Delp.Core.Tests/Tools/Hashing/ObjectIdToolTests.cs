using Delp.Core.Tools.Hashing;

namespace Delp.Core.Tests.Tools.Hashing;

public class ObjectIdToolTests
{
    [Fact]
    public void Generate_Produces24LowercaseHexChars()
    {
        var id = ObjectIdTool.Generate();
        Assert.Equal(24, id.Length);
        Assert.All(id, c => Assert.True(Uri.IsHexDigit(c) && (!char.IsLetter(c) || char.IsLower(c))));
    }

    [Fact]
    public void Generate_ThenDecode_RoundTripsTimestamp()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var id = ObjectIdTool.Generate();
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        var decoded = ObjectIdTool.Decode(id);
        Assert.InRange(decoded.Timestamp, before, after);
        Assert.Equal(10, decoded.ProcessRandomHex.Length);
    }

    [Fact]
    public void GenerateBatch_CounterIncrementsAcrossIds()
    {
        var ids = ObjectIdTool.GenerateBatch(10);
        var counters = ids.Select(id => ObjectIdTool.Decode(id).Counter).ToList();

        Assert.Equal(10, counters.Distinct().Count());
        for (var i = 1; i < counters.Count; i++)
        {
            var diff = (counters[i] - counters[i - 1] + 0x1000000) % 0x1000000;
            Assert.Equal(1, diff);
        }
    }

    [Fact]
    public void GenerateBatch_SameProcessRandomAcrossIds()
    {
        var ids = ObjectIdTool.GenerateBatch(5);
        var randoms = ids.Select(id => ObjectIdTool.Decode(id).ProcessRandomHex).Distinct().ToList();
        Assert.Single(randoms);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void GenerateBatch_OutOfRangeCount_Throws(int count)
    {
        Assert.Throws<ArgumentException>(() => ObjectIdTool.GenerateBatch(count));
    }

    [Fact]
    public void Decode_KnownObjectId_MatchesExpectedTimestamp()
    {
        // 507f1f77bcf86cd799439011 is the canonical example ObjectId used throughout MongoDB's
        // own docs. Its first 4 bytes (507f1f77) are the Unix-second timestamp.
        var decoded = ObjectIdTool.Decode("507f1f77bcf86cd799439011");
        var expectedSeconds = Convert.ToInt64("507f1f77", 16);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(expectedSeconds), decoded.Timestamp);
        Assert.Equal("bcf86c", decoded.ProcessRandomHex[..6]);
    }

    [Theory]
    [InlineData("too-short")]
    [InlineData("507f1f77bcf86cd79943901")] // 23 chars
    [InlineData("507f1f77bcf86cd7994390111")] // 25 chars
    [InlineData("zzzf1f77bcf86cd799439011")] // non-hex
    public void Decode_InvalidInput_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => ObjectIdTool.Decode(input));
    }
}
