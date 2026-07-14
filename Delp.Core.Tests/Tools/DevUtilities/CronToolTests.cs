using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class CronToolTests
{
    // 2026-07-13T00:00:00Z is a Monday.
    private static readonly DateTime FixedFromUtc = new(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Explain_FiveFieldExpression_DetectsNoSeconds()
    {
        var report = CronTool.Explain("*/15 9-17 * * 1-5", FixedFromUtc);
        Assert.False(report.HasSeconds);
        Assert.Equal(5, report.Fields.Count);
    }

    [Fact]
    public void Explain_SixFieldExpression_DetectsSeconds()
    {
        var report = CronTool.Explain("*/5 * * * * *", FixedFromUtc);
        Assert.True(report.HasSeconds);
        Assert.Equal(6, report.Fields.Count);
        Assert.Equal("Second", report.Fields[0].Field);
    }

    [Theory]
    [InlineData("*/15 9-17 * * 1-5", "Every 15 minutes, between 09:00 AM and 05:59 PM, Monday through Friday")]
    [InlineData("* * * * *", "Every minute")]
    [InlineData("0 0 * * *", "At 12:00 AM")]
    [InlineData("30 9 * * 1", "At 09:30 AM, only on Monday")]
    public void Explain_HumanText_MatchesDescriptorFixtures(string expr, string expectedHuman)
    {
        var report = CronTool.Explain(expr, FixedFromUtc);
        Assert.Equal(expectedHuman, report.Human);
    }

    [Fact]
    public void Explain_NextOccurrences_UseFixedFromTimeAndAreMonotonic()
    {
        var report = CronTool.Explain("*/15 9-17 * * 1-5", FixedFromUtc);
        Assert.Equal(10, report.NextLocal.Count);

        var expectedFirstUtc = new DateTime(2026, 7, 13, 9, 0, 0, DateTimeKind.Utc);
        Assert.Equal(expectedFirstUtc.ToLocalTime(), report.NextLocal[0]);

        var expectedSecondUtc = new DateTime(2026, 7, 13, 9, 15, 0, DateTimeKind.Utc);
        Assert.Equal(expectedSecondUtc.ToLocalTime(), report.NextLocal[1]);

        for (var i = 1; i < report.NextLocal.Count; i++)
            Assert.True(report.NextLocal[i] > report.NextLocal[i - 1]);
    }

    [Fact]
    public void Explain_SixFieldSeconds_NextOccurrencesEveryFiveSeconds()
    {
        var report = CronTool.Explain("*/5 * * * * *", FixedFromUtc);
        var expectedFirstUtc = new DateTime(2026, 7, 13, 0, 0, 5, DateTimeKind.Utc);
        Assert.Equal(expectedFirstUtc.ToLocalTime(), report.NextLocal[0]);
    }

    [Fact]
    public void Explain_InvalidExpression_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CronTool.Explain("not a cron expression at all"));
    }

    [Fact]
    public void Explain_InvalidFieldValue_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CronTool.Explain("99 * * * *"));
    }

    [Fact]
    public void Explain_WrongFieldCount_ThrowsFormatExceptionMentioningFieldCount()
    {
        var ex = Assert.Throws<FormatException>(() => CronTool.Explain("* * *"));
        Assert.Contains("5 fields", ex.Message);
    }

    [Fact]
    public void Explain_EmptyExpression_Throws()
    {
        Assert.Throws<FormatException>(() => CronTool.Explain(""));
    }

    [Fact]
    public void Explain_FieldTable_DescribesDayOfWeekRangeByName()
    {
        var report = CronTool.Explain("*/15 9-17 * * 1-5", FixedFromUtc);
        var dow = report.Fields[^1];
        Assert.Equal("Day of week", dow.Field);
        Assert.Equal("1-5", dow.Value);
        Assert.Equal("Monday through Friday", dow.Meaning);
    }

    [Fact]
    public void Explain_FieldTable_DescribesStepMinute()
    {
        var report = CronTool.Explain("*/15 9-17 * * 1-5", FixedFromUtc);
        Assert.Equal("every 15 minute(s)", report.Fields[0].Meaning);
    }
}
