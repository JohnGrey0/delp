using Delp.Core.Tools.Hashing;

namespace Delp.Core.Tests.Tools.Hashing;

public class UuidToolsTests
{
    // Every assertion below reads the canonical Guid string ("N"/"D" format) directly, rather than
    // Guid.ToByteArray() or the Guid(byte[]) constructor, since those use a mixed-endian layout that
    // does not match RFC 9562 field order.
    private static int VersionNibble(Guid g) => Convert.ToInt32(g.ToString("N")[12].ToString(), 16);

    private static int VariantNibble(Guid g) => Convert.ToInt32(g.ToString("N")[16].ToString(), 16);

    private static bool HasRfcVariant(Guid g) => VariantNibble(g) is >= 0x8 and <= 0xB;

    // ---------------------------------------------------------------- UuidFormat

    [Fact]
    public void UuidFormat_Default_UsesLowercaseHyphenated()
    {
        var guid = Guid.Parse("5df41881-3aed-3515-88a7-2f4a814cf09e");
        Assert.Equal("5df41881-3aed-3515-88a7-2f4a814cf09e", UuidFormat.Apply(guid, new UuidStyle()));
    }

    [Fact]
    public void UuidFormat_Uppercase_UppercasesHex()
    {
        var guid = Guid.Parse("5df41881-3aed-3515-88a7-2f4a814cf09e");
        Assert.Equal("5DF41881-3AED-3515-88A7-2F4A814CF09E", UuidFormat.Apply(guid, new UuidStyle(Uppercase: true)));
    }

    [Fact]
    public void UuidFormat_Braces_WrapsInBraces()
    {
        var guid = Guid.Parse("5df41881-3aed-3515-88a7-2f4a814cf09e");
        Assert.Equal("{5df41881-3aed-3515-88a7-2f4a814cf09e}", UuidFormat.Apply(guid, new UuidStyle(Braces: true)));
    }

    [Fact]
    public void UuidFormat_NoHyphens_RemovesHyphens()
    {
        var guid = Guid.Parse("5df41881-3aed-3515-88a7-2f4a814cf09e");
        Assert.Equal("5df418813aed351588a72f4a814cf09e", UuidFormat.Apply(guid, new UuidStyle(NoHyphens: true)));
    }

    [Fact]
    public void UuidFormat_AllOptions_Combine()
    {
        var guid = Guid.Parse("5df41881-3aed-3515-88a7-2f4a814cf09e");
        var style = new UuidStyle(Uppercase: true, Braces: true, NoHyphens: true);
        Assert.Equal("{5DF418813AED351588A72F4A814CF09E}", UuidFormat.Apply(guid, style));
    }

    // ---------------------------------------------------------------- UuidBatch

    [Fact]
    public void UuidBatch_Generate_RespectsCount()
    {
        var results = UuidBatch.Generate(UuidV4.Generate, 7, new UuidStyle());
        Assert.Equal(7, results.Count);
    }

    [Fact]
    public void UuidBatch_Generate_AppliesFormatting()
    {
        var results = UuidBatch.Generate(UuidV4.Generate, 3, new UuidStyle(Uppercase: true, Braces: true));
        Assert.All(results, r =>
        {
            Assert.StartsWith("{", r);
            Assert.EndsWith("}", r);
            Assert.Equal(r, r.ToUpperInvariant());
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void UuidBatch_Generate_OutOfRangeCount_Throws(int count)
    {
        Assert.Throws<ArgumentException>(() => UuidBatch.Generate(UuidV4.Generate, count, new UuidStyle()));
    }

    // ---------------------------------------------------------------- UUID v1

    [Fact]
    public void UuidV1_Generate_HasCorrectVersionAndVariant()
    {
        var guid = UuidV1.Generate(UuidNode.RandomNode(), UuidNode.RandomClockSequence());
        Assert.Equal(1, VersionNibble(guid));
        Assert.True(HasRfcVariant(guid));
    }

    [Fact]
    public void UuidV1_DecodeTimestamp_RoundTripsNearNow()
    {
        var before = DateTimeOffset.UtcNow;
        var guid = UuidV1.Generate(UuidNode.RandomNode(), UuidNode.RandomClockSequence());
        var decoded = UuidV1.DecodeTimestamp(guid);
        Assert.True(Math.Abs((decoded - before).TotalSeconds) < 5);
    }

    [Fact]
    public void UuidV1_DecodeTimestamp_OnNonV1_Throws()
    {
        Assert.Throws<FormatException>(() => UuidV1.DecodeTimestamp(Guid.NewGuid()));
    }

    [Fact]
    public void UuidV1_RandomNode_HasMulticastBitSet()
    {
        var node = UuidNode.RandomNode();
        Assert.True(UuidNode.IsMulticast(node));
    }

    [Fact]
    public void UuidV1_Batch_IsUnique()
    {
        var node = UuidNode.RandomNode();
        var clockSeq = UuidNode.RandomClockSequence();
        var guids = Enumerable.Range(0, 100).Select(_ => UuidV1.Generate(node, clockSeq)).ToList();
        Assert.Equal(100, guids.Distinct().Count());
    }

    // ---------------------------------------------------------------- UUID v2

    [Fact]
    public void UuidV2_Generate_HasCorrectVersionAndVariant()
    {
        var guid = UuidV2.Generate(1000, DceDomain.Person, UuidNode.RandomNode(), UuidNode.RandomClockSequence());
        Assert.Equal(2, VersionNibble(guid));
        Assert.True(HasRfcVariant(guid));
    }

    [Theory]
    [InlineData(DceDomain.Person)]
    [InlineData(DceDomain.Group)]
    [InlineData(DceDomain.Org)]
    public void UuidV2_Decode_RoundTripsDomainAndLocalId(DceDomain domain)
    {
        const uint localId = 4_200_000_001;
        var guid = UuidV2.Generate(localId, domain, UuidNode.RandomNode(), UuidNode.RandomClockSequence());
        var (decodedLocalId, decodedDomain) = UuidV2.Decode(guid);
        Assert.Equal(localId, decodedLocalId);
        Assert.Equal(domain, decodedDomain);
    }

    [Fact]
    public void UuidV2_Decode_OnNonV2_Throws()
    {
        Assert.Throws<FormatException>(() => UuidV2.Decode(Guid.NewGuid()));
    }

    // ---------------------------------------------------------------- UUID v3

    [Fact]
    public void UuidV3_DnsNamespace_MatchesRfcVector()
    {
        var guid = UuidNameBased.GenerateV3(UuidNamespaces.Dns, "www.example.com");
        Assert.Equal(Guid.Parse("5df41881-3aed-3515-88a7-2f4a814cf09e"), guid);
    }

    [Fact]
    public void UuidV3_Generate_HasCorrectVersionAndVariant()
    {
        var guid = UuidNameBased.GenerateV3(UuidNamespaces.Url, "example");
        Assert.Equal(3, VersionNibble(guid));
        Assert.True(HasRfcVariant(guid));
    }

    [Fact]
    public void UuidV3_Generate_IsDeterministic()
    {
        var a = UuidNameBased.GenerateV3(UuidNamespaces.Dns, "example.com");
        var b = UuidNameBased.GenerateV3(UuidNamespaces.Dns, "example.com");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ParseNamespace_InvalidText_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => UuidNameBased.ParseNamespace("not-a-guid"));
    }

    [Fact]
    public void ParseNamespace_ValidText_Parses()
    {
        var ns = UuidNameBased.ParseNamespace(" 6ba7b810-9dad-11d1-80b4-00c04fd430c8 ");
        Assert.Equal(UuidNamespaces.Dns, ns);
    }

    // ---------------------------------------------------------------- UUID v4

    [Fact]
    public void UuidV4_Generate_HasCorrectVersionAndVariant()
    {
        var guid = UuidV4.Generate();
        Assert.Equal(4, VersionNibble(guid));
        Assert.True(HasRfcVariant(guid));
    }

    [Fact]
    public void UuidV4_Batch_ProducesDistinctValues()
    {
        var results = UuidBatch.Generate(UuidV4.Generate, 50, new UuidStyle());
        Assert.Equal(50, results.Distinct().Count());
    }

    // ---------------------------------------------------------------- UUID v5

    [Fact]
    public void UuidV5_DnsNamespace_MatchesRfcVector()
    {
        var guid = UuidNameBased.GenerateV5(UuidNamespaces.Dns, "www.example.com");
        Assert.Equal(Guid.Parse("2ed6657d-e927-568b-95e1-2665a8aea6a2"), guid);
    }

    [Fact]
    public void UuidV5_Generate_HasCorrectVersionAndVariant()
    {
        var guid = UuidNameBased.GenerateV5(UuidNamespaces.Oid, "1.3.6.1");
        Assert.Equal(5, VersionNibble(guid));
        Assert.True(HasRfcVariant(guid));
    }

    // ---------------------------------------------------------------- UUID v6

    [Fact]
    public void UuidV6_Generate_HasCorrectVersionAndVariant()
    {
        var guid = UuidV6.Generate(UuidNode.RandomNode(), UuidNode.RandomClockSequence());
        Assert.Equal(6, VersionNibble(guid));
        Assert.True(HasRfcVariant(guid));
    }

    [Fact]
    public void UuidV6_DecodeTimestamp_RoundTripsNearNow()
    {
        var before = DateTimeOffset.UtcNow;
        var guid = UuidV6.Generate(UuidNode.RandomNode(), UuidNode.RandomClockSequence());
        var decoded = UuidV6.DecodeTimestamp(guid);
        Assert.True(Math.Abs((decoded - before).TotalSeconds) < 5);
    }

    [Fact]
    public void UuidV6_DecodeTimestamp_OnNonV6_Throws()
    {
        Assert.Throws<FormatException>(() => UuidV6.DecodeTimestamp(Guid.NewGuid()));
    }

    [Fact]
    public void UuidV6_SequentialGenerations_SortAscendingAsStrings()
    {
        var node = UuidNode.RandomNode();
        var clockSeq = UuidNode.RandomClockSequence();
        var first = UuidV6.Generate(node, clockSeq).ToString();
        var second = UuidV6.Generate(node, clockSeq).ToString();
        Assert.True(string.CompareOrdinal(first, second) < 0);
    }

    [Fact]
    public void UuidV6_Batch_SortsAscendingAsStrings()
    {
        var node = UuidNode.RandomNode();
        var clockSeq = UuidNode.RandomClockSequence();
        var generated = Enumerable.Range(0, 25).Select(_ => UuidV6.Generate(node, clockSeq).ToString()).ToList();
        var sorted = generated.OrderBy(s => s, StringComparer.Ordinal).ToList();
        Assert.Equal(generated, sorted);
    }

    // ---------------------------------------------------------------- UUID v7

    [Fact]
    public void UuidV7_Generate_HasCorrectVersionAndVariant()
    {
        var guid = UuidV7.Generate();
        Assert.Equal(7, VersionNibble(guid));
        Assert.True(HasRfcVariant(guid));
    }

    [Fact]
    public void UuidV7_Batch_SortsAscendingAsStrings()
    {
        // Spaced a couple of ms apart so each UUID's millisecond timestamp strictly increases -- within
        // the same millisecond, .NET's CreateVersion7() fills the rest with plain random bits (no extra
        // monotonic counter), so ordering is only guaranteed across millisecond boundaries.
        var generated = new List<string>();
        for (var i = 0; i < 15; i++)
        {
            generated.Add(UuidV7.Generate().ToString());
            System.Threading.Thread.Sleep(2);
        }
        var sorted = generated.OrderBy(s => s, StringComparer.Ordinal).ToList();
        Assert.Equal(generated, sorted);
    }

    [Fact]
    public void UuidV7_DecodeTimestamp_IsWithinFiveSecondsOfNow()
    {
        var before = DateTimeOffset.UtcNow;
        var guid = UuidV7.Generate();
        var decoded = UuidV7.DecodeTimestamp(guid);
        Assert.True(Math.Abs((decoded - before).TotalSeconds) < 5);
    }

    [Fact]
    public void UuidV7_DecodeTimestamp_RejectsNonV7()
    {
        Assert.Throws<FormatException>(() => UuidV7.DecodeTimestamp(UuidV4.Generate()));
    }

    // ---------------------------------------------------------------- UUID v8

    [Fact]
    public void UuidV8_Generate_ForcesVersionAndVariant()
    {
        var guid = UuidV8.Generate(customHex: null, randomFill: true);
        Assert.Equal(8, VersionNibble(guid));
        Assert.True(HasRfcVariant(guid));
    }

    [Fact]
    public void UuidV8_CustomHex_SurvivesInNonReservedPositions()
    {
        // All-F payload: every bit should still be 1 except the forced version nibble and the two
        // forced high variant bits.
        var guid = UuidV8.Generate("ffffffffffffffffffffffffffffffff", randomFill: false);
        var hex = guid.ToString("N");
        Assert.Equal("ffffffff", hex[..8]); // time_low-equivalent: fully custom
        Assert.Equal("ffff", hex[8..12]); // time_mid-equivalent: fully custom
        Assert.Equal('8', hex[12]); // forced version nibble
        Assert.Equal("fff", hex[13..16]); // remaining bits of that field: custom
        Assert.Equal('b', hex[16]); // 0xFF with top 2 bits forced to '10' -> 0xB
        Assert.Equal("fffffffffffffff", hex[17..]); // rest: fully custom
    }

    [Fact]
    public void UuidV8_CustomHex_IsZeroPaddedOnTheRight()
    {
        var guid = UuidV8.Generate("ab", randomFill: false);
        var hex = guid.ToString("N");
        Assert.StartsWith("ab000000", hex);
    }

    [Fact]
    public void UuidV8_InvalidHex_Throws()
    {
        Assert.Throws<FormatException>(() => UuidV8.Generate("zzzz", randomFill: false));
    }

    [Fact]
    public void UuidV8_TooLongHex_Throws()
    {
        Assert.Throws<FormatException>(() => UuidV8.Generate(new string('a', 33), randomFill: false));
    }

    [Fact]
    public void UuidV8_EmptyWithoutRandomFill_IsAllZeroPayload()
    {
        var guid = UuidV8.Generate(customHex: null, randomFill: false);
        var hex = guid.ToString("N");
        Assert.Equal("00000000", hex[..8]);
        Assert.Equal("0000", hex[8..12]);
        Assert.Equal("000", hex[13..16]);
        Assert.Equal("000000000000000", hex[17..]);
    }
}

public class UuidTimeBasedBatchTests
{
    [Fact]
    public void V1_BatchOfHundred_AllUnique()
    {
        var node = UuidNode.RandomNode();
        var clockSeq = UuidNode.RandomClockSequence();
        var batch = Enumerable.Range(0, 100).Select(_ => UuidV1.Generate(node, clockSeq)).ToList();
        Assert.Equal(100, batch.Distinct().Count());
    }

    [Fact]
    public void V6_BatchOfHundred_AllUnique()
    {
        var node = UuidNode.RandomNode();
        var clockSeq = UuidNode.RandomClockSequence();
        var batch = Enumerable.Range(0, 100).Select(_ => UuidV6.Generate(node, clockSeq)).ToList();
        Assert.Equal(100, batch.Distinct().Count());
    }

    [Fact]
    public void V2_FixedNodeAndClockSeq_IsIdenticalWithinTimestampWindow_ByDesign()
    {
        // Documents WHY the view randomizes node+clockSeq per UUID: with both fixed,
        // v2's layout leaves nothing that changes between rapid generations.
        var node = UuidNode.RandomNode();
        var clockSeq = UuidNode.RandomClockSequence();
        var a = UuidV2.Generate(1000, DceDomain.Person, node, clockSeq);
        var b = UuidV2.Generate(1000, DceDomain.Person, node, clockSeq);
        Assert.Equal(a, b);
    }

    [Fact]
    public void V2_RandomNodeAndClockSeqPerUuid_BatchOfHundred_AllUnique()
    {
        var batch = Enumerable.Range(0, 100)
            .Select(_ => UuidV2.Generate(1000, DceDomain.Person, UuidNode.RandomNode(), UuidNode.RandomClockSequence()))
            .ToList();
        Assert.Equal(100, batch.Distinct().Count());
    }
}
