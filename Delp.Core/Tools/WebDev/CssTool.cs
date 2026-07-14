using System.Text;
using NUglify;

namespace Delp.Core.Tools.WebDev;

/// <summary>CSS minifier (NUglify) and hand-rolled deterministic beautifier.</summary>
public static class CssTool
{
    public static MinifyResult Minify(string css)
    {
        css ??= "";
        var before = System.Text.Encoding.UTF8.GetByteCount(css);
        var result = Uglify.Css(css);
        var code = result.Code;
        var after = string.IsNullOrEmpty(code) ? 0 : System.Text.Encoding.UTF8.GetByteCount(code);
        return new MinifyResult(code, NUglifyErrors.Format(result.Errors), before, after);
    }

    /// <summary>
    /// Hand-rolled beautifier (NUglify has no pretty-printer): one selector per line, <c>{</c> on the
    /// same line, one declaration per line, a blank line between sibling rules, and all comments
    /// (including <c>/*! ... */</c>) preserved verbatim. Never throws — malformed/unbalanced input is
    /// formatted best-effort.
    /// </summary>
    public static string Beautify(string css, int indentSize = 2)
    {
        css ??= "";
        var indent = new string(' ', Math.Max(0, indentSize));
        var tokens = Tokenize(css);
        var sb = new StringBuilder();
        var depth = 0;

        string Pad() => string.Concat(Enumerable.Repeat(indent, depth));

        for (var i = 0; i < tokens.Count; i++)
        {
            var tok = tokens[i];
            switch (tok.Kind)
            {
                case TokenKind.Comment:
                    sb.Append(Pad()).Append(tok.Text).Append('\n');
                    break;

                case TokenKind.AtStatement:
                    sb.Append(Pad()).Append(CollapseWhitespace(tok.Text)).Append(";\n");
                    break;

                case TokenKind.Declaration:
                    sb.Append(Pad()).Append(NormalizeDeclaration(tok.Text)).Append(";\n");
                    break;

                case TokenKind.SelectorOpen:
                    sb.Append(FormatPrelude(tok.Text, Pad())).Append(" {\n");
                    depth++;
                    break;

                case TokenKind.BlockClose:
                    depth = Math.Max(0, depth - 1);
                    sb.Append(Pad()).Append("}\n");
                    if (i + 1 < tokens.Count && tokens[i + 1].Kind != TokenKind.BlockClose)
                        sb.Append('\n');
                    break;
            }
        }

        return sb.ToString().TrimEnd('\n') + "\n";
    }

    // ---- tokenizer ----

    private enum TokenKind { Comment, AtStatement, SelectorOpen, BlockClose, Declaration }

    private readonly record struct Token(TokenKind Kind, string Text);

    private static List<Token> Tokenize(string css)
    {
        var tokens = new List<Token>();
        var sb = new StringBuilder();
        var parenDepth = 0;
        var i = 0;
        var n = css.Length;

        void FlushPending()
        {
            var text = sb.ToString().Trim();
            sb.Clear();
            if (text.Length == 0)
                return;
            tokens.Add(new Token(text.StartsWith('@') ? TokenKind.AtStatement : TokenKind.Declaration, text));
        }

        while (i < n)
        {
            var c = css[i];

            if (c == '/' && i + 1 < n && css[i + 1] == '*')
            {
                var end = css.IndexOf("*/", i + 2, StringComparison.Ordinal);
                var commentEnd = end < 0 ? n : end + 2;
                tokens.Add(new Token(TokenKind.Comment, css[i..commentEnd]));
                i = commentEnd;
                continue;
            }

            if (c is '"' or '\'')
            {
                var start = i;
                var quote = c;
                i++;
                while (i < n && css[i] != quote)
                    i += css[i] == '\\' && i + 1 < n ? 2 : 1;
                i = Math.Min(i + 1, n);
                sb.Append(css, start, i - start);
                continue;
            }

            if (c == '(') { parenDepth++; sb.Append(c); i++; continue; }
            if (c == ')') { parenDepth = Math.Max(0, parenDepth - 1); sb.Append(c); i++; continue; }

            if (c == '{' && parenDepth == 0)
            {
                tokens.Add(new Token(TokenKind.SelectorOpen, sb.ToString().Trim()));
                sb.Clear();
                i++;
                continue;
            }

            if (c == '}' && parenDepth == 0)
            {
                FlushPending();
                tokens.Add(new Token(TokenKind.BlockClose, ""));
                i++;
                continue;
            }

            if (c == ';' && parenDepth == 0)
            {
                FlushPending();
                i++;
                continue;
            }

            sb.Append(c);
            i++;
        }

        FlushPending();
        return tokens;
    }

    // ---- formatting helpers ----

    private static string FormatPrelude(string text, string pad)
    {
        var parts = SplitTopLevel(text, ',').Select(CollapseWhitespace).Where(p => p.Length > 0).ToList();
        if (parts.Count == 0)
            return pad + CollapseWhitespace(text);
        return string.Join(",\n", parts.Select(p => pad + p));
    }

    private static string NormalizeDeclaration(string text)
    {
        var collapsed = CollapseWhitespace(text);
        var segments = SplitTopLevel(collapsed, ':');
        if (segments.Count < 2)
            return collapsed;

        var prop = segments[0].TrimEnd();
        var value = string.Join(":", segments.Skip(1)).TrimStart();
        return $"{prop}: {value}";
    }

    /// <summary>Collapses runs of whitespace to a single space, leaving quoted strings untouched.</summary>
    private static string CollapseWhitespace(string s)
    {
        var sb = new StringBuilder();
        char? quote = null;
        var pendingSpace = false;

        foreach (var c in s)
        {
            if (quote != null)
            {
                sb.Append(c);
                if (c == quote) quote = null;
                continue;
            }

            if (c is '"' or '\'')
            {
                quote = c;
                sb.Append(c);
                pendingSpace = false;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                pendingSpace = sb.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                sb.Append(' ');
                pendingSpace = false;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>Splits on <paramref name="sep"/>, ignoring occurrences inside strings, (), or [].</summary>
    private static List<string> SplitTopLevel(string s, char sep)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var paren = 0;
        var bracket = 0;
        char? quote = null;

        foreach (var c in s)
        {
            if (quote != null)
            {
                sb.Append(c);
                if (c == quote) quote = null;
                continue;
            }

            switch (c)
            {
                case '"' or '\'':
                    quote = c;
                    sb.Append(c);
                    break;
                case '(':
                    paren++;
                    sb.Append(c);
                    break;
                case ')':
                    paren = Math.Max(0, paren - 1);
                    sb.Append(c);
                    break;
                case '[':
                    bracket++;
                    sb.Append(c);
                    break;
                case ']':
                    bracket = Math.Max(0, bracket - 1);
                    sb.Append(c);
                    break;
                default:
                    if (c == sep && paren == 0 && bracket == 0)
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }

        result.Add(sb.ToString());
        return result;
    }
}
