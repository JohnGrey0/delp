using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Delp.Core.Tools.DataFormat;

public enum SchemaKind
{
    Object,
    Array,
    String,
    Int,
    Double,
    Bool,
    /// <summary>Every observed occurrence was JSON null.</summary>
    Null,
    /// <summary>No occurrences observed (empty array).</summary>
    Unknown,
    /// <summary>Two or more incompatible JSON types observed for the same slot.</summary>
    Mixed,
}

public sealed class SchemaNode
{
    public required SchemaKind Kind { get; init; }
    public List<SchemaProperty>? Properties { get; init; }
    public SchemaNode? ElementType { get; init; }

    /// <summary>Collision-resolved PascalCase name, set for <see cref="SchemaKind.Object"/>
    /// nodes (and, as a fallback, on the overall root node) by <see cref="JsonTypesTool.Infer"/>.</summary>
    public string? ResolvedName { get; internal set; }
}

public sealed class SchemaProperty
{
    /// <summary>The original JSON key, unmodified.</summary>
    public required string Name { get; init; }
    public required SchemaNode Type { get; init; }
    /// <summary>True when the key was missing (or the type conflicted) in at least one array element.</summary>
    public bool Optional { get; init; }
}

public sealed record CSharpOptions(bool Records = true, bool JsonPropertyNames = true);

public sealed record TsOptions(bool Interfaces = true);

/// <summary>Infers a merged schema from a JSON document and emits idiomatic C# or TypeScript types.</summary>
public static class JsonTypesTool
{
    private static readonly Regex TsIdentifier =
        new(@"^[A-Za-z_$][A-Za-z0-9_$]*$", RegexOptions.Compiled, TimeSpan.FromSeconds(2));

    public static SchemaNode Infer(string json, string rootName)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Invalid JSON: {ex.Message}", ex);
        }

        using (doc)
        {
            var root = InferNode(doc.RootElement);
            var used = new HashSet<string>(StringComparer.Ordinal);
            var name = string.IsNullOrWhiteSpace(rootName) ? "Root" : rootName;
            AssignNames(root, name, used);
            root.ResolvedName ??= ToPascalCase(name); // fallback for a bare scalar/primitive-array root
            return root;
        }
    }

    public static string ToCSharp(SchemaNode schema, CSharpOptions options)
    {
        var types = new List<SchemaNode>();
        CollectNamedObjects(schema, types);

        if (types.Count == 0)
            return $"// Root JSON value has no object properties to model (inferred type: {CSharpTypeExpr(schema)}).\n";

        var needsUsing = options.JsonPropertyNames &&
            types.Any(t => t.Properties!.Any(p => ToPascalCase(p.Name) != p.Name));

        var sb = new StringBuilder();
        if (needsUsing)
            sb.AppendLine("using System.Text.Json.Serialization;").AppendLine();

        foreach (var type in types)
            EmitCSharpType(type, options, sb);

        return sb.ToString().TrimEnd('\r', '\n') + "\n";
    }

    public static string ToTypeScript(SchemaNode schema, TsOptions options)
    {
        var types = new List<SchemaNode>();
        CollectNamedObjects(schema, types);

        if (types.Count == 0)
            return $"export type {schema.ResolvedName ?? "Root"} = {TsTypeExpr(schema)};\n";

        var sb = new StringBuilder();
        foreach (var type in types)
            EmitTsType(type, options, sb);

        return sb.ToString().TrimEnd('\r', '\n') + "\n";
    }

    // ------------------------------------------------------------- inference

    private static SchemaNode InferNode(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Object => InferObject(el),
        JsonValueKind.Array => InferArray(el),
        JsonValueKind.String => new SchemaNode { Kind = SchemaKind.String },
        JsonValueKind.Number => new SchemaNode { Kind = IsIntegral(el) ? SchemaKind.Int : SchemaKind.Double },
        JsonValueKind.True or JsonValueKind.False => new SchemaNode { Kind = SchemaKind.Bool },
        JsonValueKind.Null => new SchemaNode { Kind = SchemaKind.Null },
        _ => new SchemaNode { Kind = SchemaKind.Unknown },
    };

    private static bool IsIntegral(JsonElement el) => el.GetRawText().IndexOfAny(['.', 'e', 'E']) < 0;

    private static SchemaNode InferObject(JsonElement el)
    {
        var props = new List<SchemaProperty>();
        foreach (var prop in el.EnumerateObject())
            props.Add(new SchemaProperty { Name = prop.Name, Type = InferNode(prop.Value), Optional = false });
        return new SchemaNode { Kind = SchemaKind.Object, Properties = props };
    }

    private static SchemaNode InferArray(JsonElement el)
    {
        SchemaNode? merged = null;
        foreach (var item in el.EnumerateArray())
        {
            var itemSchema = InferNode(item);
            merged = merged is null ? itemSchema : MergeTypes(merged, itemSchema);
        }
        return new SchemaNode { Kind = SchemaKind.Array, ElementType = merged ?? new SchemaNode { Kind = SchemaKind.Unknown } };
    }

    private static SchemaNode MergeTypes(SchemaNode a, SchemaNode b)
    {
        if (a.Kind == SchemaKind.Null) return b;
        if (b.Kind == SchemaKind.Null) return a;
        if (a.Kind == SchemaKind.Unknown) return b;
        if (b.Kind == SchemaKind.Unknown) return a;

        if (a.Kind == b.Kind)
        {
            return a.Kind switch
            {
                SchemaKind.Object => MergeObjects(a, b),
                SchemaKind.Array => new SchemaNode { Kind = SchemaKind.Array, ElementType = MergeTypes(a.ElementType!, b.ElementType!) },
                _ => a,
            };
        }

        if (IsNumeric(a.Kind) && IsNumeric(b.Kind))
            return new SchemaNode { Kind = SchemaKind.Double };

        return new SchemaNode { Kind = SchemaKind.Mixed };
    }

    private static bool IsNumeric(SchemaKind kind) => kind is SchemaKind.Int or SchemaKind.Double;

    private static SchemaNode MergeObjects(SchemaNode a, SchemaNode b)
    {
        var result = new List<SchemaProperty>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void AddFrom(SchemaNode source)
        {
            foreach (var prop in source.Properties!)
            {
                if (!seen.Add(prop.Name))
                    continue;
                var pa = a.Properties!.FirstOrDefault(p => p.Name == prop.Name);
                var pb = b.Properties!.FirstOrDefault(p => p.Name == prop.Name);
                var optional = pa is null || pa.Optional || pb is null || pb.Optional;
                var type = pa is null ? pb!.Type : pb is null ? pa.Type : MergeTypes(pa.Type, pb.Type);
                result.Add(new SchemaProperty { Name = prop.Name, Type = type, Optional = optional });
            }
        }

        AddFrom(a);
        AddFrom(b);
        return new SchemaNode { Kind = SchemaKind.Object, Properties = result };
    }

    // ------------------------------------------------------------- naming

    private static void AssignNames(SchemaNode node, string suggestedName, HashSet<string> used)
    {
        switch (node.Kind)
        {
            case SchemaKind.Object:
                node.ResolvedName = ReserveName(ToPascalCase(suggestedName), used);
                foreach (var prop in node.Properties!)
                    AssignNames(prop.Type, prop.Name, used);
                break;
            case SchemaKind.Array:
                AssignNames(node.ElementType!, suggestedName, used);
                break;
        }
    }

    private static string ReserveName(string desired, HashSet<string> used)
    {
        if (string.IsNullOrEmpty(desired))
            desired = "Type";
        var candidate = desired;
        var n = 1;
        while (!used.Add(candidate))
        {
            n++;
            candidate = desired + n;
        }
        return candidate;
    }

    private static void CollectNamedObjects(SchemaNode node, List<SchemaNode> result)
    {
        switch (node.Kind)
        {
            case SchemaKind.Object:
                result.Add(node);
                foreach (var prop in node.Properties!)
                    CollectNamedObjects(prop.Type, result);
                break;
            case SchemaKind.Array:
                CollectNamedObjects(node.ElementType!, result);
                break;
        }
    }

    /// <summary>Splits a JSON key into identifier tokens (on non-alphanumerics and
    /// lower-to-upper camelCase boundaries) and re-joins them as PascalCase.</summary>
    private static string ToPascalCase(string key)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();

        void Flush()
        {
            if (current.Length > 0)
            {
                tokens.Add(current.ToString());
                current.Clear();
            }
        }

        for (var i = 0; i < key.Length; i++)
        {
            var c = key[i];
            if (!char.IsLetterOrDigit(c))
            {
                Flush();
                continue;
            }
            if (i > 0 && char.IsUpper(c) && char.IsLower(key[i - 1]))
                Flush();
            current.Append(c);
        }
        Flush();

        if (tokens.Count == 0)
            return "Field";

        var sb = new StringBuilder();
        foreach (var token in tokens)
            sb.Append(char.ToUpperInvariant(token[0])).Append(token, 1, token.Length - 1);

        var name = sb.ToString();
        return char.IsDigit(name[0]) ? "_" + name : name;
    }

    // ------------------------------------------------------------- C# emission

    private static string CSharpTypeExpr(SchemaNode node) => node.Kind switch
    {
        SchemaKind.Object => node.ResolvedName ?? "object",
        SchemaKind.Array => CSharpTypeExpr(node.ElementType!) + "[]",
        SchemaKind.String => "string",
        SchemaKind.Int => "int",
        SchemaKind.Double => "double",
        SchemaKind.Bool => "bool",
        _ => "object", // Null, Unknown, Mixed
    };

    private static void EmitCSharpType(SchemaNode node, CSharpOptions options, StringBuilder sb)
    {
        var props = node.Properties!;

        if (options.Records)
        {
            sb.Append("public record ").Append(node.ResolvedName).AppendLine("(");
            for (var i = 0; i < props.Count; i++)
            {
                var p = props[i];
                var pascal = ToPascalCase(p.Name);
                var attr = options.JsonPropertyNames && pascal != p.Name
                    ? $"[property: JsonPropertyName(\"{EscapeCs(p.Name)}\")] "
                    : "";
                var nullMark = p.Optional || p.Type.Kind == SchemaKind.Null ? "?" : "";
                sb.Append("    ").Append(attr).Append(CSharpTypeExpr(p.Type)).Append(nullMark)
                  .Append(' ').Append(pascal).Append(i < props.Count - 1 ? ",\n" : "\n");
            }
            sb.AppendLine(");").AppendLine();
        }
        else
        {
            sb.Append("public class ").AppendLine(node.ResolvedName);
            sb.AppendLine("{");
            for (var i = 0; i < props.Count; i++)
            {
                var p = props[i];
                var pascal = ToPascalCase(p.Name);
                if (options.JsonPropertyNames && pascal != p.Name)
                    sb.Append("    [JsonPropertyName(\"").Append(EscapeCs(p.Name)).AppendLine("\")]");
                var nullMark = p.Optional || p.Type.Kind == SchemaKind.Null ? "?" : "";
                sb.Append("    public ").Append(CSharpTypeExpr(p.Type)).Append(nullMark)
                  .Append(' ').Append(pascal).AppendLine(" { get; set; }");
                if (i < props.Count - 1)
                    sb.AppendLine();
            }
            sb.AppendLine("}").AppendLine();
        }
    }

    private static string EscapeCs(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    // ------------------------------------------------------------- TypeScript emission

    private static string TsTypeExpr(SchemaNode node) => node.Kind switch
    {
        SchemaKind.Object => node.ResolvedName ?? "unknown",
        SchemaKind.Array => TsTypeExpr(node.ElementType!) + "[]",
        SchemaKind.String => "string",
        SchemaKind.Int or SchemaKind.Double => "number",
        SchemaKind.Bool => "boolean",
        SchemaKind.Mixed => "any",
        _ => "unknown", // Null, Unknown
    };

    private static void EmitTsType(SchemaNode node, TsOptions options, StringBuilder sb)
    {
        sb.AppendLine(options.Interfaces
            ? $"export interface {node.ResolvedName} {{"
            : $"export type {node.ResolvedName} = {{");

        foreach (var p in node.Properties!)
        {
            var key = TsPropertyKey(p.Name);
            var optionalMark = p.Optional ? "?" : "";
            sb.Append("  ").Append(key).Append(optionalMark).Append(": ").Append(TsTypeExpr(p.Type)).AppendLine(";");
        }

        sb.AppendLine(options.Interfaces ? "}" : "};").AppendLine();
    }

    private static string TsPropertyKey(string name) =>
        TsIdentifier.IsMatch(name) ? name : $"\"{name.Replace("\"", "\\\"")}\"";
}
