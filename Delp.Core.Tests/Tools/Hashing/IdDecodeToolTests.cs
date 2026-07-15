using System.Diagnostics;
using Delp.Core.Tools.Hashing;

namespace Delp.Core.Tests.Tools.Hashing;

public class IdDecodeToolTests
{
    [Fact]
    public void Decode_Ulid_DetectsKindAndTimestamp()
    {
        var result = IdDecodeTool.Decode("01AN4Z07BY79KA1307SR9X4MV3", SnowflakeTool.DiscordEpochMs);
        Assert.Equal(DetectedIdKind.Ulid, result.Kind);
        Assert.Equal("ULID", result.TypeLabel);
        Assert.Equal(new DateTimeOffset(2016, 6, 13, 13, 25, 20, 894, TimeSpan.Zero), result.Timestamp);
        Assert.Contains(result.Fields, f => f.Label == "Randomness");
    }

    [Fact]
    public void Decode_ObjectId_DetectsKindAndTimestamp()
    {
        var result = IdDecodeTool.Decode("507f1f77bcf86cd799439011", SnowflakeTool.DiscordEpochMs);
        Assert.Equal(DetectedIdKind.ObjectId, result.Kind);
        Assert.Equal("ObjectId", result.TypeLabel);
        Assert.NotNull(result.Timestamp);
    }

    [Fact]
    public void Decode_Snowflake_DetectsKindAndFields()
    {
        var result = IdDecodeTool.Decode("175928847299117063", SnowflakeTool.DiscordEpochMs);
        Assert.Equal(DetectedIdKind.Snowflake, result.Kind);
        Assert.Equal("Snowflake", result.TypeLabel);
        Assert.Equal(new DateTimeOffset(2016, 4, 30, 11, 18, 25, 796, TimeSpan.Zero), result.Timestamp);
        Assert.Contains(result.Fields, f => f.Label == "Worker id" && f.Value == "1");
        Assert.Contains(result.Fields, f => f.Label == "Process id" && f.Value == "0");
        Assert.Contains(result.Fields, f => f.Label == "Sequence" && f.Value == "7");
    }

    [Fact]
    public void Decode_UuidV7_DetectsKindAndTimestamp()
    {
        var guid = Guid.CreateVersion7();
        var result = IdDecodeTool.Decode(guid.ToString(), SnowflakeTool.DiscordEpochMs);
        Assert.Equal(DetectedIdKind.Uuid, result.Kind);
        Assert.Equal("UUID v7", result.TypeLabel);
        Assert.NotNull(result.Timestamp);
    }

    [Fact]
    public void Decode_UuidV4_DetectedButHasNoTimestamp()
    {
        var guid = Guid.NewGuid(); // v4
        var result = IdDecodeTool.Decode(guid.ToString(), SnowflakeTool.DiscordEpochMs);
        Assert.Equal(DetectedIdKind.Uuid, result.Kind);
        Assert.Equal("UUID v4", result.TypeLabel);
        Assert.Null(result.Timestamp);
        Assert.Contains(result.Fields, f => f.Label == "Note");
    }

    [Fact]
    public void Decode_26CharHexString_IsUlidNotObjectId()
    {
        // Detector ambiguity rule: a 26-character string made entirely of hex digits still has
        // ULID's length (26), never ObjectId's (24) — it must decode as a ULID.
        var hex26 = string.Concat(Enumerable.Repeat("0123456789ABCDEF", 2))[..26];
        Assert.Equal(26, hex26.Length);
        var result = IdDecodeTool.Decode(hex26, SnowflakeTool.DiscordEpochMs);
        Assert.Equal(DetectedIdKind.Ulid, result.Kind);
    }

    [Fact]
    public void Decode_EmptyInput_Throws()
    {
        Assert.Throws<FormatException>(() => IdDecodeTool.Decode("   ", SnowflakeTool.DiscordEpochMs));
    }

    [Fact]
    public void Decode_UnrecognizedFormat_Throws()
    {
        Assert.Throws<FormatException>(() => IdDecodeTool.Decode("not-an-id-at-all!!", SnowflakeTool.DiscordEpochMs));
    }

    [Fact]
    public void Decode_24CharHexString_IsObjectIdNotUlid()
    {
        // Detector ambiguity rule (the mirror image of the 26-char case below): a 24-character
        // string made entirely of Crockford-safe hex digits has ObjectId's length (24), not
        // ULID's (26) — it must decode as an ObjectId, never attempted as a ULID.
        var hex24 = string.Concat(Enumerable.Repeat("0123456789ABCDEF", 2))[..24];
        Assert.Equal(24, hex24.Length);
        var result = IdDecodeTool.Decode(hex24, SnowflakeTool.DiscordEpochMs);
        Assert.Equal(DetectedIdKind.ObjectId, result.Kind);
    }

    [Theory]
    [InlineData("99999999999999999999")] // 20 digits: longer than any 64-bit snowflake can be
    [InlineData("9999999999999999999")] // 19 digits but > long.MaxValue (9223372036854775807)
    public void Decode_NumericStringTooBigForSnowflake_ThrowsInsteadOfMisclassifying(string input)
    {
        // Must not crash (e.g. on an unhandled OverflowException) and must not silently produce
        // a wrapped/truncated Snowflake — there is no valid interpretation, so it should report
        // "no format matched" like any other unrecognized input.
        var ex = Assert.Throws<FormatException>(() => IdDecodeTool.Decode(input, SnowflakeTool.DiscordEpochMs));
        Assert.Contains("Could not detect", ex.Message);
    }

    /// <summary>
    /// Regression guard for the DECODE tab's "paste thousands of ids" scenario: Decode() must stay
    /// cheap per line so a large paste doesn't itself become the bottleneck (the view additionally
    /// debounces the decode pass and virtualizes the results list — see NanoIdView).
    /// </summary>
    [Fact(Timeout = 10_000)]
    public void Decode_TenThousandLines_CompletesQuickly()
    {
        const int count = 10_000;
        var ids = Enumerable.Range(0, count).Select(_ => UlidTool.Generate()).ToList();

        var sw = Stopwatch.StartNew();
        foreach (var id in ids)
            IdDecodeTool.Decode(id, SnowflakeTool.DiscordEpochMs);
        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(2), $"Decoding {count} ids took {sw.Elapsed}, expected well under 2s.");
    }
}
