using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Delp.Core.Tools.TextProcessing;

public sealed record EpochRow(string Input, string? LocalIso, string? UtcIso, string? Error);

/// <summary>Converts a multi-line block of timestamps (e.g. pasted from a log file) to
/// local/UTC dates, one row per input line.</summary>
public static partial class EpochBatchTool
{
    [GeneratedRegex(@"-?\d+", RegexOptions.None, matchTimeoutMilliseconds: 2000)]
    private static partial Regex IntegerToken();

    /// <summary>Blank lines are skipped. Each remaining line's first integer token is
    /// extracted and converted; lines without one, or with an out-of-range value, become
    /// error rows rather than throwing.</summary>
    public static IReadOnlyList<EpochRow> Convert(string multiline, EpochUnit? forceUnit = null)
    {
        ArgumentNullException.ThrowIfNull(multiline);
        var rows = new List<EpochRow>();

        foreach (var rawLine in multiline.Split(['\r', '\n']))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            var match = IntegerToken().Match(line);
            if (!match.Success)
            {
                rows.Add(new EpochRow(line, null, null, "No integer timestamp found."));
                continue;
            }

            try
            {
                long value;
                EpochUnit unit;
                if (forceUnit is { } fixedUnit)
                {
                    unit = fixedUnit;
                    if (!long.TryParse(match.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value))
                        throw new FormatException($"'{match.Value}' is out of range for a 64-bit timestamp.");
                }
                else
                {
                    (value, unit) = EpochTool.Detect(match.Value);
                }

                var date = EpochTool.ToDate(value, unit);
                var local = date.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
                var utc = date.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture);
                rows.Add(new EpochRow(line, local, utc, null));
            }
            catch (FormatException ex)
            {
                rows.Add(new EpochRow(line, null, null, ex.Message));
            }
        }

        return rows;
    }

    public static string ToCsv(IReadOnlyList<EpochRow> rows)
    {
        var sb = new StringBuilder();
        sb.Append("input,local,utc,error").Append('\n');
        foreach (var row in rows)
        {
            sb.Append(CsvField(row.Input)).Append(',')
              .Append(CsvField(row.LocalIso ?? "")).Append(',')
              .Append(CsvField(row.UtcIso ?? "")).Append(',')
              .Append(CsvField(row.Error ?? ""))
              .Append('\n');
        }
        return sb.ToString();
    }

    private static string CsvField(string value)
    {
        var needsQuoting = value.IndexOfAny([',', '"', '\n', '\r']) >= 0;
        return needsQuoting ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }

    /// <summary>Aligned plain-text table: <c>input → local → utc</c>, one row per line.
    /// Error rows show the error message in place of local/utc.</summary>
    public static string ToTable(IReadOnlyList<EpochRow> rows)
    {
        if (rows.Count == 0)
            return "";

        string Local(EpochRow r) => r.Error is null ? r.LocalIso ?? "" : "ERROR";
        string Utc(EpochRow r) => r.Error is null ? r.UtcIso ?? "" : r.Error;

        var w1 = Math.Max("input".Length, rows.Max(r => r.Input.Length));
        var w2 = Math.Max("local".Length, rows.Max(r => Local(r).Length));

        var sb = new StringBuilder();
        sb.Append("input".PadRight(w1)).Append("  →  ")
          .Append("local".PadRight(w2)).Append("  →  ")
          .Append("utc").Append('\n');
        foreach (var row in rows)
        {
            sb.Append(row.Input.PadRight(w1)).Append("  →  ")
              .Append(Local(row).PadRight(w2)).Append("  →  ")
              .Append(Utc(row)).Append('\n');
        }
        return sb.ToString().TrimEnd('\n');
    }
}
