using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Delp.Core.Tools.DataFormat;

/// <summary>Re-emits YAML in canonical block style with a chosen indent width, and validates it.</summary>
public static class YamlFormatTool
{
    public sealed record YamlError(int Line, int Col, string Message);

    /// <summary>Formats every document in the stream, preserving scalar types
    /// (comments are not preserved — YAML's data model has no place to keep them).</summary>
    /// <exception cref="FormatException">The input is not well-formed YAML.</exception>
    public static string Format(string yaml, int indent = 2)
    {
        var stream = YamlParsing.ParseOrThrow(yaml);
        if (stream.Documents.Count == 0)
            return "";

        var valueSerializer = new SerializerBuilder()
            .WithIndentedSequences()
            .WithQuotingNecessaryStrings(true)
            .BuildValueSerializer();
        var serializer = Serializer.FromValueSerializer(
            valueSerializer, EmitterSettings.Default.WithBestIndent(Math.Clamp(indent, 1, 8)).WithNewLine("\n"));

        var multiDoc = stream.Documents.Count > 1;
        var sb = new StringBuilder();
        foreach (var document in stream.Documents)
        {
            if (multiDoc)
                sb.Append("---\n");
            var graph = YamlGraphHelper.ToGraph(document.RootNode);
            var text = serializer.Serialize(graph);
            if (!string.IsNullOrEmpty(text))
                sb.Append(text.TrimEnd('\r', '\n')).Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>Returns null when valid, otherwise the first error's location and message.</summary>
    public static YamlError? Validate(string yaml)
    {
        var error = YamlParsing.TryGetError(yaml);
        return error is null ? null : new YamlError(error.Value.Line, error.Value.Col, error.Value.Message);
    }
}
