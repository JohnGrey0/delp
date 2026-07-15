using Delp.Core.Tools.DataFormat;

namespace Delp.Core.Tests.Tools.DataFormat;

public class TableToolTests
{
    private static readonly TableData Simple = new(
        ["name", "age"],
        [["Alice", "30"], ["Bob", "25"]]);

    // ---------------- Detect ----------------

    [Theory]
    [InlineData("a,b\n1,2\n", TableFormat.Csv)]
    [InlineData("a\tb\n1\t2\n", TableFormat.Tsv)]
    [InlineData("| a | b |\n|---|---|\n| 1 | 2 |\n", TableFormat.Markdown)]
    [InlineData("[{\"a\":1}]", TableFormat.Json)]
    [InlineData("  [1,2,3]", TableFormat.Json)]
    [InlineData("{\"a\":1}", TableFormat.Json)]
    public void Detect_ReturnsExpectedFormat(string text, TableFormat expected)
    {
        Assert.Equal(expected, TableTool.Detect(text));
    }

    [Fact]
    public void Detect_TabPresent_WinsOverMarkdownLikeContent()
    {
        // Excel/Sheets paste is TSV even if a cell happens to contain a pipe.
        Assert.Equal(TableFormat.Tsv, TableTool.Detect("a|b\tc\n1|2\t3\n"));
    }

    [Fact]
    public void Detect_PlainCsv_FallsBackWhenNoOtherSignal()
    {
        Assert.Equal(TableFormat.Csv, TableTool.Detect("a,b,c\n1,2,3\n"));
    }

    // ---------------- CSV / TSV parsing ----------------

    [Fact]
    public void Parse_Csv_QuotedCellsWithCommasAndNewlines()
    {
        const string csv = "name,note\nAlice,\"hello, world\nsecond line\"\n";
        var data = TableTool.Parse(csv, TableFormat.Csv);
        Assert.Equal(["name", "note"], data.Headers);
        Assert.Equal("hello, world\nsecond line", data.Rows[0][1]);
    }

    [Fact]
    public void Parse_Csv_NoHeader_GeneratesColumnNames()
    {
        var data = TableTool.Parse("a,1\nb,2\n", TableFormat.Csv, hasHeader: false);
        Assert.Equal(["Column1", "Column2"], data.Headers);
        Assert.Equal(2, data.Rows.Count);
    }

    [Fact]
    public void Parse_Csv_RaggedRows_PadWithEmptyString()
    {
        var data = TableTool.Parse("a,b,c\n1,2\n3,4,5,6\n", TableFormat.Csv);
        Assert.Equal(4, data.Headers.Count); // padded to widest row (4 cols)
        Assert.Equal("", data.Headers[3]);
        Assert.Equal(["1", "2", "", ""], data.Rows[0]);
        Assert.Equal(["3", "4", "5", "6"], data.Rows[1]);
    }

    [Fact]
    public void Parse_Tsv_SplitsOnTabRegardlessOfDelimiterArgument()
    {
        var data = TableTool.Parse("a\tb\n1\t2\n", TableFormat.Tsv, delimiter: ',');
        Assert.Equal(["a", "b"], data.Headers);
        Assert.Equal(["1", "2"], data.Rows[0]);
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsEmptyTable()
    {
        var data = TableTool.Parse("", TableFormat.Csv);
        Assert.Empty(data.Headers);
        Assert.Empty(data.Rows);
    }

    [Fact]
    public void Parse_WhitespaceOnlyInput_ReturnsEmptyTable()
    {
        var data = TableTool.Parse("   \n  ", TableFormat.Markdown);
        Assert.Empty(data.Headers);
    }

    // ---------------- Markdown parsing ----------------

    [Fact]
    public void Parse_Markdown_BasicTable()
    {
        const string md = "| name | age |\n| --- | --- |\n| Alice | 30 |\n| Bob | 25 |\n";
        var data = TableTool.Parse(md, TableFormat.Markdown);
        Assert.Equal(["name", "age"], data.Headers);
        Assert.Equal(2, data.Rows.Count);
        Assert.Equal(["Alice", "30"], data.Rows[0]);
    }

    [Fact]
    public void Parse_Markdown_EscapedPipeIsUnescapedIntoCell()
    {
        const string md = "| a | b |\n| --- | --- |\n| x\\|y | 2 |\n";
        var data = TableTool.Parse(md, TableFormat.Markdown);
        Assert.Equal("x|y", data.Rows[0][0]);
    }

    [Fact]
    public void Parse_Markdown_AlignmentSeparatorRowIsNotTreatedAsData()
    {
        const string md = "| a | b |\n|:---|---:|\n| 1 | 2 |\n";
        var data = TableTool.Parse(md, TableFormat.Markdown);
        Assert.Single(data.Rows);
        Assert.Equal(["1", "2"], data.Rows[0]);
    }

    // ---------------- JSON parsing ----------------

    [Fact]
    public void Parse_Json_ArrayOfObjects_UsesUnionOfKeysInFirstSeenOrder()
    {
        const string json = """[{"a":1,"b":2},{"a":3,"c":4}]""";
        var data = TableTool.Parse(json, TableFormat.Json);
        Assert.Equal(["a", "b", "c"], data.Headers);
        Assert.Equal(["1", "2", ""], data.Rows[0]);
        Assert.Equal(["3", "", "4"], data.Rows[1]);
    }

    [Fact]
    public void Parse_Json_ArrayOfArrays_WithHeaderRow()
    {
        const string json = """[["a","b"],["1","2"]]""";
        var data = TableTool.Parse(json, TableFormat.Json, hasHeader: true);
        Assert.Equal(["a", "b"], data.Headers);
        Assert.Single(data.Rows);
    }

    [Fact]
    public void Parse_Json_ArrayOfArrays_WithoutHeaderRow_GeneratesColumnNames()
    {
        const string json = """[["1","2"],["3","4"]]""";
        var data = TableTool.Parse(json, TableFormat.Json, hasHeader: false);
        Assert.Equal(["Column1", "Column2"], data.Headers);
        Assert.Equal(2, data.Rows.Count);
    }

    [Fact]
    public void Parse_Json_InvalidJson_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => TableTool.Parse("{not json", TableFormat.Json));
    }

    [Fact]
    public void Parse_Json_NonArrayRoot_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => TableTool.Parse("""{"a":1}""", TableFormat.Json));
    }

    // ---------------- Write: Markdown ----------------

    [Fact]
    public void Write_Markdown_DefaultAlignment_UsesPlainDashes()
    {
        var md = TableTool.Write(Simple, TableWriteTarget.Markdown);
        var lines = md.Split('\n');
        Assert.Equal("| name | age |", lines[0]);
        Assert.Equal("| --- | --- |", lines[1]);
    }

    [Theory]
    [InlineData(MarkdownAlign.Left, ":---")]
    [InlineData(MarkdownAlign.Center, ":---:")]
    [InlineData(MarkdownAlign.Right, "---:")]
    public void Write_Markdown_AlignmentAppliesToEveryColumn(MarkdownAlign align, string segment)
    {
        var md = TableTool.Write(Simple, TableWriteTarget.Markdown, new TableWriteOptions(Alignment: align));
        var separatorLine = md.Split('\n')[1];
        Assert.Equal($"| {segment} | {segment} |", separatorLine);
    }

    [Fact]
    public void Write_Markdown_EscapesPipeAndBackslash()
    {
        var data = new TableData(["a"], [["x|y\\z"]]);
        var md = TableTool.Write(data, TableWriteTarget.Markdown);
        Assert.Contains("x\\|y\\\\z", md);
    }

    // ---------------- Write: AsciiBox ----------------

    [Fact]
    public void Write_AsciiBox_UsesPlusAndDashBorders()
    {
        var box = TableTool.Write(Simple, TableWriteTarget.AsciiBox);
        Assert.StartsWith("+", box);
        Assert.Contains("-+-", box);
        Assert.Contains("| name", box);
    }

    [Fact]
    public void Write_AsciiBox_UnicodeStyle_UsesBoxDrawingChars()
    {
        var box = TableTool.Write(Simple, TableWriteTarget.AsciiBox, new TableWriteOptions(Borders: AsciiBorderStyle.Unicode));
        Assert.StartsWith("┌", box);
        Assert.Contains("─┬─", box);
    }

    // ---------------- Write: SQL ----------------

    [Fact]
    public void Write_SqlInsert_SingleStatementWithValueListPerRow()
    {
        var sql = TableTool.Write(Simple, TableWriteTarget.SqlInsert, new TableWriteOptions(SqlTableName: "people"));
        Assert.Equal(
            "INSERT INTO people (name, age)\nVALUES\n  ('Alice', '30'),\n  ('Bob', '25');",
            sql);
    }

    [Fact]
    public void Write_SqlInsert_EscapesEmbeddedSingleQuotes()
    {
        var data = new TableData(["name"], [["O'Brien"]]);
        var sql = TableTool.Write(data, TableWriteTarget.SqlInsert);
        Assert.Contains("'O''Brien'", sql);
    }

    [Fact]
    public void Write_SqlInsert_EmptyTable_ReturnsEmptyString()
    {
        Assert.Equal("", TableTool.Write(TableData.Empty, TableWriteTarget.SqlInsert));
    }

    // ---------------- Write: JSON ----------------

    [Fact]
    public void Write_Json_ObjectsShape_RoundTripsThroughParse()
    {
        var json = TableTool.Write(Simple, TableWriteTarget.Json);
        var reparsed = TableTool.Parse(json, TableFormat.Json);
        Assert.Equal(Simple.Headers, reparsed.Headers);
        Assert.Equal(Simple.Rows, reparsed.Rows);
    }

    [Fact]
    public void Write_Json_ArraysShape_FirstRowIsHeader()
    {
        var json = TableTool.Write(Simple, TableWriteTarget.Json, new TableWriteOptions(Shape: JsonTableShape.Arrays));
        var reparsed = TableTool.Parse(json, TableFormat.Json, hasHeader: true);
        Assert.Equal(Simple.Headers, reparsed.Headers);
        Assert.Equal(Simple.Rows, reparsed.Rows);
    }

    // ---------------- Write: LaTeX ----------------

    [Fact]
    public void Write_Latex_EscapesSpecialCharacters()
    {
        var data = new TableData(["a"], [["100% & $5_x #{y}~z^2 \\end"]]);
        var latex = TableTool.Write(data, TableWriteTarget.LaTeX);
        Assert.Contains("100\\% \\& \\$5\\_x \\#\\{y\\}\\textasciitilde{}z\\textasciicircum{}2 \\textbackslash{}end", latex);
        Assert.Contains("\\begin{tabular}{l}", latex);
        Assert.Contains("\\end{tabular}", latex);
    }

    // ---------------- Write: CSV / TSV / HTML ----------------

    [Fact]
    public void Write_Csv_QuotesFieldsWithEmbeddedComma()
    {
        var data = new TableData(["a"], [["hello, world"]]);
        var csv = TableTool.Write(data, TableWriteTarget.Csv);
        Assert.Contains("\"hello, world\"", csv);
    }

    [Fact]
    public void Write_Tsv_UsesTabDelimiter()
    {
        var tsv = TableTool.Write(Simple, TableWriteTarget.Tsv);
        Assert.Equal("name\tage", tsv.Split('\n')[0]);
    }

    [Fact]
    public void Write_Html_EscapesReservedCharacters()
    {
        var data = new TableData(["a"], [["<b>&\"</b>"]]);
        var html = TableTool.Write(data, TableWriteTarget.Html);
        Assert.Contains("&lt;b&gt;&amp;&quot;&lt;/b&gt;", html);
    }

    // ---------------- round trip across all writers ----------------

    [Theory]
    [InlineData(TableWriteTarget.Markdown)]
    [InlineData(TableWriteTarget.AsciiBox)]
    [InlineData(TableWriteTarget.Html)]
    [InlineData(TableWriteTarget.SqlInsert)]
    [InlineData(TableWriteTarget.Json)]
    [InlineData(TableWriteTarget.Csv)]
    [InlineData(TableWriteTarget.Tsv)]
    [InlineData(TableWriteTarget.LaTeX)]
    public void Write_NeverThrows_ForEveryTarget(TableWriteTarget target)
    {
        var output = TableTool.Write(Simple, target);
        Assert.NotNull(output);
    }
}
