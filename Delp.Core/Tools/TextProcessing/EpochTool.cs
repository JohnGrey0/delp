using System.Globalization;

namespace Delp.Core.Tools.TextProcessing;

public enum EpochUnit
{
    Seconds,
    Millis,
    Micros,

    /// <summary>Windows FILETIME / LDAP timestamp: 100 ns intervals since 1601-01-01 UTC.</summary>
    FileTime,

    /// <summary>.NET ticks: 100 ns intervals since 0001-01-01 UTC (<see cref="DateTime.Ticks"/>).</summary>
    Ticks,
}

public sealed record EpochValues(long Seconds, long Millis);

/// <summary>Every representation of a single instant shown on the CONVERT tab's output card,
/// plus the human-relative description.</summary>
public sealed record DateConversion(
    long Seconds,
    long Millis,
    long? FileTime,
    long Ticks,
    string LocalIso,
    string UtcIso,
    string Rfc1123,
    string Relative);

/// <summary>Unix timestamp ↔ date conversions, with a length-based unit heuristic for
/// pasted timestamps of unknown unit.</summary>
public static class EpochTool
{
    // Ticks (epoch 0001-01-01) and FILETIME (epoch 1601-01-01) both land on 17-18 digit
    // numbers for any date within the two systems' shared usable range, so digit count alone
    // can't tell them apart. Ticks for any calendar year from roughly 950 AD onward are always
    // >= this split; FILETIME stays below it until roughly the year 2550s (1601 + ~950 years —
    // the two systems use the same 100 ns unit, so they cross the same magnitude after the
    // same span of elapsed years, just offset by their different epochs). That covers every
    // realistic pasted timestamp with room to spare.
    private const long FileTimeTicksSplit = 300_000_000_000_000_000L;

    /// <summary>Guesses the unit from digit count/magnitude: ≤10 digits → seconds, 11-13 →
    /// millis, 14-16 → micros, 17+ → FILETIME or .NET ticks (split by magnitude — see
    /// <see cref="FileTimeTicksSplit"/>). Underscores/spaces and a leading sign are
    /// tolerated.</summary>
    /// <exception cref="FormatException">The input has no digits or overflows a 64-bit
    /// integer.</exception>
    public static (long Value, EpochUnit Unit) Detect(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var trimmed = input.Trim().Replace("_", "").Replace(" ", "");
        if (trimmed.Length == 0)
            throw new FormatException("Input is empty.");

        var negative = trimmed[0] == '-';
        var digits = negative || trimmed[0] == '+' ? trimmed[1..] : trimmed;
        if (digits.Length == 0 || !digits.All(char.IsAsciiDigit))
            throw new FormatException($"'{input}' is not a valid integer timestamp.");

        if (!long.TryParse(trimmed, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
            throw new FormatException($"'{input}' is out of range for a 64-bit timestamp.");

        var unit = digits.Length switch
        {
            <= 10 => EpochUnit.Seconds,
            <= 13 => EpochUnit.Millis,
            <= 16 => EpochUnit.Micros,
            _ => Math.Abs(value) < FileTimeTicksSplit ? EpochUnit.FileTime : EpochUnit.Ticks,
        };

        return (value, unit);
    }

    /// <exception cref="FormatException">The value is out of the representable date
    /// range.</exception>
    public static DateTimeOffset ToDate(long value, EpochUnit unit)
    {
        try
        {
            return unit switch
            {
                EpochUnit.Seconds => DateTimeOffset.FromUnixTimeSeconds(value),
                EpochUnit.Millis => DateTimeOffset.FromUnixTimeMilliseconds(value),
                EpochUnit.Micros => DateTimeOffset.UnixEpoch.AddTicks(checked(value * 10)),
                EpochUnit.FileTime => new DateTimeOffset(DateTime.FromFileTimeUtc(value)),
                EpochUnit.Ticks => new DateTimeOffset(new DateTime(value, DateTimeKind.Utc)),
                _ => throw new ArgumentOutOfRangeException(nameof(unit)),
            };
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or OverflowException)
        {
            throw new FormatException($"Timestamp {value} ({unit}) is out of the representable date range.", ex);
        }
    }

    public static EpochValues FromDate(DateTimeOffset date) =>
        new(date.ToUnixTimeSeconds(), date.ToUnixTimeMilliseconds());

    private static readonly DateTime FileTimeEpochUtc = new(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Full multi-format breakdown of a single instant for the CONVERT tab's output
    /// card. <see cref="DateConversion.FileTime"/> is <c>null</c> for instants before the
    /// FILETIME epoch (1601-01-01 UTC), which can't be represented.</summary>
    public static DateConversion Describe(DateTimeOffset date, DateTimeOffset now) => new(
        Seconds: date.ToUnixTimeSeconds(),
        Millis: date.ToUnixTimeMilliseconds(),
        FileTime: date.UtcDateTime >= FileTimeEpochUtc ? date.UtcDateTime.ToFileTimeUtc() : null,
        Ticks: date.UtcDateTime.Ticks,
        LocalIso: date.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture),
        UtcIso: date.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture),
        Rfc1123: date.UtcDateTime.ToString("R", CultureInfo.InvariantCulture),
        Relative: Humanize(date, now));

    /// <summary>Renders a short "3 days ago" / "in 2 h" style relative description.</summary>
    public static string Humanize(DateTimeOffset target, DateTimeOffset now)
    {
        var diff = target - now;
        var abs = diff.Duration();
        if (abs.TotalSeconds < 1)
            return "just now";

        string magnitude = abs.TotalSeconds < 60
            ? $"{(int)abs.TotalSeconds} s"
            : abs.TotalMinutes < 60
                ? $"{(int)abs.TotalMinutes} min"
                : abs.TotalHours < 24
                    ? $"{(int)abs.TotalHours} h"
                    : $"{(int)abs.TotalDays} d";

        return diff > TimeSpan.Zero ? $"in {magnitude}" : $"{magnitude} ago";
    }
}
