using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class ByteSizeToolTests
{
    private static decimal EquivalentOf(ByteSizeResult r, ByteUnit unit) =>
        r.Equivalents.Single(e => e.Unit == unit).Value;

    [Fact]
    public void Convert_1KB_Equals1000Bytes()
    {
        var r = ByteSizeTool.Convert(1, ByteUnit.KB);
        Assert.Equal(1000m, EquivalentOf(r, ByteUnit.B));
    }

    [Fact]
    public void Convert_1KiB_Equals1024Bytes()
    {
        var r = ByteSizeTool.Convert(1, ByteUnit.KiB);
        Assert.Equal(1024m, EquivalentOf(r, ByteUnit.B));
    }

    [Fact]
    public void Convert_1Byte_Equals8Bits()
    {
        var r = ByteSizeTool.Convert(1, ByteUnit.B);
        Assert.Equal(8m, EquivalentOf(r, ByteUnit.Bit));
    }

    [Fact]
    public void Convert_1MB_Equals1000KB()
    {
        var r = ByteSizeTool.Convert(1, ByteUnit.MB);
        Assert.Equal(1000m, EquivalentOf(r, ByteUnit.KB));
    }

    [Fact]
    public void Convert_1GiB_Equals1024MiB()
    {
        var r = ByteSizeTool.Convert(1, ByteUnit.GiB);
        Assert.Equal(1024m, EquivalentOf(r, ByteUnit.MiB));
    }

    [Fact]
    public void Convert_1TiB_EqualsExpectedBytes()
    {
        var r = ByteSizeTool.Convert(1, ByteUnit.TiB);
        Assert.Equal(1_099_511_627_776m, EquivalentOf(r, ByteUnit.B));
    }

    [Fact]
    public void Convert_1Gbit_Equals1000Mbit()
    {
        var r = ByteSizeTool.Convert(1, ByteUnit.Gbit);
        Assert.Equal(1000m, EquivalentOf(r, ByteUnit.Mbit));
    }

    [Fact]
    public void Convert_ReturnsAllThirteenUnits()
    {
        var r = ByteSizeTool.Convert(1, ByteUnit.MB);
        Assert.Equal(13, r.Equivalents.Count);
        Assert.Equal(4, r.TransferTimes.Count);
    }

    [Fact]
    public void Convert_Zero_AllEquivalentsAreZero()
    {
        var r = ByteSizeTool.Convert(0, ByteUnit.GB);
        Assert.All(r.Equivalents, e => Assert.Equal(0m, e.Value));
    }

    [Fact]
    public void Convert_Negative_Throws()
    {
        Assert.Throws<FormatException>(() => ByteSizeTool.Convert(-1, ByteUnit.KB));
    }

    [Fact]
    public void Convert_HugeValue_ThrowsInsteadOfOverflowing()
    {
        Assert.Throws<FormatException>(() => ByteSizeTool.Convert(decimal.MaxValue, ByteUnit.TiB));
    }

    [Fact]
    public void Convert_HugeValueInSmallMultiplierUnit_DoesNotOverflowTransferTime()
    {
        // Bit's multiplier is 1, so decimal.MaxValue survives the initial-multiply overflow
        // guard untouched — but at the slowest reference bandwidth (10 Mbps) the resulting
        // duration is far more than long.MaxValue seconds. FormatDuration must not cast that
        // through `long` (regression test: it used to throw an uncaught OverflowException,
        // not the FormatException this API documents for oversized input).
        var r = ByteSizeTool.Convert(decimal.MaxValue, ByteUnit.Bit);
        Assert.All(r.TransferTimes, t => Assert.False(string.IsNullOrWhiteSpace(t.Duration)));
    }

    [Fact]
    public void TransferTime_1GB_At100Mbps_Is80Seconds()
    {
        // 1 GB = 8,000,000,000 bits; at 100,000,000 bit/s that's exactly 80 s = "1 m 20 s".
        var r = ByteSizeTool.Convert(1, ByteUnit.GB);
        var t = r.TransferTimes.Single(t => t.RateLabel == "100 Mbps");
        Assert.Equal("1 m 20 s", t.Duration);
    }

    [Fact]
    public void TransferTime_1GB_At1Gbps_Is8Seconds()
    {
        var r = ByteSizeTool.Convert(1, ByteUnit.GB);
        var t = r.TransferTimes.Single(t => t.RateLabel == "1 Gbps");
        Assert.Equal("8 s", t.Duration);
    }

    [Fact]
    public void TransferTime_SubSecond_FormatsAsMilliseconds()
    {
        // 1 MB = 8,000,000 bits; at 100,000,000 bit/s that's exactly 0.08 s = 80 ms.
        var r = ByteSizeTool.Convert(1, ByteUnit.MB);
        var t = r.TransferTimes.Single(t => t.RateLabel == "100 Mbps");
        Assert.Equal("80 ms", t.Duration);
    }

    [Fact]
    public void TransferTime_SubMillisecond_FormatsAsMicroseconds()
    {
        // 1 MB = 8,000,000 bits; at 10,000,000,000 bit/s that's exactly 0.0008 s = 800 µs.
        var r = ByteSizeTool.Convert(1, ByteUnit.MB);
        var t = r.TransferTimes.Single(t => t.RateLabel == "10 Gbps");
        Assert.Equal("800 µs", t.Duration);
    }

    [Theory]
    [InlineData(ByteUnit.B)]
    [InlineData(ByteUnit.KiB)]
    [InlineData(ByteUnit.Gbit)]
    public void Convert_RoundTripsBackToItself(ByteUnit unit)
    {
        var r = ByteSizeTool.Convert(5, unit);
        Assert.Equal(5m, EquivalentOf(r, unit));
    }

    [Fact]
    public void UnitList_HasThirteenUnitsInSpecOrder()
    {
        var labels = ByteSizeTool.UnitList.Select(u => u.Label).ToArray();
        Assert.Equal(new[] { "B", "KB", "MB", "GB", "TB", "KiB", "MiB", "GiB", "TiB", "bit", "Kbit", "Mbit", "Gbit" }, labels);
    }
}
