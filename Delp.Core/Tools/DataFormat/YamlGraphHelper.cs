using System.Globalization;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Delp.Core.Tools.DataFormat;

/// <summary>
/// Converts a parsed YAML <see cref="YamlNode"/> tree into a plain CLR object graph
/// (Dictionary&lt;string, object?&gt; / List&lt;object?&gt; / string / long / double / bool / null),
/// inferring scalar types from YAML's core-schema plain-scalar rules so re-emitting or
/// converting the document preserves the author's intent (42 the integer vs "42" the string).
/// Shared by yaml-format and json-yaml.
/// </summary>
internal static class YamlGraphHelper
{
    private static readonly Regex FloatPattern = new(
        @"^[-+]?(\d+\.\d*|\.\d+|\d+)([eE][-+]?\d+)?$",
        RegexOptions.Compiled, TimeSpan.FromSeconds(2));

    public static object? ToGraph(YamlNode? node) => node switch
    {
        null => null,
        YamlScalarNode scalar => ScalarToObject(scalar),
        YamlSequenceNode seq => seq.Children.Select(ToGraph).ToList(),
        YamlMappingNode map => MapToGraph(map),
        _ => throw new FormatException($"Unsupported YAML node type: {node.NodeType}"),
    };

    private static Dictionary<string, object?> MapToGraph(YamlMappingNode map)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var kv in map.Children)
        {
            var key = kv.Key is YamlScalarNode keyScalar ? keyScalar.Value ?? "" : kv.Key.ToString();
            dict[key] = ToGraph(kv.Value);
        }
        return dict;
    }

    private static object? ScalarToObject(YamlScalarNode scalar)
    {
        var text = scalar.Value ?? "";

        // Quoted scalars are always strings, regardless of what they look like.
        if (scalar.Style is ScalarStyle.SingleQuoted or ScalarStyle.DoubleQuoted
            or ScalarStyle.Literal or ScalarStyle.Folded)
            return text;

        if (text.Length == 0 || text is "~" or "null" or "Null" or "NULL")
            return null;
        if (text is "true" or "True" or "TRUE")
            return true;
        if (text is "false" or "False" or "FALSE")
            return false;
        if (IsPlainInt(text, out var l))
            return l;
        if (IsPlainFloat(text, out var d))
            return d;
        return text;
    }

    private static bool IsPlainInt(string s, out long value)
    {
        value = 0;
        var digits = s.StartsWith('+') || s.StartsWith('-') ? s[1..] : s;
        if (digits.Length == 0 || !digits.All(char.IsAsciiDigit))
            return false;
        if (digits.Length > 1 && digits[0] == '0')
            return false; // leading zero: ambiguous (could be intended as a string) — keep as-is.
        return long.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    private static bool IsPlainFloat(string s, out double value)
    {
        value = 0;
        var lower = s.ToLowerInvariant();
        switch (lower)
        {
            case ".inf" or "+.inf":
                value = double.PositiveInfinity;
                return true;
            case "-.inf":
                value = double.NegativeInfinity;
                return true;
            case ".nan":
                value = double.NaN;
                return true;
        }

        if (!s.Contains('.') && !s.Contains('e') && !s.Contains('E'))
            return false; // avoid re-classifying plain integers as floats.
        if (!FloatPattern.IsMatch(s))
            return false;
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
