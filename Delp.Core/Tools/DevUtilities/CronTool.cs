using Cronos;
using CronExpressionDescriptor;

namespace Delp.Core.Tools.DevUtilities;

/// <summary>One row of the per-field breakdown table: the raw token and a hand-written plain-English meaning.</summary>
public sealed record CronFieldInfo(string Field, string Value, string Meaning);

/// <summary>Full explanation of a cron expression: human summary, next occurrences, and a per-field breakdown.</summary>
public sealed record CronReport(
    string Human,
    IReadOnlyList<DateTime> NextLocal,
    IReadOnlyList<CronFieldInfo> Fields,
    bool HasSeconds);

/// <summary>
/// Parses standard 5-field cron and Quartz-style 6-field (leading seconds) cron expressions: a human-readable
/// description (CronExpressionDescriptor), the next occurrences (Cronos), and a per-field breakdown (hand-written).
/// </summary>
public static class CronTool
{
    private const int OccurrenceCount = 10;

    /// <summary>
    /// Explains <paramref name="expression"/>. <paramref name="fromUtc"/> pins the origin instant for computing
    /// the next occurrences (defaults to <see cref="DateTime.UtcNow"/>) so callers — including tests — can get
    /// deterministic results.
    /// </summary>
    /// <exception cref="FormatException">The expression is empty, has the wrong field count, or is malformed.</exception>
    public static CronReport Explain(string expression, DateTime? fromUtc = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new FormatException("Enter a cron expression.");

        var parts = expression.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is not (5 or 6))
        {
            throw new FormatException(
                $"A cron expression needs 5 fields (standard) or 6 fields (leading seconds field, Quartz-style); found {parts.Length}.");
        }

        var hasSeconds = parts.Length == 6;
        var format = hasSeconds ? CronFormat.IncludeSeconds : CronFormat.Standard;

        CronExpression cron;
        try
        {
            cron = CronExpression.Parse(expression, format);
        }
        catch (CronFormatException ex)
        {
            throw new FormatException($"Invalid cron expression: {ex.Message}", ex);
        }

        string human;
        try
        {
            human = ExpressionDescriptor.GetDescription(expression);
        }
        catch (Exception)
        {
            human = "(no human-readable description available)";
        }

        var from = DateTime.SpecifyKind(fromUtc ?? DateTime.UtcNow, DateTimeKind.Utc);

        var next = new List<DateTime>(OccurrenceCount);
        var cursor = from;
        for (var i = 0; i < OccurrenceCount; i++)
        {
            var occurrence = cron.GetNextOccurrence(cursor, inclusive: false);
            if (occurrence is null)
                break;
            next.Add(occurrence.Value.ToLocalTime());
            cursor = occurrence.Value;
        }

        return new CronReport(human, next, DescribeFields(parts, hasSeconds), hasSeconds);
    }

    private static IReadOnlyList<CronFieldInfo> DescribeFields(string[] parts, bool hasSeconds)
    {
        var labels = hasSeconds
            ? new[] { "Second", "Minute", "Hour", "Day of month", "Month", "Day of week" }
            : new[] { "Minute", "Hour", "Day of month", "Month", "Day of week" };

        var result = new List<CronFieldInfo>(parts.Length);
        for (var i = 0; i < parts.Length; i++)
            result.Add(new CronFieldInfo(labels[i], parts[i], DescribeField(labels[i], parts[i])));
        return result;
    }

    private static readonly string[] MonthNames =
    {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December",
    };

    private static readonly string[] DayNames =
    {
        "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday",
    };

    private static string UnitFor(string label) => label switch
    {
        "Second" => "second",
        "Minute" => "minute",
        "Hour" => "hour",
        "Day of month" => "day of the month",
        "Month" => "month",
        "Day of week" => "day of the week",
        _ => label.ToLowerInvariant(),
    };

    private static Func<string, string>? NamerFor(string label) => label switch
    {
        "Month" => NameForMonth,
        "Day of week" => NameForDayOfWeek,
        _ => null,
    };

    private static string DescribeField(string label, string token)
    {
        var unit = UnitFor(label);
        var namer = NamerFor(label);
        var pieces = token.Split(',');
        return string.Join(", ", pieces.Select(p => DescribePiece(p, unit, namer)));
    }

    private static string DescribePiece(string piece, string unit, Func<string, string>? namer)
    {
        var slash = piece.IndexOf('/');
        if (slash >= 0)
        {
            var basePart = piece[..slash];
            var step = piece[(slash + 1)..];
            return basePart == "*"
                ? $"every {step} {unit}(s)"
                : $"{DescribeBase(basePart, namer)}, every {step} {unit}(s)";
        }

        return piece == "*" ? $"every {unit}" : DescribeBase(piece, namer);
    }

    private static string DescribeBase(string basePart, Func<string, string>? namer)
    {
        if (basePart is "*" or "?")
            return "any";
        if (basePart.Contains('-'))
        {
            var range = basePart.Split('-', 2);
            return $"{Name(range[0], namer)} through {Name(range[1], namer)}";
        }
        return Name(basePart, namer);
    }

    private static string Name(string token, Func<string, string>? namer) => namer is null ? token : namer(token);

    private static string NameForMonth(string token) =>
        int.TryParse(token, out var m) && m is >= 1 and <= 12 ? MonthNames[m - 1] : token;

    private static string NameForDayOfWeek(string token) =>
        int.TryParse(token, out var d) && d is >= 0 and <= 7 ? DayNames[d % 7] : token;
}
