using Delp.Core.Tools.TextProcessing;

namespace Delp.Core.Tests.Tools.TextProcessing;

public class EpochToolTests
{
    [Theory]
    [InlineData("1712345678", EpochUnit.Seconds)]
    [InlineData("171234567", EpochUnit.Seconds)]
    [InlineData("1712345678901", EpochUnit.Millis)]
    [InlineData("1712345678901234", EpochUnit.Micros)]
    public void Detect_UsesDigitCountHeuristic(string input, EpochUnit expected)
    {
        var (_, unit) = EpochTool.Detect(input);
        Assert.Equal(expected, unit);
    }

    [Fact]
    public void Detect_NegativeValue_ParsesCorrectly()
    {
        var (value, unit) = EpochTool.Detect("-100");
        Assert.Equal(-100, value);
        Assert.Equal(EpochUnit.Seconds, unit);
    }

    [Fact]
    public void Detect_NonNumeric_Throws()
    {
        Assert.Throws<FormatException>(() => EpochTool.Detect("not-a-number"));
    }

    [Fact]
    public void RoundTrip_SecondsToDateAndBack()
    {
        var date = EpochTool.ToDate(1712345678, EpochUnit.Seconds);
        var values = EpochTool.FromDate(date);
        Assert.Equal(1712345678, values.Seconds);
    }

    [Fact]
    public void RoundTrip_MillisToDateAndBack()
    {
        var date = EpochTool.ToDate(1712345678901, EpochUnit.Millis);
        var values = EpochTool.FromDate(date);
        Assert.Equal(1712345678901, values.Millis);
    }

    [Fact]
    public void ToDate_YearThirtyThousand_ThrowsFormatException()
    {
        // ~year 30000 in unix seconds is far beyond DateTimeOffset's max (year 9999).
        Assert.Throws<FormatException>(() => EpochTool.ToDate(900000000000, EpochUnit.Seconds));
    }

    [Theory]
    [InlineData(-3 * 86400, "3 d ago")]
    [InlineData(2 * 3600, "in 2 h")]
    public void Humanize_Buckets(double offsetSeconds, string expected)
    {
        var now = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var target = now.AddSeconds(offsetSeconds);
        Assert.Equal(expected, EpochTool.Humanize(target, now));
    }

    [Fact]
    public void Humanize_JustNow()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Equal("just now", EpochTool.Humanize(now, now));
    }

    [Fact]
    public void ToDate_KnownEpoch_MatchesExpectedUtc()
    {
        var date = EpochTool.ToDate(0, EpochUnit.Seconds);
        Assert.Equal(new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero), date);
    }
}
