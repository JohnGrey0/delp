using System.Text;
using System.Text.RegularExpressions;

namespace Delp.Core.Tools.TextProcessing;

/// <summary>One converted style, ready for display.</summary>
public sealed record CaseResult(string Style, string Value);

/// <summary>
/// Tokenizes free-form identifiers/phrases into words and renders them in the
/// common programming and prose case conventions.
/// </summary>
public static class CaseTool
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    // Splits on whitespace and the common word-separator punctuation.
    private static readonly Regex SeparatorSplit =
        new(@"[\s_\-./]+", RegexOptions.None, Timeout);

    // Boundaries within a separator-free chunk:
    //   lower/digit -> Upper   ("aB"      -> "a", "B")
    //   ACRONYM run -> Word    ("HTTPServer" -> "HTTP", "Server")
    //   letter -> digit        ("v2"      -> "v", "2")
    //   digit -> letter        ("2go"     -> "2", "go")
    private static readonly Regex BoundarySplit = new(
        @"(?<=[\p{Ll}\p{Nd}])(?=\p{Lu})" +
        @"|(?<=\p{Lu})(?=\p{Lu}\p{Ll})" +
        @"|(?<=\p{L})(?=\p{Nd})" +
        @"|(?<=\p{Nd})(?=\p{L})",
        RegexOptions.None, Timeout);

    /// <summary>Splits input into lowercase word tokens per the batch H tokenizer rules.</summary>
    public static IReadOnlyList<string> Tokenize(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return Array.Empty<string>();

        var tokens = new List<string>();
        foreach (var chunk in SeparatorSplit.Split(input))
        {
            if (chunk.Length == 0)
                continue;
            foreach (var part in BoundarySplit.Split(chunk))
            {
                if (part.Length > 0)
                    tokens.Add(part.ToLowerInvariant());
            }
        }
        return tokens;
    }

    private static string Capitalize(string token) =>
        token.Length == 0 ? token : char.ToUpperInvariant(token[0]) + token[1..];

    public static string ToCamelCase(string? input)
    {
        var tokens = Tokenize(input);
        if (tokens.Count == 0)
            return "";
        var sb = new StringBuilder(tokens[0]);
        for (var i = 1; i < tokens.Count; i++)
            sb.Append(Capitalize(tokens[i]));
        return sb.ToString();
    }

    public static string ToPascalCase(string? input) =>
        string.Concat(Tokenize(input).Select(Capitalize));

    public static string ToSnakeCase(string? input) =>
        string.Join("_", Tokenize(input));

    public static string ToScreamingSnakeCase(string? input) =>
        string.Join("_", Tokenize(input).Select(t => t.ToUpperInvariant()));

    public static string ToKebabCase(string? input) =>
        string.Join("-", Tokenize(input));

    public static string ToTrainCase(string? input) =>
        string.Join("-", Tokenize(input).Select(Capitalize));

    public static string ToTitleCase(string? input) =>
        string.Join(" ", Tokenize(input).Select(Capitalize));

    public static string ToSentenceCase(string? input)
    {
        var tokens = Tokenize(input);
        if (tokens.Count == 0)
            return "";
        var sb = new StringBuilder(Capitalize(tokens[0]));
        for (var i = 1; i < tokens.Count; i++)
            sb.Append(' ').Append(tokens[i]);
        return sb.ToString();
    }

    public static string ToLowercase(string? input) =>
        string.Join(" ", Tokenize(input));

    public static string ToUppercase(string? input) =>
        string.Join(" ", Tokenize(input).Select(t => t.ToUpperInvariant()));

    public static string ToDotCase(string? input) =>
        string.Join(".", Tokenize(input));

    public static string ToPathCase(string? input) =>
        string.Join("/", Tokenize(input));

    /// <summary>Renders every supported style, in the order the UI lists them.</summary>
    public static IReadOnlyList<CaseResult> ConvertAll(string? input) =>
    [
        new("camelCase", ToCamelCase(input)),
        new("PascalCase", ToPascalCase(input)),
        new("snake_case", ToSnakeCase(input)),
        new("SCREAMING_SNAKE", ToScreamingSnakeCase(input)),
        new("kebab-case", ToKebabCase(input)),
        new("Train-Case", ToTrainCase(input)),
        new("Title Case", ToTitleCase(input)),
        new("Sentence case", ToSentenceCase(input)),
        new("lowercase", ToLowercase(input)),
        new("UPPERCASE", ToUppercase(input)),
        new("dot.case", ToDotCase(input)),
        new("path/case", ToPathCase(input)),
    ];
}
