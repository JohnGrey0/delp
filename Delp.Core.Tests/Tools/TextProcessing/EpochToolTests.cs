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

    // --------------------------------------------------------------------- FILETIME / ticks

    [Fact]
    public void FileTime_RoundTrips()
    {
        var original = new DateTimeOffset(2025, 6, 15, 12, 30, 0, TimeSpan.Zero);
        var fileTime = original.UtcDateTime.ToFileTimeUtc();

        var restored = EpochTool.ToDate(fileTime, EpochUnit.FileTime);

        Assert.Equal(original, restored);
    }

    [Fact]
    public void Ticks_RoundTrips()
    {
        var original = new DateTimeOffset(2025, 6, 15, 12, 30, 0, TimeSpan.Zero);
        var ticks = original.UtcDateTime.Ticks;

        var restored = EpochTool.ToDate(ticks, EpochUnit.Ticks);

        Assert.Equal(original, restored);
    }

    [Fact]
    public void ToDate_FileTime_NegativeValue_ThrowsFormatException()
    {
        // FILETIME can't represent instants before 1601-01-01.
        Assert.Throws<FormatException>(() => EpochTool.ToDate(-1, EpochUnit.FileTime));
    }

    [Fact]
    public void ToDate_Ticks_NegativeValue_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => EpochTool.ToDate(-1, EpochUnit.Ticks));
    }

    // ------------------------------------------------------------------ Auto detection bands

    [Fact]
    public void Detect_SeventeenDigitFileTimeValue_DetectsAsFileTime()
    {
        // 1700-01-01 lands in the 17-digit band (well below the 18-digit ticks range).
        var fileTime = new DateTime(1700, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToFileTimeUtc();

        var (value, unit) = EpochTool.Detect(fileTime.ToString());

        Assert.Equal(fileTime, value);
        Assert.Equal(EpochUnit.FileTime, unit);
    }

    [Fact]
    public void Detect_EighteenDigitSmallValue_DetectsAsFileTime()
    {
        // A modern-era FILETIME (~1.3e17) is 18 digits but well under the ticks/FILETIME split.
        var fileTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToFileTimeUtc();

        var (_, unit) = EpochTool.Detect(fileTime.ToString());

        Assert.Equal(EpochUnit.FileTime, unit);
    }

    [Fact]
    public void Detect_EighteenDigitLargeValue_DetectsAsTicks()
    {
        // A modern-era tick count (~6.4e17) is also 18 digits but above the split.
        var ticks = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

        var (_, unit) = EpochTool.Detect(ticks.ToString());

        Assert.Equal(EpochUnit.Ticks, unit);
    }

    [Fact]
    public void Detect_NineteenDigitValue_DetectsAsTicks()
    {
        var ticks = DateTime.MaxValue.Ticks; // 3155378975999999999 — 19 digits
        Assert.Equal(19, ticks.ToString().Length);

        var (_, unit) = EpochTool.Detect(ticks.ToString());

        Assert.Equal(EpochUnit.Ticks, unit);
    }

    // ----------------------------------------------------------------------------- Describe

    [Fact]
    public void Describe_ModernDate_PopulatesEveryField()
    {
        var date = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var now = date.AddDays(1);

        var result = EpochTool.Describe(date, now);

        Assert.Equal(date.ToUnixTimeSeconds(), result.Seconds);
        Assert.Equal(date.ToUnixTimeMilliseconds(), result.Millis);
        Assert.Equal(date.UtcDateTime.ToFileTimeUtc(), result.FileTime);
        Assert.Equal(date.UtcDateTime.Ticks, result.Ticks);
        Assert.Equal("2024-01-01T00:00:00Z", result.UtcIso);
        Assert.Contains("2024", result.Rfc1123);
        Assert.Equal("1 d ago", result.Relative);
    }

    [Fact]
    public void Describe_DateBeforeFileTimeEpoch_FileTimeIsNull()
    {
        var date = new DateTimeOffset(1500, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var result = EpochTool.Describe(date, date);

        Assert.Null(result.FileTime);
        Assert.True(result.Ticks > 0); // ticks (epoch year 1) still represent this date fine
    }
}
