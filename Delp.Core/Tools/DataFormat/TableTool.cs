using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CsvHelper;
using CsvHelper.Configuration;

namespace Delp.Core.Tools.DataFormat;

public enum TableFormat
{
    Csv,
    Tsv,
    Markdown,
    Json,
}

public enum TableWriteTarget
{
    Markdown,
    AsciiBox,
    Html,
    SqlInsert,
    Json,
    Csv,
    Tsv,
    LaTeX,
}

public enum MarkdownAlign
{
    None,
    Left,
    Center,
    Right,
}

public enum AsciiBorderStyle
{
    Ascii,
    Unicode,
}

public enum JsonTableShape
{
    Objects,
    Arrays,
}

/// <summary>A parsed table. Every row is padded to <see cref="Headers"/>' length with "" — ragged
/// source rows never leave callers guessing which column a short row's cells belong to.</summary>
public sealed record TableData(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows)
{
    public static readonly TableData Empty = new([], []);
}

/// <summary>Options consumed by the subset of <see cref="TableTool.Write"/> targets that need them
/// (Markdown alignment, ASCII/Unicode box borders, the SQL table name, JSON output shape).</summary>
public sealed record TableWriteOptions(
    MarkdownAlign Alignment = MarkdownAlign.None,
    AsciiBorderStyle Borders = AsciiBorderStyle.Ascii,
    string SqlTableName = "table_name",
    JsonTableShape Shape = JsonTableShape.Objects);

/// <summary>Detects and converts tabular data between CSV, TSV, Markdown pipe-tables, JSON,
/// an ASCII/Unicode box, HTML, a single SQL INSERT, and a LaTeX tabular.</summary>
public static class TableTool
{
    /// <summary>TSV wins over CSV whenever a tab is present — pasting from Excel/Sheets produces
    /// TSV, and a comma-delimited file practically never contains a raw tab.</summary>
    public static TableFormat Detect(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var trimmed = text.TrimStart();
        if (trimmed.Length > 0 && (trimmed[0] == '{' || trimmed[0] == '['))
            return TableFormat.Json;

        if (text.Contains('\t'))
            return TableFormat.Tsv;

        if (LooksLikeMarkdown(text))
            return TableFormat.Markdown;

        return TableFormat.Csv;
    }

    /// <exception cref="FormatException">The input could not be parsed as the given format.</exception>
    public static TableData Parse(string text, TableFormat format, bool hasHeader = true, char delimiter = ',')
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Trim().Length == 0)
            return TableData.Empty;

        return format switch
        {
            TableFormat.Csv => ParseDelimited(text, delimiter, hasHeader),
            TableFormat.Tsv => ParseDelimited(text, '\t', hasHeader),
            TableFormat.Markdown => ParseMarkdown(text),
            TableFormat.Json => ParseJson(text, hasHeader),
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
    }

    public static string Write(TableData data, TableWriteTarget target, TableWriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        options ??= new TableWriteOptions();
        return target switch
        {
            TableWriteTarget.Markdown => WriteMarkdown(data, options.Alignment),
            TableWriteTarget.AsciiBox => WriteAsciiBox(data, options.Borders),
            TableWriteTarget.Html => WriteHtml(data),
            TableWriteTarget.SqlInsert => WriteSqlInsert(data, options.SqlTableName),
            TableWriteTarget.Json => WriteJson(data, options.Shape),
            TableWriteTarget.Csv => WriteDelimited(data, ','),
            TableWriteTarget.Tsv => WriteDelimited(data, '\t'),
            TableWriteTarget.LaTeX => WriteLatex(data),
            _ => throw new ArgumentOutOfRangeException(nameof(target)),
        };
    }

    // ---------------- detection ----------------

    private static bool LooksLikeMarkdown(string text)
    {
        var lines = SplitNonEmptyLines(text);
        if (lines.Count < 2 || !lines[0].Contains('|'))
            return false;
        return IsMarkdownSeparatorRow(lines[1]);
    }

    // ---------------- CSV / TSV ----------------

    private static TableData ParseDelimited(string text, char delimiter, bool hasHeader)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
            HasHeaderRecord = false,
            BadDataFound = null,
            MissingFieldFound = null,
            IgnoreBlankLines = true,
        };

        using var textReader = new StringReader(text);
        using var csvReader = new CsvReader(textReader, config);

        var rows = new List<string[]>();
        while (csvReader.Read())
        {
            var record = csvReader.Parser.Record;
            if (record is null || record.Length == 0)
                continue;
            rows.Add(record);
        }

        if (rows.Count == 0)
            return TableData.Empty;

        var maxCols = rows.Max(r => r.Length);

        if (hasHeader)
        {
            var headers = PadRow(rows[0], maxCols);
            var dataRows = rows.Skip(1).Select(r => (IReadOnlyList<string>)PadRow(r, maxCols)).ToList();
            return new TableData(headers, dataRows);
        }
        else
        {
            var headers = GeneratedHeaders(maxCols);
            var dataRows = rows.Select(r => (IReadOnlyList<string>)PadRow(r, maxCols)).ToList();
            return new TableData(headers, dataRows);
        }
    }

    // CSV-injection caveat: cells beginning with = + - @ (or tab/CR) are written verbatim. This keeps
    // the output structurally valid, lossless CSV (prefixing a guard character would silently corrupt
    // the data and break round-tripping), but a spreadsheet that opens the file may interpret such a
    // cell as a formula. Callers exporting untrusted data for Excel should sanitize on their side.
    private static string WriteDelimited(TableData data, char delimiter)
    {
        if (data.Headers.Count == 0)
            return "";

        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = delimiter.ToString(), NewLine = "\n" };
        using var stringWriter = new StringWriter();
        using (var csvWriter = new CsvWriter(stringWriter, config))
        {
            foreach (var h in data.Headers)
                csvWriter.WriteField(h);
            csvWriter.NextRecord();

            foreach (var row in data.Rows)
            {
                for (var c = 0; c < data.Headers.Count; c++)
                    csvWriter.WriteField(c < row.Count ? row[c] : "");
                csvWriter.NextRecord();
            }
        }
        return stringWriter.ToString().TrimEnd('\n');
    }

    // ---------------- Markdown ----------------

    private static TableData ParseMarkdown(string text)
    {
        var lines = SplitNonEmptyLines(text);
        if (lines.Count == 0)
            return TableData.Empty;

        var headerCells = ParseMarkdownRow(lines[0]);
        var dataLines = lines.Skip(1).ToList();
        if (dataLines.Count > 0 && IsMarkdownSeparatorRow(dataLines[0]))
            dataLines = dataLines.Skip(1).ToList();

        var rows = dataLines.Select(ParseMarkdownRow).ToList();
        var maxCols = Math.Max(headerCells.Count, rows.Count == 0 ? 0 : rows.Max(r => r.Count));

        var headers = PadRow(headerCells, maxCols);
        var dataRows = rows.Select(r => (IReadOnlyList<string>)PadRow(r, maxCols)).ToList();
        return new TableData(headers, dataRows);
    }

    /// <summary>Splits a "| a | b |" row on unescaped '|', trimming each cell and unescaping "\|".</summary>
    private static List<string> ParseMarkdownRow(string line)
    {
        var trimmed = StripOuterPipes(line.Trim());

        var cells = new List<string>();
        var sb = new StringBuilder();
        for (var i = 0; i < trimmed.Length; i++)
        {
            var c = trimmed[i];
            if (c == '\\' && i + 1 < trimmed.Length && trimmed[i + 1] == '|')
            {
                sb.Append('|');
                i++;
            }
            else if (c == '|')
            {
                cells.Add(sb.ToString().Trim());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        cells.Add(sb.ToString().Trim());
        return cells;
    }

    private static string StripOuterPipes(string s)
    {
        if (s.StartsWith('|'))
            s = s[1..];
        if (s.EndsWith('|') && !IsEscapedTrailingPipe(s))
            s = s[..^1];
        return s;
    }

    /// <summary>True when the trailing '|' is preceded by an odd number of backslashes (i.e. escaped).</summary>
    private static bool IsEscapedTrailingPipe(string s)
    {
        var backslashes = 0;
        var i = s.Length - 2;
        while (i >= 0 && s[i] == '\\')
        {
            backslashes++;
            i--;
        }
        return backslashes % 2 == 1;
    }

    private static bool IsMarkdownSeparatorRow(string line)
    {
        var cells = ParseMarkdownRow(line);
        return cells.Count > 0 && cells.All(IsAlignmentSegment);
    }

    private static bool IsAlignmentSegment(string cell)
    {
        var t = cell.Trim();
        var inner = t.Trim(':');
        return inner.Length > 0 && inner.All(ch => ch == '-');
    }

    private static string WriteMarkdown(TableData data, MarkdownAlign align)
    {
        if (data.Headers.Count == 0)
            return "";

        var sb = new StringBuilder();
        AppendMarkdownRow(sb, data.Headers, EscapeMarkdownCell);
        AppendMarkdownRow(sb, data.Headers.Select(_ => AlignmentSegment(align)), s => s);
        foreach (var row in data.Rows)
        {
            var cells = Enumerable.Range(0, data.Headers.Count).Select(c => c < row.Count ? row[c] : "");
            AppendMarkdownRow(sb, cells, EscapeMarkdownCell);
        }
        return sb.ToString().TrimEnd('\n');
    }

    private static void AppendMarkdownRow(StringBuilder sb, IEnumerable<string> cells, Func<string, string> escape)
    {
        sb.Append('|');
        foreach (var cell in cells)
            sb.Append(' ').Append(escape(cell)).Append(" |");
        sb.Append('\n');
    }

    private static string AlignmentSegment(MarkdownAlign align) => align switch
    {
        MarkdownAlign.Left => ":---",
        MarkdownAlign.Center => ":---:",
        MarkdownAlign.Right => "---:",
        _ => "---",
    };

    private static string EscapeMarkdownCell(string s) =>
        s.Replace("\\", "\\\\").Replace("|", "\\|").Replace("\n", "<br>").Replace("\r", "");

    // ---------------- JSON ----------------

    private static TableData ParseJson(string text, bool hasHeader)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(text);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Invalid JSON: {ex.Message}", ex);
        }

        if (node is not JsonArray array)
            throw new FormatException("JSON input must be an array of objects or an array of arrays.");

        if (array.Count == 0)
            return TableData.Empty;

        if (array[0] is JsonObject)
            return ParseJsonObjects(array);
        if (array[0] is JsonArray)
            return ParseJsonArrays(array, hasHeader);

        throw new FormatException("JSON array elements must all be objects or all be arrays.");
    }

    private static TableData ParseJsonObjects(JsonArray array)
    {
        var headers = new List<string>();
        var seen = new HashSet<string>();
        foreach (var item in array)
        {
            if (item is not JsonObject obj)
                throw new FormatException("All array elements must be objects to convert to a table.");
            foreach (var key in obj.Select(kv => kv.Key))
                if (seen.Add(key))
                    headers.Add(key);
        }

        var rows = new List<IReadOnlyList<string>>();
        foreach (var item in array)
        {
            var obj = (JsonObject)item!;
            rows.Add(headers.Select(h => obj.TryGetPropertyValue(h, out var v) ? JsonCellToString(v) : "").ToList());
        }
        return new TableData(headers, rows);
    }

    private static TableData ParseJsonArrays(JsonArray array, bool hasHeader)
    {
        var rows = new List<List<string>>();
        foreach (var item in array)
        {
            if (item is not JsonArray inner)
                throw new FormatException("All array elements must be arrays to convert to a table.");
            rows.Add(inner.Select(JsonCellToString).ToList());
        }

        var maxCols = rows.Max(r => r.Count);
        if (hasHeader)
        {
            var headers = PadRow(rows[0], maxCols);
            var dataRows = rows.Skip(1).Select(r => (IReadOnlyList<string>)PadRow(r, maxCols)).ToList();
            return new TableData(headers, dataRows);
        }
        else
        {
            var headers = GeneratedHeaders(maxCols);
            var dataRows = rows.Select(r => (IReadOnlyList<string>)PadRow(r, maxCols)).ToList();
            return new TableData(headers, dataRows);
        }
    }

    private static string JsonCellToString(JsonNode? node) => node switch
    {
        null => "",
        JsonValue v when v.TryGetValue<string>(out var s) => s,
        _ => node.ToJsonString(),
    };

    private static string WriteJson(TableData data, JsonTableShape shape)
    {
        if (data.Headers.Count == 0)
            return "[]";

        var array = new JsonArray();
        if (shape == JsonTableShape.Arrays)
        {
            array.Add(new JsonArray(data.Headers.Select(h => (JsonNode?)JsonValue.Create(h)).ToArray()));
            foreach (var row in data.Rows)
                array.Add(new JsonArray(Enumerable.Range(0, data.Headers.Count)
                    .Select(c => (JsonNode?)JsonValue.Create(c < row.Count ? row[c] : "")).ToArray()));
        }
        else
        {
            foreach (var row in data.Rows)
            {
                var obj = new JsonObject();
                for (var c = 0; c < data.Headers.Count; c++)
                    obj[data.Headers[c]] = JsonValue.Create(c < row.Count ? row[c] : "");
                array.Add(obj);
            }
        }
        return JsonFormatTool.FormatNode(array);
    }

    // ---------------- ASCII / Unicode box ----------------

    // Width caveat: column widths use string length in UTF-16 code units. That is correct for ASCII
    // and BMP text, but full-width CJK and most emoji occupy two terminal columns (and emoji/astral
    // chars are two code units), while combining marks occupy zero — so a table containing them will
    // look slightly misaligned in a fixed-width font. A precise fix needs an East-Asian-width table;
    // it is intentionally out of scope here.
    private static string WriteAsciiBox(TableData data, AsciiBorderStyle style)
    {
        var cols = data.Headers.Count;
        if (cols == 0)
            return "";

        var widths = new int[cols];
        for (var c = 0; c < cols; c++)
            widths[c] = data.Headers[c].Length;
        foreach (var row in data.Rows)
            for (var c = 0; c < cols; c++)
                widths[c] = Math.Max(widths[c], (c < row.Count ? row[c] : "").Length);

        var (h, v, tl, tm, tr, ml, mm, mr, bl, bm, br) = style == AsciiBorderStyle.Unicode
            ? ('─', '│', '┌', '┬', '┐', '├', '┼', '┤', '└', '┴', '┘')
            : ('-', '|', '+', '+', '+', '+', '+', '+', '+', '+', '+');

        var sb = new StringBuilder();
        AppendBoxBorder(sb, widths, h, tl, tm, tr);
        AppendBoxRow(sb, data.Headers, widths, v);
        AppendBoxBorder(sb, widths, h, ml, mm, mr);
        foreach (var row in data.Rows)
            AppendBoxRow(sb, row, widths, v);
        AppendBoxBorder(sb, widths, h, bl, bm, br);
        return sb.ToString().TrimEnd('\n');
    }

    private static void AppendBoxBorder(StringBuilder sb, int[] widths, char h, char left, char mid, char right)
    {
        sb.Append(left);
        for (var c = 0; c < widths.Length; c++)
        {
            sb.Append(new string(h, widths[c] + 2));
            sb.Append(c < widths.Length - 1 ? mid : right);
        }
        sb.Append('\n');
    }

    private static void AppendBoxRow(StringBuilder sb, IReadOnlyList<string> cells, int[] widths, char v)
    {
        sb.Append(v);
        for (var c = 0; c < widths.Length; c++)
        {
            var cell = c < cells.Count ? cells[c] : "";
            sb.Append(' ').Append(cell.PadRight(widths[c])).Append(' ').Append(v);
        }
        sb.Append('\n');
    }

    // ---------------- HTML ----------------

    private static string WriteHtml(TableData data)
    {
        if (data.Headers.Count == 0)
            return "";

        var sb = new StringBuilder();
        sb.Append("<table>\n  <thead>\n    <tr>");
        foreach (var h in data.Headers)
            sb.Append("<th>").Append(HtmlEscape(h)).Append("</th>");
        sb.Append("</tr>\n  </thead>\n  <tbody>\n");
        foreach (var row in data.Rows)
        {
            sb.Append("    <tr>");
            for (var c = 0; c < data.Headers.Count; c++)
                sb.Append("<td>").Append(HtmlEscape(c < row.Count ? row[c] : "")).Append("</td>");
            sb.Append("</tr>\n");
        }
        sb.Append("  </tbody>\n</table>");
        return sb.ToString();
    }

    private static string HtmlEscape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    // ---------------- SQL ----------------

    private static string WriteSqlInsert(TableData data, string tableName)
    {
        if (data.Headers.Count == 0 || data.Rows.Count == 0)
            return "";

        var name = string.IsNullOrWhiteSpace(tableName) ? "table_name" : tableName.Trim();
        var sb = new StringBuilder();
        sb.Append("INSERT INTO ").Append(QuoteSqlIdentifier(name))
          .Append(" (").Append(string.Join(", ", data.Headers.Select(QuoteSqlIdentifier))).Append(")\nVALUES\n");

        for (var r = 0; r < data.Rows.Count; r++)
        {
            var row = data.Rows[r];
            var values = Enumerable.Range(0, data.Headers.Count).Select(c => SqlLiteral(c < row.Count ? row[c] : ""));
            sb.Append("  (").Append(string.Join(", ", values)).Append(')');
            sb.Append(r < data.Rows.Count - 1 ? ",\n" : ";\n");
        }
        return sb.ToString().TrimEnd('\n');
    }

    /// <summary>Every cell is emitted as a quoted string literal with single quotes doubled — note this
    /// means an empty cell becomes '' (an empty string), never SQL NULL, and a source JSON null (which
    /// the string-based <see cref="TableData"/> model already collapsed to "") is likewise indistinguishable
    /// from an empty string here.</summary>
    private static string SqlLiteral(string s) => "'" + s.Replace("'", "''") + "'";

    /// <summary>Leaves a plain identifier (ASCII letters/digits/underscore, not starting with a digit)
    /// untouched; anything else — spaces, quotes, brackets, reserved punctuation — is wrapped in ANSI
    /// double quotes with embedded double quotes doubled, so a column header like <c>first name</c> or
    /// <c>a"b</c> or <c>[id]</c> can't produce broken (or injected) SQL. Dialect note: SQL Server uses
    /// <c>[ ]</c> and MySQL backticks; the ANSI double-quote form is the portable default.</summary>
    private static string QuoteSqlIdentifier(string name) =>
        IsPlainSqlIdentifier(name) ? name : "\"" + name.Replace("\"", "\"\"") + "\"";

    private static bool IsPlainSqlIdentifier(string s)
    {
        if (s.Length == 0 || char.IsAsciiDigit(s[0]))
            return false;
        foreach (var c in s)
            if (!(char.IsAsciiLetterOrDigit(c) || c == '_'))
                return false;
        return true;
    }

    // ---------------- LaTeX ----------------

    private static string WriteLatex(TableData data)
    {
        var cols = data.Headers.Count;
        if (cols == 0)
            return "";

        var sb = new StringBuilder();
        sb.Append("\\begin{tabular}{").Append(new string('l', cols)).Append("}\n\\hline\n");
        sb.Append(string.Join(" & ", data.Headers.Select(EscapeLatex))).Append(" \\\\\n\\hline\n");
        foreach (var row in data.Rows)
        {
            var cells = Enumerable.Range(0, cols).Select(c => EscapeLatex(c < row.Count ? row[c] : ""));
            sb.Append(string.Join(" & ", cells)).Append(" \\\\\n");
        }
        sb.Append("\\hline\n\\end{tabular}");
        return sb.ToString();
    }

    private static string EscapeLatex(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\textbackslash{}"); break;
                case '&': sb.Append("\\&"); break;
                case '%': sb.Append("\\%"); break;
                case '$': sb.Append("\\$"); break;
                case '#': sb.Append("\\#"); break;
                case '_': sb.Append("\\_"); break;
                case '{': sb.Append("\\{"); break;
                case '}': sb.Append("\\}"); break;
                case '~': sb.Append("\\textasciitilde{}"); break;
                case '^': sb.Append("\\textasciicircum{}"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    // ---------------- shared helpers ----------------

    private static List<string> SplitNonEmptyLines(string text) =>
        text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n').Where(l => l.Trim().Length > 0).ToList();

    private static List<string> GeneratedHeaders(int count) =>
        Enumerable.Range(1, count).Select(i => "Column" + i.ToString(CultureInfo.InvariantCulture)).ToList();

    private static List<string> PadRow(IReadOnlyList<string> row, int count)
    {
        if (row.Count >= count)
            return row is List<string> list ? list : [.. row];
        var padded = new List<string>(count);
        padded.AddRange(row);
        while (padded.Count < count)
            padded.Add("");
        return padded;
    }
}
