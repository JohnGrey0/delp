using Delp.Core.Tools.Hashing;

namespace Delp.Core.Tests.Tools.Hashing;

public class SnowflakeToolTests
{
    [Fact]
    public void Decode_KnownDiscordId_MatchesPublishedFields()
    {
        // Well-known example from Discord's developer docs (Snowflake reference page):
        // 175928847299117063 -> 2016-04-30T11:18:25.796Z, worker 1, process 0, sequence 7.
        var decoded = SnowflakeTool.Decode(175928847299117063L, SnowflakeTool.DiscordEpochMs);

        Assert.Equal(new DateTimeOffset(2016, 4, 30, 11, 18, 25, 796, TimeSpan.Zero), decoded.Timestamp);
        Assert.Equal(1, decoded.WorkerId);
        Assert.Equal(0, decoded.ProcessId);
        Assert.Equal(7, decoded.Sequence);
    }

    [Fact]
    public void Generate_ThenDecode_RoundTripsFields()
    {
        var id = SnowflakeTool.Generate(SnowflakeTool.TwitterEpochMs, workerId: 5, processId: 12);
        var decoded = SnowflakeTool.Decode(id, SnowflakeTool.TwitterEpochMs);

        Assert.Equal(5, decoded.WorkerId);
        Assert.Equal(12, decoded.ProcessId);
        Assert.True(decoded.Timestamp <= DateTimeOffset.UtcNow.AddSeconds(1));
        Assert.True(decoded.Timestamp >= DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void GenerateBatch_ProducesStrictlyIncreasingIds()
    {
        var ids = SnowflakeTool.GenerateBatch(50, SnowflakeTool.DiscordEpochMs, workerId: 1, processId: 1);
        Assert.Equal(50, ids.Count);
        for (var i = 1; i < ids.Count; i++)
            Assert.True(ids[i] > ids[i - 1]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(32)]
    public void Generate_WorkerIdOutOfRange_Throws(int workerId)
    {
        Assert.Throws<ArgumentException>(() => SnowflakeTool.Generate(SnowflakeTool.DiscordEpochMs, workerId, 0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(32)]
    public void Generate_ProcessIdOutOfRange_Throws(int processId)
    {
        Assert.Throws<ArgumentException>(() => SnowflakeTool.Generate(SnowflakeTool.DiscordEpochMs, 0, processId));
    }

    [Fact]
    public void Decode_NegativeId_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => SnowflakeTool.Decode(-1, SnowflakeTool.DiscordEpochMs));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void GenerateBatch_OutOfRangeCount_Throws(int count)
    {
        Assert.Throws<ArgumentException>(() => SnowflakeTool.GenerateBatch(count, SnowflakeTool.DiscordEpochMs, 0, 0));
    }

    [Fact]
    public void Decode_TwitterAndDiscordEpochs_GiveDifferentTimestampsForSameId()
    {
        const long id = 1000000000000000L;
        var viaTwitter = SnowflakeTool.Decode(id, SnowflakeTool.TwitterEpochMs);
        var viaDiscord = SnowflakeTool.Decode(id, SnowflakeTool.DiscordEpochMs);
        Assert.NotEqual(viaTwitter.Timestamp, viaDiscord.Timestamp);
    }
}
