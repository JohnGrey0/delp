using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Exceptions;
using GraphQLParser.Visitors;

namespace Delp.Core.Tools.DataFormat;

/// <summary>A GraphQL syntax error location and message.</summary>
public sealed record GraphQlError(int Line, int Column, string Message);

/// <summary>
/// Formats, minifies and validates GraphQL documents (queries, mutations, fragments or
/// SDL schemas) using the GraphQL-Parser package's AST and <see cref="SDLPrinter"/>.
/// </summary>
public static class GraphQlTool
{
    public static string Format(string graphQl)
    {
        ArgumentNullException.ThrowIfNull(graphQl);
        var document = ParseOrThrowFormatException(graphQl);
        var printer = new SDLPrinter(new SDLPrinterOptions { IndentSize = 2 });
        return NormalizeNewLines(printer.Print(document));
    }

    /// <summary>SDLPrinter writes <see cref="Environment.NewLine"/>; normalize to "\n" for determinism.</summary>
    private static string NormalizeNewLines(string text) => text.Replace("\r\n", "\n");

    /// <summary>Strips insignificant whitespace and comments, keeping the document on as few lines as possible.</summary>
    public static string Minify(string graphQl)
    {
        ArgumentNullException.ThrowIfNull(graphQl);
        var formatted = Format(graphQl);
        return Collapse(formatted);
    }

    /// <summary>Returns null when <paramref name="graphQl"/> is syntactically valid, otherwise the error location.</summary>
    public static GraphQlError? Validate(string graphQl)
    {
        ArgumentNullException.ThrowIfNull(graphQl);
        try
        {
            Parse(graphQl);
            return null;
        }
        catch (GraphQLSyntaxErrorException ex)
        {
            return new GraphQlError(ex.Location.Line, ex.Location.Column, ex.Description);
        }
    }

    private static GraphQLDocument Parse(string graphQl) => GraphQLParser.Parser.Parse(graphQl);

    private static GraphQLDocument ParseOrThrowFormatException(string graphQl)
    {
        try
        {
            return Parse(graphQl);
        }
        catch (GraphQLSyntaxErrorException ex)
        {
            throw new FormatException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Collapses the fully-formatted SDL text to as few characters as possible: runs of
    /// whitespace outside string literals become a single space only where omitting it
    /// would merge two adjacent "word" tokens (names, keywords, numbers); everywhere else
    /// the whitespace is dropped entirely. String literal contents (including triple-quoted
    /// block strings) are copied through verbatim.
    /// </summary>
    private static string Collapse(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        int i = 0;
        int n = text.Length;
        char lastEmitted = '\0';
        bool hasLast = false;

        while (i < n)
        {
            char c = text[i];

            if (c == '"')
            {
                bool isBlock = i + 2 < n && text[i + 1] == '"' && text[i + 2] == '"';
                int start = i;
                if (isBlock)
                {
                    i += 3;
                    while (i < n && !(text[i] == '"' && i + 2 < n && text[i + 1] == '"' && text[i + 2] == '"'))
                        i++;
                    i = Math.Min(i + 3, n);
                }
                else
                {
                    i++;
                    while (i < n && text[i] != '"')
                    {
                        if (text[i] == '\\' && i + 1 < n) i++;
                        i++;
                    }
                    i = Math.Min(i + 1, n);
                }
                var literal = text[start..i];
                sb.Append(literal);
                hasLast = literal.Length > 0;
                if (hasLast) lastEmitted = literal[^1];
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                int j = i;
                while (j < n && char.IsWhiteSpace(text[j])) j++;
                char next = j < n ? text[j] : '\0';
                if (hasLast && IsWordChar(lastEmitted) && IsWordChar(next))
                {
                    sb.Append(' ');
                    lastEmitted = ' ';
                }
                i = j;
                continue;
            }

            sb.Append(c);
            lastEmitted = c;
            hasLast = true;
            i++;
        }

        return sb.ToString();
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '$';
}
