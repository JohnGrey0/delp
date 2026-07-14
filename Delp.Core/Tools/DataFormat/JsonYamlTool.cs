using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Delp.Core.Tools.DataFormat;

/// <summary>Converts documents between JSON and YAML, preserving scalar types both ways.</summary>
public static class JsonYamlTool
{
    /// <exception cref="FormatException">The input is not valid JSON.</exception>
    public static string JsonToYaml(string json)
    {
        var root = JsonParsing.ParseOrThrow(json);
        var graph = JsonToGraph(root);

        var valueSerializer = new SerializerBuilder()
            .WithIndentedSequences()
            .WithQuotingNecessaryStrings(true)
            .BuildValueSerializer();
        var serializer = Serializer.FromValueSerializer(
            valueSerializer, EmitterSettings.Default.WithBestIndent(2).WithNewLine("\n"));

        var text = serializer.Serialize(graph);
        return string.IsNullOrEmpty(text) ? "" : text.TrimEnd('\r', '\n') + "\n";
    }

    /// <exception cref="FormatException">The input is not well-formed YAML, or contains more
    /// than one document.</exception>
    public static string YamlToJson(string yaml)
    {
        var stream = YamlParsing.ParseOrThrow(yaml);

        if (stream.Documents.Count == 0)
            return JsonFormatTool.Format("null");
        if (stream.Documents.Count > 1)
            throw new FormatException(
                "Multiple YAML documents are not supported by this converter — " +
                "use the YAML Formatter tool, or convert one document at a time.");

        var graph = YamlGraphHelper.ToGraph(stream.Documents[0].RootNode);
        var node = GraphToJsonNode(graph);
        return JsonFormatTool.Format(node is null ? "null" : node.ToJsonString());
    }

    private static object? JsonToGraph(JsonNode? node) => node switch
    {
        null => null,
        JsonObject obj => obj.ToDictionary(kv => kv.Key, kv => JsonToGraph(kv.Value)),
        JsonArray arr => arr.Select(JsonToGraph).ToList(),
        JsonValue val => JsonScalarToObject(val),
        _ => null,
    };

    private static object? JsonScalarToObject(JsonValue val)
    {
        if (val.TryGetValue<JsonElement>(out var el))
        {
            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDouble(),
                _ => null,
            };
        }
        if (val.TryGetValue<string>(out var s))
            return s;
        if (val.TryGetValue<bool>(out var b))
            return b;
        if (val.TryGetValue<double>(out var d))
            return d;
        if (val.TryGetValue<long>(out var lv))
            return lv;
        return val.ToJsonString();
    }

    private static JsonNode? GraphToJsonNode(object? value) => value switch
    {
        null => null,
        string s => JsonValue.Create(s),
        bool b => JsonValue.Create(b),
        long l => JsonValue.Create(l),
        double d => JsonValue.Create(d),
        Dictionary<string, object?> map => ToJsonObject(map),
        List<object?> list => ToJsonArray(list),
        _ => JsonValue.Create(value.ToString()),
    };

    private static JsonObject ToJsonObject(Dictionary<string, object?> map)
    {
        var obj = new JsonObject();
        foreach (var kv in map)
            obj[kv.Key] = GraphToJsonNode(kv.Value);
        return obj;
    }

    private static JsonArray ToJsonArray(List<object?> list)
    {
        var arr = new JsonArray();
        foreach (var item in list)
            arr.Add(GraphToJsonNode(item));
        return arr;
    }
}
