using Delp.Core.Tools.TextProcessing;

namespace Delp.Core.Tests.Tools.TextProcessing;

public class DateTimeToolTests
{
    // ------------------------------------------------------------------------- ParseFlexible

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ParseFlexible_BlankInput_ReturnsNow(string? text)
    {
        var now = new DateTimeOffset(2025, 5, 1, 8, 0, 0, TimeSpan.Zero);
        Assert.Equal(now, DateTimeTool.ParseFlexible(text, now));
    }

    [Fact]
    public void ParseFlexible_LocalFormat_ParsesAsLocalTime()
    {
        var now = DateTimeOffset.UtcNow;
        var result = DateTimeTool.ParseFlexible("2025-06-01 12:00:00", now);

        var expectedLocal = new DateTimeOffset(new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Local));
        Assert.Equal(expectedLocal, result);
    }

    [Fact]
    public void ParseFlexible_ExplicitOffset_Preserved()
    {
        var now = DateTimeOffset.UtcNow;
        var result = DateTimeTool.ParseFlexible("2025-06-01T12:00:00+05:00", now);

        Assert.Equal(TimeSpan.FromHours(5), result.Offset);
        Assert.Equal(new DateTime(2025, 6, 1, 12, 0, 0), result.DateTime);
    }

    [Fact]
    public void ParseFlexible_InvalidInput_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => DateTimeTool.ParseFlexible("not a date", DateTimeOffset.UtcNow));
    }

    // --------------------------------------------------------- ParseFlexible (zone-assumed overload)

    [Fact]
    public void ParseFlexible_WithAssumedZone_InterpretsNaiveTextInThatZone()
    {
        var tokyo = DateTimeTool.FindZone("Tokyo Standard Time"); // UTC+9, no DST
        var result = DateTimeTool.ParseFlexible("2025-06-01 12:00:00", DateTimeOffset.UtcNow, tokyo);

        Assert.Equal(new DateTimeOffset(2025, 6, 1, 3, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void ParseFlexible_WithAssumedZone_ExplicitOffsetOverridesZone()
    {
        var tokyo = DateTimeTool.FindZone("Tokyo Standard Time");
        var result = DateTimeTool.ParseFlexible("2025-06-01T12:00:00+05:00", DateTimeOffset.UtcNow, tokyo);

        Assert.Equal(TimeSpan.FromHours(5), result.Offset);
    }

    [Fact]
    public void ParseFlexible_WithAssumedZone_BlankReturnsNow()
    {
        var now = new DateTimeOffset(2025, 5, 1, 8, 0, 0, TimeSpan.Zero);
        Assert.Equal(now, DateTimeTool.ParseFlexible("", now, DateTimeTool.FindZone("UTC")));
    }

    // ------------------------------------------------------------------------------ FindZone

    [Fact]
    public void FindZone_LocalSentinel_ReturnsTimeZoneInfoLocal()
    {
        Assert.Equal(TimeZoneInfo.Local.Id, DateTimeTool.FindZone(DateTimeTool.LocalZoneId).Id);
    }

    [Fact]
    public void FindZone_Utc_Resolves()
    {
        Assert.Equal("UTC", DateTimeTool.FindZone("UTC").Id);
    }

    [Fact]
    public void FindZone_UnknownId_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => DateTimeTool.FindZone("Not/AZone"));
    }

    [Fact]
    public void DefaultPinnedZoneIds_AllResolve()
    {
        foreach (var id in DateTimeTool.DefaultPinnedZoneIds)
            DateTimeTool.FindZone(id); // throws on failure
    }

    // ------------------------------------------------------------------------ ConvertToZone

    [Fact]
    public void ConvertToZone_Utc_HasZeroOffsetAndNoDst()
    {
        var instant = new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero);
        var result = DateTimeTool.ConvertToZone(instant, DateTimeTool.FindZone("UTC"));

        Assert.Equal(TimeSpan.Zero, result.UtcOffset);
        Assert.False(result.IsDaylightSaving);
    }

    [Fact]
    public void ConvertToZone_Tokyo_NeverObservesDst()
    {
        var zone = DateTimeTool.FindZone("Tokyo Standard Time");
        var summer = new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero);
        var winter = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var summerResult = DateTimeTool.ConvertToZone(summer, zone);
        var winterResult = DateTimeTool.ConvertToZone(winter, zone);

        Assert.Equal(TimeSpan.FromHours(9), summerResult.UtcOffset);
        Assert.False(summerResult.IsDaylightSaving);
        Assert.Equal(TimeSpan.FromHours(9), winterResult.UtcOffset);
        Assert.False(winterResult.IsDaylightSaving);
    }

    [Fact]
    public void ConvertToZone_PacificSpringForwardBoundary_OffsetAndDstFlagChange()
    {
        // 2025-03-09 02:00 local is the US spring-forward instant for "Pacific Standard Time"
        // (PST = UTC-8 → PDT = UTC-7), i.e. 2025-03-09T10:00:00Z.
        var zone = DateTimeTool.FindZone("Pacific Standard Time");
        var beforeTransition = new DateTimeOffset(2025, 3, 9, 9, 59, 0, TimeSpan.Zero);
        var afterTransition = new DateTimeOffset(2025, 3, 9, 10, 0, 0, TimeSpan.Zero);

        var before = DateTimeTool.ConvertToZone(beforeTransition, zone);
        var after = DateTimeTool.ConvertToZone(afterTransition, zone);

        Assert.Equal(TimeSpan.FromHours(-8), before.UtcOffset);
        Assert.False(before.IsDaylightSaving);

        Assert.Equal(TimeSpan.FromHours(-7), after.UtcOffset);
        Assert.True(after.IsDaylightSaving);
    }

    // ------------------------------------------------------------------------------- Delta

    [Fact]
    public void Delta_ComputesHumanTotalsAndIso8601()
    {
        var a = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var b = a.AddDays(3).AddHours(4).AddMinutes(12);

        var result = DateTimeTool.Delta(a, b);

        Assert.Equal("3 d 4 h 12 m", result.Human);
        Assert.Equal("P3DT4H12M", result.Iso8601);
        Assert.Equal(1, result.Direction);
        Assert.Equal((b - a).TotalSeconds, result.TotalSeconds, 3);
    }

    [Fact]
    public void Delta_EarlierSecondArgument_MagnitudesStayPositiveDirectionNegative()
    {
        var a = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var b = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero);

        var result = DateTimeTool.Delta(a, b);

        Assert.Equal(-1, result.Direction);
        Assert.Equal(5, result.TotalDays);
        Assert.Equal("5 d", result.Human);
    }

    [Fact]
    public void Delta_EqualInstants_ZeroDurationAndDirection()
    {
        var a = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var result = DateTimeTool.Delta(a, a);

        Assert.Equal(0, result.Direction);
        Assert.Equal("0 s", result.Human);
        Assert.Equal("PT0S", result.Iso8601);
    }

    [Fact]
    public void Delta_SubSecondOnly_FormatsSecondsOnly()
    {
        var a = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var b = a.AddSeconds(45);

        var result = DateTimeTool.Delta(a, b);

        Assert.Equal("45 s", result.Human);
        Assert.Equal("PT45S", result.Iso8601);
    }

    // ---------------------------------------------------------------------------- AddUnits

    [Fact]
    public void AddUnits_Days_AddsCorrectDuration()
    {
        var baseDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var result = DateTimeTool.AddUnits(baseDate, 10, DateMathUnit.Days);
        Assert.Equal(new DateTimeOffset(2025, 1, 11, 0, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void AddUnits_NegativeMonths_SubtractsCalendarMonths()
    {
        var baseDate = new DateTimeOffset(2025, 3, 15, 0, 0, 0, TimeSpan.Zero);
        var result = DateTimeTool.AddUnits(baseDate, -1, DateMathUnit.Months);
        Assert.Equal(new DateTimeOffset(2025, 2, 15, 0, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void AddUnits_Weeks_AddsSevenDaysEach()
    {
        var baseDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var result = DateTimeTool.AddUnits(baseDate, 2, DateMathUnit.Weeks);
        Assert.Equal(new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void AddUnits_Years_UsesCalendarArithmetic()
    {
        var baseDate = new DateTimeOffset(2024, 2, 29, 0, 0, 0, TimeSpan.Zero); // leap day
        var result = DateTimeTool.AddUnits(baseDate, 1, DateMathUnit.Years);
        Assert.Equal(new DateTimeOffset(2025, 2, 28, 0, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void AddUnits_OutOfRange_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => DateTimeTool.AddUnits(DateTimeOffset.MaxValue, 1, DateMathUnit.Days));
    }

    // DateTimeOffset.AddSeconds/Minutes/Hours/Days/AddDays(amount*7) silently no-op on NaN
    // (the BCL's own range check and unchecked NaN->long tick conversion swallow it) instead
    // of throwing, which would otherwise make "NaN" in the amount box return the base date
    // unchanged with no error shown. AddUnits now rejects NaN explicitly for every unit.
    [Theory]
    [InlineData(DateMathUnit.Seconds)]
    [InlineData(DateMathUnit.Minutes)]
    [InlineData(DateMathUnit.Hours)]
    [InlineData(DateMathUnit.Days)]
    [InlineData(DateMathUnit.Weeks)]
    [InlineData(DateMathUnit.Months)]
    [InlineData(DateMathUnit.Years)]
    public void AddUnits_NaNAmount_ThrowsFormatException_ForEveryUnit(DateMathUnit unit)
    {
        var baseDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.Throws<FormatException>(() => DateTimeTool.AddUnits(baseDate, double.NaN, unit));
    }

    [Fact]
    public void AddUnits_PositiveInfinityAmount_ThrowsFormatException()
    {
        var baseDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.Throws<FormatException>(() => DateTimeTool.AddUnits(baseDate, double.PositiveInfinity, DateMathUnit.Days));
    }
}
