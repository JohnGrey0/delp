using System.Text.Json.Nodes;
using Json.Path;

namespace Delp.Core.Tools.DataFormat;

/// <summary>Runs a JSONPath query against a JSON document.</summary>
public static class JsonPathTool
{
    public sealed record JsonPathResult(int Count, string ResultJson);

    /// <summary>Evaluates <paramref name="path"/> against <paramref name="json"/> and returns
    /// every match as a pretty-printed JSON array.</summary>
    /// <exception cref="FormatException">The JSON or the path is invalid; the message says which.</exception>
    public static JsonPathResult Query(string json, string path)
    {
        JsonNode? root;
        try
        {
            root = JsonParsing.ParseOrThrow(json);
        }
        catch (FormatException ex)
        {
            throw new FormatException($"Invalid JSON — {ex.Message}");
        }

        JsonPath compiled;
        try
        {
            compiled = JsonPath.Parse(path);
        }
        catch (PathParseException ex)
        {
            throw new FormatException($"Invalid JSONPath at position {ex.Index} — {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new FormatException($"Invalid JSONPath — {ex.Message}");
        }

        var evaluated = compiled.Evaluate(root);

        var matches = new JsonArray();
        foreach (var match in evaluated.Matches)
            matches.Add(match.Value?.DeepClone());

        var resultJson = JsonFormatTool.Format(matches.ToJsonString());
        return new JsonPathResult(evaluated.Matches.Count, resultJson);
    }
}
