using System.Globalization;

namespace Delp.Core.Tools.TextProcessing;

public enum EpochUnit
{
    Seconds,
    Millis,
    Micros,
}

public sealed record EpochValues(long Seconds, long Millis);

/// <summary>Unix timestamp ↔ date conversions, with a length-based unit heuristic for
/// pasted timestamps of unknown unit.</summary>
public static class EpochTool
{
    /// <summary>Guesses the unit from digit count: ≤10 digits → seconds, 11-13 → millis,
    /// 14+ → micros. Underscores/spaces and a leading sign are tolerated.</summary>
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

        var unit = digits.Length switch
        {
            <= 10 => EpochUnit.Seconds,
            <= 13 => EpochUnit.Millis,
            _ => EpochUnit.Micros,
        };

        if (!long.TryParse(trimmed, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
            throw new FormatException($"'{input}' is out of range for a 64-bit timestamp.");

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
