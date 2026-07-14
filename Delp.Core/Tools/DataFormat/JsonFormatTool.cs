using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Delp.Core.Tools.DataFormat;

/// <summary>Formats, minifies, and validates JSON with precise line/column error reporting.</summary>
public static class JsonFormatTool
{
    public sealed record JsonFormatOptions(int IndentSize = 2, bool UseTabs = false, bool SortKeys = false, bool EscapeNonAscii = false);

    public sealed record JsonError(int Line, int Col, string Message);

    /// <summary>Pretty-prints JSON with the given options. Numbers are emitted exactly as
    /// written in the source; everything else is re-serialized from a normalized tree.</summary>
    /// <exception cref="FormatException">The input is not valid JSON.</exception>
    public static string Format(string json, JsonFormatOptions? options = null) =>
        FormatNode(JsonParsing.ParseOrThrow(json), options);

    /// <summary>Removes all insignificant whitespace.</summary>
    /// <exception cref="FormatException">The input is not valid JSON.</exception>
    public static string Minify(string json)
    {
        var node = JsonParsing.ParseOrThrow(json);
        var sb = new StringBuilder();
        WriteNode(node, sb, 0, new JsonFormatOptions(), pretty: false);
        return sb.ToString();
    }

    /// <summary>Pretty-prints an already-parsed <see cref="JsonNode"/> tree directly, skipping
    /// the parse/duplicate-key-check pass — for callers that built the tree in memory (jsonpath,
    /// json-yaml, toml-parse) and would otherwise pay for a redundant full re-parse of their own
    /// freshly-serialized output.</summary>
    internal static string FormatNode(JsonNode? node, JsonFormatOptions? options = null)
    {
        options ??= new JsonFormatOptions();
        var sb = new StringBuilder();
        WriteNode(node, sb, 0, options, pretty: true);
        return sb.ToString();
    }

    /// <summary>Returns null when valid, otherwise the first error's location and message.</summary>
    public static JsonError? Validate(string json)
    {
        var error = JsonParsing.TryGetError(json);
        return error is null ? null : new JsonError(error.Value.Line, error.Value.Col, error.Value.Message);
    }

    private static void WriteNode(JsonNode? node, StringBuilder sb, int depth, JsonFormatOptions o, bool pretty)
    {
        switch (node)
        {
            case null:
                sb.Append("null");
                break;
            case JsonArray array:
                WriteArray(array, sb, depth, o, pretty);
                break;
            case JsonObject obj:
                WriteObject(obj, sb, depth, o, pretty);
                break;
            case JsonValue value:
                WriteScalar(value, sb, o);
                break;
        }
    }

    private static void WriteObject(JsonObject obj, StringBuilder sb, int depth, JsonFormatOptions o, bool pretty)
    {
        if (obj.Count == 0)
        {
            sb.Append("{}");
            return;
        }

        IEnumerable<KeyValuePair<string, JsonNode?>> entries = obj;
        if (o.SortKeys)
            entries = entries.OrderBy(e => e.Key, StringComparer.Ordinal);
        var list = entries.ToList();

        sb.Append('{');
        if (pretty)
            sb.Append('\n');
        for (var i = 0; i < list.Count; i++)
        {
            if (pretty)
                sb.Append(Indent(depth + 1, o));
            WriteJsonString(list[i].Key, sb, o.EscapeNonAscii);
            sb.Append(pretty ? ": " : ":");
            WriteNode(list[i].Value, sb, depth + 1, o, pretty);
            if (i < list.Count - 1)
                sb.Append(',');
            if (pretty)
                sb.Append('\n');
        }
        if (pretty)
            sb.Append(Indent(depth, o));
        sb.Append('}');
    }

    private static void WriteArray(JsonArray array, StringBuilder sb, int depth, JsonFormatOptions o, bool pretty)
    {
        if (array.Count == 0)
        {
            sb.Append("[]");
            return;
        }

        sb.Append('[');
        if (pretty)
            sb.Append('\n');
        for (var i = 0; i < array.Count; i++)
        {
            if (pretty)
                sb.Append(Indent(depth + 1, o));
            WriteNode(array[i], sb, depth + 1, o, pretty);
            if (i < array.Count - 1)
                sb.Append(',');
            if (pretty)
                sb.Append('\n');
        }
        if (pretty)
            sb.Append(Indent(depth, o));
        sb.Append(']');
    }

    private static void WriteScalar(JsonValue value, StringBuilder sb, JsonFormatOptions o)
    {
        // Values parsed from source text are backed by a JsonElement — use its raw text so
        // numbers are preserved exactly as written (1.50, 1e10, huge integers, ...).
        if (value.TryGetValue<JsonElement>(out var element))
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    WriteJsonString(element.GetString() ?? "", sb, o.EscapeNonAscii);
                    return;
                case JsonValueKind.Number:
                    sb.Append(element.GetRawText());
                    return;
                case JsonValueKind.True:
                    sb.Append("true");
                    return;
                case JsonValueKind.False:
                    sb.Append("false");
                    return;
            }
        }

        // Fallback for values constructed in memory (e.g. by JsonPathTool's result array).
        if (value.TryGetValue<string>(out var s))
        {
            WriteJsonString(s, sb, o.EscapeNonAscii);
            return;
        }
        if (value.TryGetValue<bool>(out var b))
        {
            sb.Append(b ? "true" : "false");
            return;
        }
        sb.Append(value.ToJsonString());
    }

    private static void WriteJsonString(string s, StringBuilder sb, bool escapeNonAscii)
    {
        sb.Append('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (c < 0x20 || (escapeNonAscii && c > 0x7E))
                        sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }

    private static string Indent(int depth, JsonFormatOptions o) =>
        o.UseTabs ? new string('\t', depth) : new string(' ', Math.Max(0, o.IndentSize) * depth);
}
