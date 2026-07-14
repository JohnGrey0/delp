using Delp.Core.Tools.TextProcessing;

namespace Delp.Core.Tests.Tools.TextProcessing;

public class EpochBatchToolTests
{
    [Fact]
    public void Convert_MixedUnits_DetectsPerLine()
    {
        var rows = EpochBatchTool.Convert("1712345678\n1712345678901");
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Null(r.Error));
        Assert.NotNull(rows[0].UtcIso);
        Assert.NotNull(rows[1].UtcIso);
    }

    [Fact]
    public void Convert_ExtractsFirstIntegerFromNoisyLine()
    {
        var rows = EpochBatchTool.Convert("1712345678, foo bar");
        Assert.Single(rows);
        Assert.Null(rows[0].Error);
    }

    [Fact]
    public void Convert_GarbageLine_ProducesErrorRowNotException()
    {
        var rows = EpochBatchTool.Convert("no digits here");
        Assert.Single(rows);
        Assert.NotNull(rows[0].Error);
    }

    [Fact]
    public void Convert_BlankLines_AreSkipped()
    {
        var rows = EpochBatchTool.Convert("1712345678\n\n   \n1712345679");
        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void Convert_ForcedUnit_OverridesDetection()
    {
        // This 10-digit value would normally auto-detect as Seconds; force Millis instead
        // and confirm it still converts cleanly (small but valid date near the epoch).
        var rows = EpochBatchTool.Convert("1712345678", EpochUnit.Millis);
        Assert.Single(rows);
        Assert.Null(rows[0].Error);
    }

    [Fact]
    public void ToCsv_HasHeaderAndOneRowPerLine()
    {
        var rows = EpochBatchTool.Convert("1712345678\nbad-line");
        var csv = EpochBatchTool.ToCsv(rows);
        var lines = csv.TrimEnd('\n').Split('\n');
        Assert.Equal("input,local,utc,error", lines[0]);
        Assert.Equal(3, lines.Length); // header + 2 rows
        Assert.Contains("No integer timestamp found.", lines[2]);
    }

    [Fact]
    public void ToTable_ContainsArrowSeparatedColumns()
    {
        var rows = EpochBatchTool.Convert("1712345678");
        var table = EpochBatchTool.ToTable(rows);
        Assert.Contains("→", table);
        Assert.Contains("1712345678", table);
    }

    [Fact]
    public void ToTable_EmptyRows_ReturnsEmptyString()
    {
        Assert.Equal("", EpochBatchTool.ToTable([]));
    }
}
