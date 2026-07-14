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

    public static string ToCamelCase(string? input) => ToCamelCase(Tokenize(input));

    private static string ToCamelCase(IReadOnlyList<string> tokens)
    {
        if (tokens.Count == 0)
            return "";
        var sb = new StringBuilder(tokens[0]);
        for (var i = 1; i < tokens.Count; i++)
            sb.Append(Capitalize(tokens[i]));
        return sb.ToString();
    }

    public static string ToPascalCase(string? input) => ToPascalCase(Tokenize(input));

    private static string ToPascalCase(IReadOnlyList<string> tokens) =>
        string.Concat(tokens.Select(Capitalize));

    public static string ToSnakeCase(string? input) => ToSnakeCase(Tokenize(input));

    private static string ToSnakeCase(IReadOnlyList<string> tokens) =>
        string.Join("_", tokens);

    public static string ToScreamingSnakeCase(string? input) => ToScreamingSnakeCase(Tokenize(input));

    private static string ToScreamingSnakeCase(IReadOnlyList<string> tokens) =>
        string.Join("_", tokens.Select(t => t.ToUpperInvariant()));

    public static string ToKebabCase(string? input) => ToKebabCase(Tokenize(input));

    private static string ToKebabCase(IReadOnlyList<string> tokens) =>
        string.Join("-", tokens);

    public static string ToTrainCase(string? input) => ToTrainCase(Tokenize(input));

    private static string ToTrainCase(IReadOnlyList<string> tokens) =>
        string.Join("-", tokens.Select(Capitalize));

    public static string ToTitleCase(string? input) => ToTitleCase(Tokenize(input));

    private static string ToTitleCase(IReadOnlyList<string> tokens) =>
        string.Join(" ", tokens.Select(Capitalize));

    public static string ToSentenceCase(string? input) => ToSentenceCase(Tokenize(input));

    private static string ToSentenceCase(IReadOnlyList<string> tokens)
    {
        if (tokens.Count == 0)
            return "";
        var sb = new StringBuilder(Capitalize(tokens[0]));
        for (var i = 1; i < tokens.Count; i++)
            sb.Append(' ').Append(tokens[i]);
        return sb.ToString();
    }

    public static string ToLowercase(string? input) => ToLowercase(Tokenize(input));

    private static string ToLowercase(IReadOnlyList<string> tokens) =>
        string.Join(" ", tokens);

    public static string ToUppercase(string? input) => ToUppercase(Tokenize(input));

    private static string ToUppercase(IReadOnlyList<string> tokens) =>
        string.Join(" ", tokens.Select(t => t.ToUpperInvariant()));

    public static string ToDotCase(string? input) => ToDotCase(Tokenize(input));

    private static string ToDotCase(IReadOnlyList<string> tokens) =>
        string.Join(".", tokens);

    public static string ToPathCase(string? input) => ToPathCase(Tokenize(input));

    private static string ToPathCase(IReadOnlyList<string> tokens) =>
        string.Join("/", tokens);

    /// <summary>
    /// Renders every supported style, in the order the UI lists them. Tokenizes the
    /// input once and reuses it across all 12 converters, instead of re-running the
    /// tokenizer's regex passes independently for each style.
    /// </summary>
    public static IReadOnlyList<CaseResult> ConvertAll(string? input)
    {
        var tokens = Tokenize(input);
        return
        [
            new("camelCase", ToCamelCase(tokens)),
            new("PascalCase", ToPascalCase(tokens)),
            new("snake_case", ToSnakeCase(tokens)),
            new("SCREAMING_SNAKE", ToScreamingSnakeCase(tokens)),
            new("kebab-case", ToKebabCase(tokens)),
            new("Train-Case", ToTrainCase(tokens)),
            new("Title Case", ToTitleCase(tokens)),
            new("Sentence case", ToSentenceCase(tokens)),
            new("lowercase", ToLowercase(tokens)),
            new("UPPERCASE", ToUppercase(tokens)),
            new("dot.case", ToDotCase(tokens)),
            new("path/case", ToPathCase(tokens)),
        ];
    }
}
