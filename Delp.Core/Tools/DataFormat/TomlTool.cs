using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tomlyn;
using Tomlyn.Model;

namespace Delp.Core.Tools.DataFormat;

/// <summary>Converts documents between TOML and JSON, and validates TOML.</summary>
public static class TomlTool
{
    public sealed record TomlError(int Line, int Col, string Message);

    /// <exception cref="FormatException">The input is not valid TOML.</exception>
    public static string TomlToJson(string toml)
    {
        var table = ParseOrThrow(toml);
        var node = ValueToJson(table);
        return JsonFormatTool.Format(node is null ? "null" : node.ToJsonString());
    }

    /// <summary>Returns null when valid, otherwise the first diagnostic's location and message.</summary>
    public static TomlError? Validate(string toml)
    {
        try
        {
            TomlSerializer.Deserialize<TomlTable>(toml);
            return null;
        }
        catch (TomlException ex)
        {
            return new TomlError(ex.Line ?? 1, ex.Column ?? 1, FirstDiagnosticMessage(ex));
        }
    }

    /// <summary>The JSON root must be an object — TOML documents are always tables.</summary>
    /// <exception cref="FormatException">The input is not valid JSON, the root isn't an object,
    /// or it contains a null (TOML has no null).</exception>
    public static string JsonToToml(string json)
    {
        var root = JsonParsing.ParseOrThrow(json);
        if (root is not JsonObject rootObject)
            throw new FormatException("The JSON root must be an object to convert to TOML.");

        var table = ConvertObject(rootObject);
        return TomlSerializer.Serialize(table);
    }

    private static TomlTable ParseOrThrow(string toml)
    {
        try
        {
            return TomlSerializer.Deserialize<TomlTable>(toml) ?? new TomlTable();
        }
        catch (TomlException ex)
        {
            throw new FormatException($"Line {ex.Line ?? 1}, Col {ex.Column ?? 1}: {FirstDiagnosticMessage(ex)}");
        }
    }

    private static string FirstDiagnosticMessage(TomlException ex) =>
        ex.Diagnostics is { Count: > 0 } ? ex.Diagnostics[0].Message : ex.Message;

    private static JsonNode? ValueToJson(object? value) => value switch
    {
        null => null,
        string s => JsonValue.Create(s),
        bool b => JsonValue.Create(b),
        long l => JsonValue.Create(l),
        double d => JsonValue.Create(d),
        TomlDateTime dt => JsonValue.Create(FormatDateTime(dt)),
        TomlTable table => TableToJson(table),
        TomlTableArray tableArray => TableArrayToJson(tableArray),
        TomlArray array => ArrayToJson(array),
        _ => JsonValue.Create(value.ToString()),
    };

    private static JsonObject TableToJson(TomlTable table)
    {
        var obj = new JsonObject();
        foreach (var key in table.Keys)
            obj[key] = ValueToJson(table[key]);
        return obj;
    }

    private static JsonArray ArrayToJson(TomlArray array)
    {
        var arr = new JsonArray();
        foreach (var item in array)
            arr.Add(ValueToJson(item));
        return arr;
    }

    private static JsonArray TableArrayToJson(TomlTableArray tableArray)
    {
        var arr = new JsonArray();
        foreach (var table in tableArray)
            arr.Add(TableToJson(table));
        return arr;
    }

    private static string FormatDateTime(TomlDateTime dt) => dt.Kind switch
    {
        TomlDateTimeKind.LocalDate => dt.DateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        TomlDateTimeKind.LocalTime => dt.DateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
        TomlDateTimeKind.LocalDateTime => dt.DateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
        _ => dt.DateTime.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture),
    };

    private static object ConvertNode(JsonNode? node) => node switch
    {
        null => throw new FormatException("TOML has no null — remove null values before converting."),
        JsonObject obj => ConvertObject(obj),
        JsonArray arr => ConvertArray(arr),
        JsonValue val => ConvertValue(val),
        _ => throw new FormatException("Unsupported JSON value."),
    };

    private static TomlTable ConvertObject(JsonObject obj)
    {
        var table = new TomlTable();
        foreach (var kv in obj)
            table[kv.Key] = ConvertNode(kv.Value);
        return table;
    }

    private static object ConvertArray(JsonArray arr)
    {
        // Represent an array whose every element is an object as a TOML array-of-tables
        // ([[items]]) — the idiomatic, readable form — rather than an inline array of tables.
        if (arr.Count > 0 && arr.All(e => e is JsonObject))
        {
            var tableArray = new TomlTableArray();
            foreach (var e in arr)
                tableArray.Add(ConvertObject((JsonObject)e!));
            return tableArray;
        }

        var array = new TomlArray();
        foreach (var e in arr)
            array.Add(ConvertNode(e));
        return array;
    }

    private static object ConvertValue(JsonValue val)
    {
        if (val.TryGetValue<JsonElement>(out var el))
        {
            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString() ?? "",
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDouble(),
                _ => throw new FormatException("Unsupported JSON value."),
            };
        }
        if (val.TryGetValue<string>(out var s))
            return s;
        if (val.TryGetValue<bool>(out var b))
            return b;
        if (val.TryGetValue<long>(out var lv))
            return lv;
        if (val.TryGetValue<double>(out var d))
            return d;
        throw new FormatException("Unsupported JSON value.");
    }
}
