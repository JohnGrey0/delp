using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Delp.Core.Tools.DataFormat;

/// <summary>
/// Shared YAML emission settings for yaml-format and json-yaml (block-style sequences,
/// quote only strings that need it). <see cref="SerializerBuilder"/> itself is a mutable,
/// non-thread-safe builder and must never be cached — but the <see cref="IValueSerializer"/>
/// it produces is immutable and safe to reuse across calls and threads, so we build it exactly
/// once instead of paying SerializerBuilder's setup cost (type inspection, converter wiring,
/// naming conventions) on every keystroke of a live-debounced conversion.
/// </summary>
internal static class YamlSerializing
{
    private static readonly IValueSerializer ValueSerializer = new SerializerBuilder()
        .WithIndentedSequences()
        .WithQuotingNecessaryStrings(true)
        .BuildValueSerializer();

    /// <summary>Creates a lightweight <see cref="ISerializer"/> wrapper around the shared,
    /// pre-built value serializer for the given indent width.</summary>
    public static ISerializer Create(int indent = 2) =>
        Serializer.FromValueSerializer(
            ValueSerializer, EmitterSettings.Default.WithBestIndent(Math.Clamp(indent, 1, 8)).WithNewLine("\n"));
}
