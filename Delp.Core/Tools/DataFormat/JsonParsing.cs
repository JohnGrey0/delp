using System.Text.Json;
using System.Text.Json.Nodes;

namespace Delp.Core.Tools.DataFormat;

/// <summary>
/// Strict JSON parsing shared by every JSON-consuming tool in this category
/// (json-format, jsonpath, json-yaml, toml-parse), so line/col error reporting
/// and duplicate-key handling behave identically everywhere.
/// </summary>
internal static class JsonParsing
{
    /// <summary>Trailing commas and comments are rejected — this is strict JSON.</summary>
    public static readonly JsonDocumentOptions Strict = new()
    {
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
    };

    /// <summary>Parses JSON, throwing a human-readable <see cref="FormatException"/> with a
    /// 1-based "Line X, Col Y: message" prefix on failure.</summary>
    public static JsonNode? ParseOrThrow(string json)
    {
        try
        {
            var node = JsonNode.Parse(json, documentOptions: Strict);
            ForceRealize(node); // JsonObject builds its dictionary lazily — force it now so a
                                 // duplicate-key ArgumentException surfaces here, not later.
            return node;
        }
        catch (JsonException ex)
        {
            throw new FormatException(FormatJsonException(ex));
        }
        catch (ArgumentException ex) when (IsDuplicateKey(ex))
        {
            throw new FormatException($"Duplicate key in JSON object: {ExtractDuplicateKey(ex)}");
        }
    }

    /// <summary>Parses for validation purposes; returns the error location/message instead of throwing.</summary>
    public static (int Line, int Col, string Message)? TryGetError(string json)
    {
        try
        {
            var node = JsonNode.Parse(json, documentOptions: Strict);
            ForceRealize(node);
            return null;
        }
        catch (JsonException ex)
        {
            return ((int)(ex.LineNumber ?? 0) + 1, (int)(ex.BytePositionInLine ?? 0) + 1, CleanMessage(ex.Message));
        }
        catch (ArgumentException ex) when (IsDuplicateKey(ex))
        {
            return (1, 1, $"Duplicate key in JSON object: {ExtractDuplicateKey(ex)}");
        }
    }

    /// <summary>Recursively touches every object/array so lazily-built internal dictionaries
    /// (and their duplicate-key checks) run eagerly, at parse time.</summary>
    private static void ForceRealize(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var kv in obj)
                    ForceRealize(kv.Value);
                break;
            case JsonArray arr:
                foreach (var item in arr)
                    ForceRealize(item);
                break;
        }
    }

    private static bool IsDuplicateKey(ArgumentException ex) =>
        ex.Message.Contains("same key", StringComparison.OrdinalIgnoreCase);

    private static string ExtractDuplicateKey(ArgumentException ex)
    {
        var message = ex.Message;
        var idx = message.IndexOf("Key: ", StringComparison.Ordinal);
        if (idx < 0)
            return message;
        var rest = message[(idx + 5)..];
        var paren = rest.IndexOf(" (Parameter", StringComparison.Ordinal);
        return paren >= 0 ? rest[..paren] : rest;
    }

    private static string FormatJsonException(JsonException ex)
    {
        var line = (int)(ex.LineNumber ?? 0) + 1;
        var col = (int)(ex.BytePositionInLine ?? 0) + 1;
        return $"Line {line}, Col {col}: {CleanMessage(ex.Message)}";
    }

    /// <summary>Strips the " LineNumber: X | BytePositionInLine: Y." suffix System.Text.Json
    /// appends — we already surface that as a proper Line/Col prefix.</summary>
    private static string CleanMessage(string message)
    {
        var idx = message.IndexOf(" LineNumber:", StringComparison.Ordinal);
        return idx >= 0 ? message[..idx] : message;
    }
}
