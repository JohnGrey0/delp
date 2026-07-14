using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using CsvHelper;
using CsvHelper.Configuration;

namespace Delp.Core.Tools.DataFormat;

/// <summary>Options for <see cref="CsvJsonTool.CsvToJson"/>.</summary>
/// <param name="Delimiter">Field delimiter; null auto-detects among comma, semicolon, tab and pipe.</param>
/// <param name="HasHeader">Whether the first row holds column names (otherwise columns are named col1..colN).</param>
/// <param name="InferTypes">Parse numeric/boolean/empty fields into JSON int, double, bool and null; otherwise every field is a JSON string.</param>
public sealed record CsvOptions(char? Delimiter = null, bool HasHeader = true, bool InferTypes = true);

/// <summary>Converts between CSV and JSON via CsvHelper, with delimiter auto-detection and type inference.</summary>
public static class CsvJsonTool
{
    public static string CsvToJson(string csv, CsvOptions options)
    {
        ArgumentNullException.ThrowIfNull(csv);
        ArgumentNullException.ThrowIfNull(options);

        csv = StripBom(csv);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false, // header handled manually below for accurate per-row error reporting
            DetectDelimiter = options.Delimiter is null,
            Delimiter = options.Delimiter is { } d ? d.ToString() : ",",
            BadDataFound = null,
            MissingFieldFound = null,
            IgnoreBlankLines = true,
        };

        using var textReader = new StringReader(csv);
        using var csvReader = new CsvReader(textReader, config);

        var result = new JsonArray();
        string[]? columnNames = null;
        int? columnCount = null;
        bool consumedHeader = false;

        while (csvReader.Read())
        {
            var record = csvReader.Parser.Record;
            if (record is null || record.Length == 0)
                continue;

            if (options.HasHeader && !consumedHeader)
            {
                columnNames = record;
                columnCount = record.Length;
                consumedHeader = true;
                continue;
            }

            columnCount ??= record.Length;
            if (record.Length != columnCount)
                throw new FormatException($"Row {csvReader.Parser.Row} has {record.Length} field(s); expected {columnCount}.");

            columnNames ??= Enumerable.Range(1, columnCount.Value).Select(i => $"col{i}").ToArray();

            var obj = new JsonObject();
            for (int i = 0; i < columnCount; i++)
            {
                var name = i < columnNames.Length ? columnNames[i] : $"col{i + 1}";
                obj[name] = options.InferTypes ? InferJsonValue(record[i]) : JsonValue.Create(record[i]);
            }
            result.Add(obj);
        }

        return DataFormatUtil.NormalizeNewLines(result.ToJsonString(DataFormatUtil.JsonWriteOptions));
    }

    public static string JsonToCsv(string json, char delimiter)
    {
        ArgumentNullException.ThrowIfNull(json);

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Invalid JSON: {ex.Message}", ex);
        }

        if (node is not JsonArray array)
            throw new FormatException("JSON root must be an array of objects.");

        var columns = new List<string>();
        var seen = new HashSet<string>();
        var rows = new List<JsonObject>();
        foreach (var item in array)
        {
            if (item is not JsonObject obj)
                throw new FormatException("Each array item must be a JSON object.");
            rows.Add(obj);
            foreach (var key in obj.Select(kv => kv.Key))
                if (seen.Add(key))
                    columns.Add(key);
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = delimiter.ToString() };
        using var sw = new StringWriter();
        using (var csvWriter = new CsvWriter(sw, config))
        {
            foreach (var col in columns)
                csvWriter.WriteField(col);
            csvWriter.NextRecord();

            foreach (var row in rows)
            {
                foreach (var col in columns)
                    csvWriter.WriteField(CellToString(row[col]));
                csvWriter.NextRecord();
            }
        }

        return sw.ToString();
    }

    private static JsonNode? InferJsonValue(string field)
    {
        if (string.IsNullOrEmpty(field)) return null;
        if (int.TryParse(field, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return JsonValue.Create(i);
        if (double.TryParse(field, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return JsonValue.Create(d);
        if (bool.TryParse(field, out var b)) return JsonValue.Create(b);
        return JsonValue.Create(field);
    }

    private static string CellToString(JsonNode? value) => value switch
    {
        null => "",
        JsonValue v when v.TryGetValue<string>(out var s) => s,
        _ => value.ToJsonString(),
    };

    private static string StripBom(string text) =>
        text.Length > 0 && text[0] == '\ufeff' ? text[1..] : text;
}
