using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Delp.Core.Tools.DevUtilities;

public enum FieldKind
{
    FirstName,
    LastName,
    FullName,
    Email,
    Username,
    Phone,
    StreetAddress,
    City,
    State,
    ZipCode,
    Country,
    Company,
    JobTitle,
    Uuid,
    Bool,
    IntRange,
    DecimalRange,
    DateBetween,
    IsoDateTime,
    Ipv4,
    Url,
    HexColor,
    LoremWords,
    Password,
}

/// <summary>One column of the mock schema. <paramref name="Options"/> is only
/// consulted for ranged kinds (IntRange, DecimalRange, DateBetween, LoremWords).</summary>
public sealed record FieldSpec(string Name, FieldKind Kind, string? Options = null);

/// <summary>
/// Generates correlated, seed-deterministic mock data rows and emits them as
/// JSON, CSV, or SQL INSERT statements.
/// </summary>
public static class MockDataTool
{
    /// <summary>Hard ceiling on requested rows so a typo (or a stray extra
    /// zero) can't accidentally trigger a multi-million-row generation.</summary>
    public const int MaxRows = 100_000;

    private static readonly string[] UrlTlds = { "com", "io", "dev", "net", "co", "app" };
    private const string PasswordChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*";

    public static List<Dictionary<string, object?>> Generate(IReadOnlyList<FieldSpec> fields, int rows, int? seed = null)
    {
        if (rows < 0)
            throw new ArgumentException("Row count cannot be negative.", nameof(rows));
        if (rows > MaxRows)
            throw new ArgumentException($"Row count cannot exceed {MaxRows}.", nameof(rows));
        if (fields.Count == 0)
            throw new ArgumentException("Add at least one field.", nameof(fields));

        var rnd = seed.HasValue ? new Random(seed.Value) : new Random();
        var result = new List<Dictionary<string, object?>>(rows);

        for (var r = 0; r < rows; r++)
        {
            // A single "person" is drawn per row so FirstName/LastName/FullName/
            // Email/Username all refer to the same underlying identity.
            var person = new Person(rnd);
            var row = new Dictionary<string, object?>();
            foreach (var field in fields)
                row[field.Name] = GenerateValue(field, person, rnd);
            result.Add(row);
        }

        return result;
    }

    public static string ToJson(IReadOnlyList<Dictionary<string, object?>> rows)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(rows, options);
    }

    public static string ToCsv(IReadOnlyList<Dictionary<string, object?>> rows)
    {
        if (rows.Count == 0)
            return "";

        var columns = rows[0].Keys.ToList();
        var sb = new StringBuilder();

        for (var i = 0; i < columns.Count; i++)
        {
            if (i > 0)
                sb.Append(',');
            AppendCsvField(sb, columns[i]);
        }
        sb.AppendLine();

        foreach (var row in rows)
        {
            for (var i = 0; i < columns.Count; i++)
            {
                if (i > 0)
                    sb.Append(',');
                AppendCsvField(sb, Stringify(row[columns[i]]));
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }

    public static string ToSqlInserts(IReadOnlyList<Dictionary<string, object?>> rows, string table)
    {
        if (rows.Count == 0)
            return "";
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("Table name is required.", nameof(table));

        var columns = rows[0].Keys.ToList();
        var columnClause = new StringBuilder();
        for (var i = 0; i < columns.Count; i++)
        {
            if (i > 0)
                columnClause.Append(", ");
            columnClause.Append('"').Append(columns[i]).Append('"');
        }

        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            sb.Append("INSERT INTO ").Append(table).Append(" (").Append(columnClause).Append(") VALUES (");
            for (var i = 0; i < columns.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                AppendSqlLiteral(sb, row[columns[i]]);
            }
            sb.AppendLine(");");
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }

    private static object? GenerateValue(FieldSpec field, Person person, Random rnd) => field.Kind switch
    {
        FieldKind.FirstName => person.First,
        FieldKind.LastName => person.Last,
        FieldKind.FullName => $"{person.First} {person.Last}",
        FieldKind.Email => person.Email,
        FieldKind.Username => person.Username,
        FieldKind.Phone => MakePhone(rnd),
        FieldKind.StreetAddress => MakeStreetAddress(rnd),
        FieldKind.City => Pick(MockCorpus.Cities, rnd),
        FieldKind.State => Pick(MockCorpus.UsStates, rnd),
        FieldKind.ZipCode => rnd.Next(10000, 100000).ToString(CultureInfo.InvariantCulture),
        FieldKind.Country => Pick(MockCorpus.Countries, rnd),
        FieldKind.Company => MakeCompany(rnd),
        FieldKind.JobTitle => Pick(MockCorpus.JobTitles, rnd),
        FieldKind.Uuid => MakeGuid(rnd),
        FieldKind.Bool => rnd.Next(2) == 1,
        FieldKind.IntRange => MakeIntRange(field, rnd),
        FieldKind.DecimalRange => MakeDecimalRange(field, rnd),
        FieldKind.DateBetween => MakeDateBetween(field, rnd),
        FieldKind.IsoDateTime => MakeIsoDateTime(rnd),
        FieldKind.Ipv4 => FormattableString.Invariant($"{rnd.Next(1, 255)}.{rnd.Next(0, 256)}.{rnd.Next(0, 256)}.{rnd.Next(1, 255)}"),
        FieldKind.Url => MakeUrl(rnd),
        FieldKind.HexColor => $"#{rnd.Next(0, 0x1000000):X6}",
        FieldKind.LoremWords => MakeLoremWords(field, rnd),
        FieldKind.Password => MakePassword(rnd),
        _ => throw new ArgumentOutOfRangeException(nameof(field), field.Kind, "Unknown field kind."),
    };

    private static string MakePhone(Random rnd) =>
        FormattableString.Invariant($"({rnd.Next(200, 1000)}) {rnd.Next(200, 1000)}-{rnd.Next(1000, 10000)}");

    private static string MakeStreetAddress(Random rnd) =>
        FormattableString.Invariant($"{rnd.Next(1, 9999)} {Pick(MockCorpus.StreetNames, rnd)} {Pick(MockCorpus.StreetTypes, rnd)}");

    private static string MakeCompany(Random rnd)
    {
        var pattern = Pick(MockCorpus.CompanyPatterns, rnd);
        var noun = Pick(MockCorpus.CompanyNouns, rnd);
        return string.Format(CultureInfo.InvariantCulture, pattern, noun);
    }

    private static string MakeUrl(Random rnd)
    {
        var noun = Pick(MockCorpus.CompanyNouns, rnd).ToLowerInvariant();
        var tld = Pick(UrlTlds, rnd);
        var path = Pick(MockCorpus.LoremWords, rnd);
        return $"https://{noun}.{tld}/{path}";
    }

    private static string MakeGuid(Random rnd)
    {
        var bytes = new byte[16];
        rnd.NextBytes(bytes);
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x40); // version 4 nibble
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // RFC variant
        return new Guid(bytes).ToString();
    }

    private static string MakePassword(Random rnd)
    {
        var buf = new char[12];
        for (var i = 0; i < buf.Length; i++)
            buf[i] = PasswordChars[rnd.Next(PasswordChars.Length)];
        return new string(buf);
    }

    private static int MakeIntRange(FieldSpec field, Random rnd)
    {
        var (minText, maxText) = SplitRange(field, "0", "100");
        if (!int.TryParse(minText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var min) ||
            !int.TryParse(maxText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var max))
            throw new FormatException($"Field '{field.Name}': invalid integer range '{field.Options}' — expected 'min..max'.");
        if (min > max)
            (min, max) = (max, min);
        return rnd.Next(min, max + 1);
    }

    private static double MakeDecimalRange(FieldSpec field, Random rnd)
    {
        var decimals = 2;
        var minText = "0";
        var maxText = "100";
        if (!string.IsNullOrWhiteSpace(field.Options))
        {
            var parts = field.Options.Split("..", StringSplitOptions.None);
            if (parts.Length < 2)
                throw new FormatException($"Field '{field.Name}': invalid decimal range '{field.Options}' — expected 'min..max' or 'min..max..decimals'.");
            minText = parts[0].Trim();
            maxText = parts[1].Trim();
            if (parts.Length >= 3 && !int.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out decimals))
                throw new FormatException($"Field '{field.Name}': invalid decimals count in '{field.Options}'.");
        }

        if (!double.TryParse(minText, NumberStyles.Float, CultureInfo.InvariantCulture, out var min) ||
            !double.TryParse(maxText, NumberStyles.Float, CultureInfo.InvariantCulture, out var max))
            throw new FormatException($"Field '{field.Name}': invalid decimal range '{field.Options}'.");
        if (min > max)
            (min, max) = (max, min);

        var value = min + rnd.NextDouble() * (max - min);
        return Math.Round(value, Math.Clamp(decimals, 0, 15));
    }

    private static string MakeDateBetween(FieldSpec field, Random rnd)
    {
        var from = new DateTime(2020, 1, 1);
        var to = new DateTime(2025, 12, 31);
        if (!string.IsNullOrWhiteSpace(field.Options))
        {
            var parts = field.Options.Split("..", StringSplitOptions.None);
            if (parts.Length != 2 ||
                !DateTime.TryParseExact(parts[0].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out from) ||
                !DateTime.TryParseExact(parts[1].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out to))
                throw new FormatException($"Field '{field.Name}': invalid date range '{field.Options}' — expected 'yyyy-MM-dd..yyyy-MM-dd'.");
        }

        if (from > to)
            (from, to) = (to, from);
        var span = (to - from).Days;
        var offset = span <= 0 ? 0 : rnd.Next(span + 1);
        return from.AddDays(offset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string MakeIsoDateTime(Random rnd)
    {
        var from = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var days = Math.Max((DateTime.UtcNow - from).Days, 1);
        var dt = from.AddDays(rnd.Next(days)).AddSeconds(rnd.Next(86400));
        return dt.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }

    private static string MakeLoremWords(FieldSpec field, Random rnd)
    {
        var count = 6;
        if (!string.IsNullOrWhiteSpace(field.Options) &&
            int.TryParse(field.Options.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) &&
            parsed > 0)
            count = parsed;

        var words = new string[count];
        for (var i = 0; i < count; i++)
            words[i] = Pick(MockCorpus.LoremWords, rnd);
        return string.Join(' ', words);
    }

    private static (string Min, string Max) SplitRange(FieldSpec field, string defaultMin, string defaultMax)
    {
        if (string.IsNullOrWhiteSpace(field.Options))
            return (defaultMin, defaultMax);
        var parts = field.Options.Split("..", StringSplitOptions.None);
        if (parts.Length < 2)
            throw new FormatException($"Field '{field.Name}': invalid range '{field.Options}' — expected 'min..max'.");
        return (parts[0].Trim(), parts[1].Trim());
    }

    private static string Pick(IReadOnlyList<string> pool, Random rnd) => pool[rnd.Next(pool.Count)];

    private static string? Stringify(object? value) => value switch
    {
        null => null,
        bool b => b ? "true" : "false",
        double d => d.ToString(CultureInfo.InvariantCulture),
        int i => i.ToString(CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    private static readonly char[] CsvSpecialChars = { ',', '"', '\n', '\r' };

    private static void AppendCsvField(StringBuilder sb, string? value)
    {
        value ??= "";
        if (value.IndexOfAny(CsvSpecialChars) < 0)
        {
            sb.Append(value);
            return;
        }

        sb.Append('"');
        foreach (var ch in value)
        {
            if (ch == '"')
                sb.Append('"');
            sb.Append(ch);
        }
        sb.Append('"');
    }

    private static void AppendSqlLiteral(StringBuilder sb, object? value)
    {
        switch (value)
        {
            case null:
                sb.Append("NULL");
                return;
            case bool b:
                sb.Append(b ? "TRUE" : "FALSE");
                return;
            case int i:
                sb.Append(i.ToString(CultureInfo.InvariantCulture));
                return;
            case double d:
                sb.Append(d.ToString(CultureInfo.InvariantCulture));
                return;
            default:
                sb.Append('\'');
                foreach (var ch in value.ToString()!)
                {
                    if (ch == '\'')
                        sb.Append('\'');
                    sb.Append(ch);
                }
                sb.Append('\'');
                return;
        }
    }

    private sealed class Person
    {
        public string First { get; }
        public string Last { get; }
        public string Email { get; }
        public string Username { get; }

        public Person(Random rnd)
        {
            First = Pick(MockCorpus.FirstNames, rnd);
            Last = Pick(MockCorpus.LastNames, rnd);
            var domain = Pick(MockCorpus.EmailDomains, rnd);
            Email = $"{First.ToLowerInvariant()}.{Last.ToLowerInvariant()}@{domain}";
            var suffix = rnd.Next(1, 100);
            Username = FormattableString.Invariant($"{First.ToLowerInvariant()}{Last.ToLowerInvariant()[..1]}{suffix}");
        }
    }
}
