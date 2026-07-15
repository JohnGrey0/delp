using System.Globalization;
using System.Text;

namespace Delp.Core.Tools.TextProcessing;

/// <summary>A single instant converted into a target time zone: the pane the ZONES tab renders
/// one row per pinned zone from.</summary>
public sealed record ZoneConversion(string ZoneId, string DisplayName, DateTimeOffset LocalTime, TimeSpan UtcOffset, bool IsDaylightSaving);

/// <summary>Human-readable and machine breakdowns of the gap between two instants, for the
/// DELTA tab.</summary>
public sealed record DurationBreakdown(string Human, double TotalDays, double TotalHours, double TotalMinutes, double TotalSeconds, string Iso8601, int Direction);

public enum DateMathUnit
{
    Seconds,
    Minutes,
    Hours,
    Days,
    Weeks,
    Months,
    Years,
}

/// <summary>Sibling to <see cref="EpochTool"/>: flexible date parsing, time-zone conversion,
/// and duration/date-math helpers for the Date &amp; Time Converter's ZONES and DELTA tabs. All
/// functions are pure — callers inject "now" rather than reading the clock here, so they stay
/// unit-testable.</summary>
public static class DateTimeTool
{
    /// <summary>Sentinel zone id (not a real <see cref="TimeZoneInfo"/> id) meaning "this
    /// machine's local zone" — kept distinct from whatever <see cref="TimeZoneInfo.Local"/>'s
    /// own <c>Id</c> happens to be, so a pinned "Local" row is always recognizable as such even
    /// if it duplicates another pinned zone.</summary>
    public const string LocalZoneId = "Local";

    /// <summary>Default pinned rows for a fresh ZONES tab (session-only — nothing here is
    /// persisted). Windows time zone ids, since <see cref="TimeZoneInfo.FindSystemTimeZoneById"/>
    /// resolves those natively on Windows.</summary>
    public static readonly IReadOnlyList<string> DefaultPinnedZoneIds =
    [
        "UTC",
        LocalZoneId,
        "Pacific Standard Time",   // US Pacific
        "Eastern Standard Time",   // US Eastern
        "GMT Standard Time",       // London
        "W. Europe Standard Time", // Berlin
        "India Standard Time",     // India
        "China Standard Time",     // China / Singapore
        "Tokyo Standard Time",     // Tokyo
        "AUS Eastern Standard Time", // Sydney
    ];

    private static readonly string[] FlexibleFormats =
    [
        "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm", "yyyy-MM-dd",
    ];

    /// <summary>Parses an ISO-ish or common date/time string. Strings with an explicit
    /// offset/zone parse as given; strings without one are treated as local time. Blank input
    /// maps to <paramref name="now"/>.</summary>
    /// <exception cref="FormatException">The text isn't blank and isn't a recognized
    /// format.</exception>
    public static DateTimeOffset ParseFlexible(string? text, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(text))
            return now;

        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var withOffset))
            return withOffset;

        if (DateTime.TryParseExact(text, FlexibleFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
            return new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Local));

        throw new FormatException($"'{text}' is not a recognized date/time format.");
    }

    /// <summary>Like <see cref="ParseFlexible(string?, DateTimeOffset)"/>, but a string with no
    /// explicit offset is interpreted as wall-clock time in <paramref name="assumedZone"/>
    /// instead of the machine's local zone — for the ZONES tab, where the source zone is a
    /// user-chosen picker rather than "wherever this app happens to be running".</summary>
    /// <exception cref="FormatException">The text isn't blank and isn't a recognized
    /// format.</exception>
    public static DateTimeOffset ParseFlexible(string? text, DateTimeOffset now, TimeZoneInfo assumedZone)
    {
        if (string.IsNullOrWhiteSpace(text))
            return now;

        // Checked before the general parse below: DateTimeOffset.TryParse *succeeds* even for
        // offset-less strings (it silently assumes the machine's local offset), which would
        // otherwise make assumedZone dead code for exactly the plain "yyyy-MM-dd HH:mm:ss"
        // shapes it exists to handle. An exact match here means the text truly had no
        // offset/zone of its own, so assumedZone gets to decide what it means.
        if (DateTime.TryParseExact(text, FlexibleFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var naive))
        {
            var unspecified = DateTime.SpecifyKind(naive, DateTimeKind.Unspecified);
            var utc = TimeZoneInfo.ConvertTimeToUtc(unspecified, assumedZone);
            return new DateTimeOffset(utc, TimeSpan.Zero);
        }

        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var withOffset))
            return withOffset;

        throw new FormatException($"'{text}' is not a recognized date/time format.");
    }

    /// <summary>Resolves a zone id, honoring the <see cref="LocalZoneId"/> sentinel.</summary>
    /// <exception cref="FormatException">The id isn't a recognized time zone.</exception>
    public static TimeZoneInfo FindZone(string id)
    {
        if (id == LocalZoneId)
            return TimeZoneInfo.Local;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new FormatException($"'{id}' is not a recognized time zone.", ex);
        }
    }

    /// <summary>All zones installed on this machine, sorted by UTC offset then name — the
    /// source for the ZONES tab's "add a zone" and source-zone pickers.</summary>
    public static IReadOnlyList<TimeZoneInfo> GetAllZones() =>
        TimeZoneInfo.GetSystemTimeZones();

    public static ZoneConversion ConvertToZone(DateTimeOffset instant, TimeZoneInfo zone)
    {
        var converted = TimeZoneInfo.ConvertTime(instant, zone);
        return new ZoneConversion(zone.Id, zone.DisplayName, converted, converted.Offset, zone.IsDaylightSavingTime(converted));
    }

    /// <summary>Duration between two instants: <paramref name="a"/> is the earlier/"FROM" end,
    /// <paramref name="b"/> the later/"TO" end. Magnitudes (human text, totals, ISO 8601) are
    /// always non-negative; <see cref="DurationBreakdown.Direction"/> is +1 when B is after A,
    /// -1 when B is before A, 0 when equal.</summary>
    public static DurationBreakdown Delta(DateTimeOffset a, DateTimeOffset b)
    {
        var span = b - a;
        var abs = span.Duration();
        var direction = span > TimeSpan.Zero ? 1 : span < TimeSpan.Zero ? -1 : 0;

        return new DurationBreakdown(
            Human: FormatHuman(abs),
            TotalDays: abs.TotalDays,
            TotalHours: abs.TotalHours,
            TotalMinutes: abs.TotalMinutes,
            TotalSeconds: abs.TotalSeconds,
            Iso8601: FormatIso8601Duration(abs),
            Direction: direction);
    }

    private static string FormatHuman(TimeSpan abs)
    {
        if (abs.TotalSeconds < 1)
            return "0 s";

        var parts = new List<string>();
        if (abs.Days != 0)
            parts.Add($"{abs.Days} d");
        if (abs.Hours != 0)
            parts.Add($"{abs.Hours} h");
        if (abs.Minutes != 0)
            parts.Add($"{abs.Minutes} m");
        if (abs.Seconds != 0 || parts.Count == 0)
            parts.Add($"{abs.Seconds} s");

        return string.Join(' ', parts);
    }

    private static string FormatIso8601Duration(TimeSpan abs)
    {
        if (abs == TimeSpan.Zero)
            return "PT0S";

        var sb = new StringBuilder("P");
        if (abs.Days != 0)
            sb.Append(abs.Days).Append('D');

        if (abs.Hours != 0 || abs.Minutes != 0 || abs.Seconds != 0)
        {
            sb.Append('T');
            if (abs.Hours != 0)
                sb.Append(abs.Hours).Append('H');
            if (abs.Minutes != 0)
                sb.Append(abs.Minutes).Append('M');
            if (abs.Seconds != 0 || (abs.Hours == 0 && abs.Minutes == 0))
                sb.Append(abs.Seconds).Append('S');
        }

        return sb.ToString();
    }

    /// <summary>Date math: <paramref name="baseDate"/> ± <paramref name="amount"/> in the given
    /// unit. Months/years use calendar arithmetic (<see cref="DateTimeOffset.AddMonths"/> /
    /// <see cref="DateTimeOffset.AddYears"/>), so they can land on a different day-of-month than
    /// a fixed-length shift would.</summary>
    /// <exception cref="FormatException">The result is out of the representable date
    /// range, or <paramref name="amount"/> is <see cref="double.NaN"/>.</exception>
    public static DateTimeOffset AddUnits(DateTimeOffset baseDate, double amount, DateMathUnit unit)
    {
        // DateTimeOffset.AddSeconds/Minutes/Hours/Days silently no-op on NaN instead of
        // throwing (the internal range check "milliseconds > max || milliseconds < -max" is
        // always false for NaN, and the unchecked NaN-to-long tick conversion yields 0) — so
        // without this guard, typing "NaN" for those units would return baseDate unchanged
        // rather than surfacing an error. AddMonths/AddYears already reject NaN correctly via
        // the checked(int) cast, but this keeps every unit's behavior consistent and explicit.
        if (double.IsNaN(amount))
            throw new FormatException($"'{amount}' is not a usable amount for date math.");

        try
        {
            return unit switch
            {
                DateMathUnit.Seconds => baseDate.AddSeconds(amount),
                DateMathUnit.Minutes => baseDate.AddMinutes(amount),
                DateMathUnit.Hours => baseDate.AddHours(amount),
                DateMathUnit.Days => baseDate.AddDays(amount),
                DateMathUnit.Weeks => baseDate.AddDays(amount * 7),
                DateMathUnit.Months => baseDate.AddMonths(checked((int)amount)),
                DateMathUnit.Years => baseDate.AddYears(checked((int)amount)),
                _ => throw new ArgumentOutOfRangeException(nameof(unit)),
            };
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or OverflowException)
        {
            throw new FormatException($"{baseDate:O} ± {amount} {unit} is out of the representable date range.", ex);
        }
    }
}
