using System.Text;

namespace Delp.Core.Tools.DataFormat;

/// <summary>Options for <see cref="SqlFormatTool.Format"/>.</summary>
public sealed record SqlFormatOptions(bool UppercaseKeywords = true, int IndentSize = 2);

internal enum SqlTokenKind
{
    Keyword, Identifier, QuotedIdentifier, Number, String, LineComment, BlockComment,
    Comma, OpenParen, CloseParen, Semicolon, Dot, Operator, Star,
}

internal readonly record struct SqlToken(SqlTokenKind Kind, string Text, bool SpaceBefore);

internal enum ClauseKind { None, Select, Where, Having, Other }

/// <summary>
/// Hand-rolled, dependency-free, dialect-agnostic SQL formatter and minifier. A small
/// tokenizer (strings, comments, quoted/bracketed identifiers, numbers, punctuation) feeds
/// a conservative clause-based layout engine: tokens are never reordered or altered, only
/// whitespace/newlines/indentation around them are chosen. Output is fully deterministic.
/// </summary>
public static class SqlFormatTool
{
    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT","FROM","WHERE","GROUP","BY","HAVING","ORDER","LIMIT","UNION","ALL","JOIN",
        "INNER","LEFT","RIGHT","FULL","OUTER","CROSS","ON","INSERT","INTO","UPDATE","DELETE",
        "SET","VALUES","WITH","AS","AND","OR","NOT","NULL","IS","IN","LIKE","BETWEEN","CASE",
        "WHEN","THEN","ELSE","END","DISTINCT","EXISTS","ASC","DESC","TOP","OFFSET","FETCH",
        "NEXT","ROWS","ONLY","DEFAULT","PRIMARY","KEY","FOREIGN","REFERENCES","CREATE","TABLE",
        "ALTER","DROP","INDEX","VIEW","TRIGGER","CONSTRAINT","CHECK","UNIQUE","RETURNING",
    };

    // Multi-word clause starters are matched greedily before single-word ones.
    private static readonly string[][] ClausePhrases =
    {
        new[] { "LEFT", "OUTER", "JOIN" },
        new[] { "RIGHT", "OUTER", "JOIN" },
        new[] { "FULL", "OUTER", "JOIN" },
        new[] { "GROUP", "BY" },
        new[] { "ORDER", "BY" },
        new[] { "INSERT", "INTO" },
        new[] { "DELETE", "FROM" },
        new[] { "UNION", "ALL" },
        new[] { "INNER", "JOIN" },
        new[] { "LEFT", "JOIN" },
        new[] { "RIGHT", "JOIN" },
        new[] { "FULL", "JOIN" },
        new[] { "CROSS", "JOIN" },
        new[] { "SELECT" },
        new[] { "FROM" },
        new[] { "WHERE" },
        new[] { "HAVING" },
        new[] { "LIMIT" },
        new[] { "UNION" },
        new[] { "UPDATE" },
        new[] { "SET" },
        new[] { "VALUES" },
        new[] { "WITH" },
        new[] { "JOIN" },
        new[] { "OFFSET" },
        new[] { "RETURNING" },
    };

    public static string Format(string sql, SqlFormatOptions options)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentNullException.ThrowIfNull(options);
        var tokens = Tokenize(sql);
        return new Layouter(tokens, options).Run();
    }

    /// <summary>Collapses whitespace outside strings to single spaces and always strips comments.</summary>
    public static string Minify(string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);
        var tokens = Tokenize(sql).Where(t => t.Kind is not (SqlTokenKind.LineComment or SqlTokenKind.BlockComment));
        var sb = new StringBuilder();
        SqlToken? prev = null;
        foreach (var tok in tokens)
        {
            if (NeedsSpaceBefore(prev, tok))
                sb.Append(' ');
            sb.Append(tok.Text);
            prev = tok;
        }
        return sb.ToString();
    }

    private static bool NeedsSpaceBefore(SqlToken? prevTok, SqlToken cur)
    {
        if (prevTok is not { } prev) return false;
        if (cur.Kind is SqlTokenKind.Comma or SqlTokenKind.CloseParen or SqlTokenKind.Semicolon or SqlTokenKind.Dot) return false;
        if (prev.Kind is SqlTokenKind.OpenParen or SqlTokenKind.Dot) return false;
        if (cur.Kind == SqlTokenKind.OpenParen && prev.Kind is SqlTokenKind.Identifier or SqlTokenKind.QuotedIdentifier)
            return cur.SpaceBefore; // preserve the source's own choice: "COUNT(x)" vs. "users (id, name)"
        return true;
    }

    private sealed class Layouter
    {
        private readonly List<SqlToken> _tokens;
        private readonly SqlFormatOptions _options;
        private readonly StringBuilder _sb = new();
        private readonly List<Frame> _frames = new();
        private bool _freshLine = true;
        private bool _isFirstLine = true;
        private SqlToken? _prevTok;

        private sealed class Frame
        {
            public int BaseIndent;
            public bool WasSubquery;
            public ClauseKind Current = ClauseKind.None;
        }

        public Layouter(List<SqlToken> tokens, SqlFormatOptions options)
        {
            _tokens = tokens;
            _options = options;
            _frames.Add(new Frame { BaseIndent = 0 });
        }

        private Frame Cur => _frames[^1];

        public string Run()
        {
            int i = 0;
            while (i < _tokens.Count)
            {
                var tok = _tokens[i];

                if (tok.Kind is SqlTokenKind.LineComment or SqlTokenKind.BlockComment)
                {
                    if (!_freshLine) StartNewLine(0);
                    Append(tok.Text, tok);
                    StartNewLine(0);
                    i++;
                    continue;
                }

                if (tok.Kind == SqlTokenKind.Keyword && TryMatchClause(i, out var phrase, out var kind))
                {
                    StartNewLine(0);
                    // Use the actual matched tokens' original text (not the canonical phrase constants)
                    // so that UppercaseKeywords=false preserves the source's own casing.
                    var text = string.Join(" ", Enumerable.Range(0, phrase.Length).Select(k => TransformKeyword(_tokens[i + k].Text)));
                    Append(text, tok);
                    Cur.Current = kind;
                    i += phrase.Length;
                    if (kind == ClauseKind.Select)
                        StartNewLine(1);
                    continue;
                }

                if (tok.Kind == SqlTokenKind.Keyword &&
                    (tok.Text.Equals("AND", StringComparison.OrdinalIgnoreCase) || tok.Text.Equals("OR", StringComparison.OrdinalIgnoreCase)) &&
                    Cur.Current is ClauseKind.Where or ClauseKind.Having)
                {
                    StartNewLine(1);
                    Append(TransformKeyword(tok.Text), tok);
                    i++;
                    continue;
                }

                if (tok.Kind == SqlTokenKind.Comma && Cur.Current == ClauseKind.Select)
                {
                    Append(",", tok);
                    StartNewLine(1);
                    i++;
                    continue;
                }

                if (tok.Kind == SqlTokenKind.OpenParen)
                {
                    bool isSubquery = NextIsSelectOrWith(i + 1);
                    Append("(", tok);
                    _frames.Add(new Frame { BaseIndent = Cur.BaseIndent + (isSubquery ? 1 : 0), WasSubquery = isSubquery });
                    i++;
                    continue;
                }

                if (tok.Kind == SqlTokenKind.CloseParen)
                {
                    var frame = Cur;
                    if (_frames.Count > 1) _frames.RemoveAt(_frames.Count - 1);
                    if (frame.WasSubquery)
                        StartNewLine(0);
                    Append(")", tok);
                    i++;
                    continue;
                }

                var outText = tok.Kind == SqlTokenKind.Keyword ? TransformKeyword(tok.Text) : tok.Text;
                Append(outText, tok);
                i++;
            }
            return _sb.ToString();
        }

        private bool NextIsSelectOrWith(int index)
        {
            for (int j = index; j < _tokens.Count; j++)
            {
                var t = _tokens[j];
                if (t.Kind is SqlTokenKind.LineComment or SqlTokenKind.BlockComment) continue;
                return t.Kind == SqlTokenKind.Keyword &&
                       (t.Text.Equals("SELECT", StringComparison.OrdinalIgnoreCase) || t.Text.Equals("WITH", StringComparison.OrdinalIgnoreCase));
            }
            return false;
        }

        private bool TryMatchClause(int i, out string[] phrase, out ClauseKind kind)
        {
            foreach (var candidate in ClausePhrases)
            {
                if (i + candidate.Length > _tokens.Count) continue;
                bool match = true;
                for (int k = 0; k < candidate.Length; k++)
                {
                    var t = _tokens[i + k];
                    if (t.Kind != SqlTokenKind.Keyword || !t.Text.Equals(candidate[k], StringComparison.OrdinalIgnoreCase))
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    phrase = candidate;
                    kind = MapKind(candidate);
                    return true;
                }
            }
            phrase = Array.Empty<string>();
            kind = ClauseKind.None;
            return false;
        }

        private static ClauseKind MapKind(string[] phrase)
        {
            var joined = string.Join(" ", phrase).ToUpperInvariant();
            return joined switch
            {
                "SELECT" => ClauseKind.Select,
                "WHERE" => ClauseKind.Where,
                "HAVING" => ClauseKind.Having,
                _ => ClauseKind.Other,
            };
        }

        private string TransformKeyword(string text) => _options.UppercaseKeywords ? text.ToUpperInvariant() : text;

        private void StartNewLine(int extraIndent)
        {
            if (!_isFirstLine)
                _sb.Append('\n');
            _isFirstLine = false;
            _sb.Append(new string(' ', Math.Max(0, (Cur.BaseIndent + extraIndent) * _options.IndentSize)));
            _freshLine = true;
            _prevTok = null;
        }

        private void Append(string text, SqlToken tok)
        {
            if (!_freshLine && NeedsSpaceBefore(_prevTok, tok))
                _sb.Append(' ');
            _sb.Append(text);
            _freshLine = false;
            _prevTok = tok;
        }
    }

    internal static List<SqlToken> Tokenize(string sql)
    {
        var tokens = new List<SqlToken>();
        int i = 0;
        int n = sql.Length;
        bool sawWhitespace = false;
        while (i < n)
        {
            char c = sql[i];
            if (char.IsWhiteSpace(c)) { i++; sawWhitespace = true; continue; }

            bool spaceBefore = sawWhitespace;
            sawWhitespace = false;

            if (c == '-' && i + 1 < n && sql[i + 1] == '-')
            {
                int start = i;
                while (i < n && sql[i] != '\n') i++;
                tokens.Add(new SqlToken(SqlTokenKind.LineComment, sql[start..i].TrimEnd('\r'), spaceBefore));
                continue;
            }
            if (c == '/' && i + 1 < n && sql[i + 1] == '*')
            {
                int start = i;
                i += 2;
                while (i + 1 < n && !(sql[i] == '*' && sql[i + 1] == '/')) i++;
                i = Math.Min(i + 2, n);
                tokens.Add(new SqlToken(SqlTokenKind.BlockComment, sql[start..i], spaceBefore));
                continue;
            }
            if (c is '\'' or '"')
            {
                int start = i;
                char quote = c;
                i++;
                while (i < n)
                {
                    if (sql[i] == quote)
                    {
                        if (i + 1 < n && sql[i + 1] == quote) { i += 2; continue; }
                        i++;
                        break;
                    }
                    i++;
                }
                tokens.Add(new SqlToken(SqlTokenKind.String, sql[start..i], spaceBefore));
                continue;
            }
            if (c == '[')
            {
                int start = i;
                i++;
                while (i < n && sql[i] != ']') i++;
                if (i < n) i++;
                tokens.Add(new SqlToken(SqlTokenKind.QuotedIdentifier, sql[start..i], spaceBefore));
                continue;
            }
            if (c == '`')
            {
                int start = i;
                i++;
                while (i < n && sql[i] != '`') i++;
                if (i < n) i++;
                tokens.Add(new SqlToken(SqlTokenKind.QuotedIdentifier, sql[start..i], spaceBefore));
                continue;
            }
            if (char.IsDigit(c))
            {
                int start = i;
                while (i < n && char.IsDigit(sql[i])) i++;
                if (i < n && sql[i] == '.' && i + 1 < n && char.IsDigit(sql[i + 1]))
                {
                    i++;
                    while (i < n && char.IsDigit(sql[i])) i++;
                }
                if (i < n && (sql[i] == 'e' || sql[i] == 'E'))
                {
                    int save = i;
                    i++;
                    if (i < n && (sql[i] == '+' || sql[i] == '-')) i++;
                    if (i < n && char.IsDigit(sql[i])) { while (i < n && char.IsDigit(sql[i])) i++; }
                    else i = save;
                }
                tokens.Add(new SqlToken(SqlTokenKind.Number, sql[start..i], spaceBefore));
                continue;
            }
            if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i < n && (char.IsLetterOrDigit(sql[i]) || sql[i] == '_')) i++;
                var text = sql[start..i];
                tokens.Add(new SqlToken(Keywords.Contains(text) ? SqlTokenKind.Keyword : SqlTokenKind.Identifier, text, spaceBefore));
                continue;
            }
            if (c is '<' or '>' or '!' && i + 1 < n && sql[i + 1] == '=')
            {
                tokens.Add(new SqlToken(SqlTokenKind.Operator, sql.Substring(i, 2), spaceBefore));
                i += 2;
                continue;
            }
            if (c == '<' && i + 1 < n && sql[i + 1] == '>')
            {
                tokens.Add(new SqlToken(SqlTokenKind.Operator, "<>", spaceBefore));
                i += 2;
                continue;
            }
            if (c == '|' && i + 1 < n && sql[i + 1] == '|')
            {
                tokens.Add(new SqlToken(SqlTokenKind.Operator, "||", spaceBefore));
                i += 2;
                continue;
            }
            switch (c)
            {
                case ',': tokens.Add(new SqlToken(SqlTokenKind.Comma, ",", spaceBefore)); break;
                case '(': tokens.Add(new SqlToken(SqlTokenKind.OpenParen, "(", spaceBefore)); break;
                case ')': tokens.Add(new SqlToken(SqlTokenKind.CloseParen, ")", spaceBefore)); break;
                case ';': tokens.Add(new SqlToken(SqlTokenKind.Semicolon, ";", spaceBefore)); break;
                case '.': tokens.Add(new SqlToken(SqlTokenKind.Dot, ".", spaceBefore)); break;
                case '*': tokens.Add(new SqlToken(SqlTokenKind.Star, "*", spaceBefore)); break;
                default: tokens.Add(new SqlToken(SqlTokenKind.Operator, c.ToString(), spaceBefore)); break;
            }
            i++;
        }
        return tokens;
    }
}
