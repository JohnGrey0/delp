using Delp.Core.Tools.TextProcessing;

namespace Delp.Core.Tests.Tools.TextProcessing;

public class TextListToolTests
{
    private static readonly TextListOptions Defaults = new(
        Trim: true, RemoveEmpty: true, Dedupe: false, Lowercase: false, StripPunctuation: false);

    // ---------------------------------------------------------------- Split: Words

    [Fact]
    public void Split_Words_HandlesApostrophesHyphensAndPunctuation()
    {
        var result = TextListTool.Split("It's a well-known fact, right?", SplitMode.Words, null, Defaults);
        Assert.Equal(["It's", "a", "well-known", "fact", "right"], result);
    }

    [Fact]
    public void Split_Words_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(TextListTool.Split("", SplitMode.Words, null, Defaults));
    }

    // ---------------------------------------------------------------- Split: Lines

    [Fact]
    public void Split_Lines_TrimsAndRemovesEmpty()
    {
        var result = TextListTool.Split("  a  \n\nb\n   \nc", SplitMode.Lines, null, Defaults);
        Assert.Equal(["a", "b", "c"], result);
    }

    // ---------------------------------------------------------------- Split: Delimiter

    [Fact]
    public void Split_Delimiter_DefaultComma_TrimsAndRemovesEmpty()
    {
        var result = TextListTool.Split("a, b,, c", SplitMode.Delimiter, ",", Defaults);
        Assert.Equal(["a", "b", "c"], result);
    }

    [Fact]
    public void Split_Delimiter_CustomMultiCharDelimiter()
    {
        var result = TextListTool.Split("a::b::c", SplitMode.Delimiter, "::", Defaults);
        Assert.Equal(["a", "b", "c"], result);
    }

    [Fact]
    public void Split_Delimiter_NullOrEmptyDelimiter_FallsBackToComma()
    {
        var result = TextListTool.Split("a,b,c", SplitMode.Delimiter, null, Defaults);
        Assert.Equal(["a", "b", "c"], result);
    }

    // ---------------------------------------------------------------- Split: StripPunctuation

    [Fact]
    public void Split_StripPunctuation_TrimsEdgesOnlyInDelimiterMode()
    {
        var options = Defaults with { StripPunctuation = true };
        var result = TextListTool.Split("(cat), (dog)", SplitMode.Delimiter, ",", options);
        Assert.Equal(["cat", "dog"], result);
    }

    [Fact]
    public void Split_StripPunctuation_DoesNotAffectWordsMode()
    {
        var options = Defaults with { StripPunctuation = true };
        var result = TextListTool.Split("cat, dog!", SplitMode.Words, null, options);
        Assert.Equal(["cat", "dog"], result);
    }

    // ---------------------------------------------------------------- Split: Dedupe + Lowercase interplay

    [Fact]
    public void Split_Dedupe_WithLowercase_IsCaseInsensitive()
    {
        var options = Defaults with { Dedupe = true, Lowercase = true };
        var result = TextListTool.Split("Cat cat DOG", SplitMode.Words, null, options);
        Assert.Equal(["cat", "dog"], result);
    }

    [Fact]
    public void Split_Dedupe_WithoutLowercase_IsOrdinal()
    {
        var options = Defaults with { Dedupe = true, Lowercase = false };
        var result = TextListTool.Split("Cat cat DOG", SplitMode.Words, null, options);
        Assert.Equal(["Cat", "cat", "DOG"], result);
    }

    // ---------------------------------------------------------------- Format: PythonList / JsArray

    [Fact]
    public void Format_PythonList_SingleQuote()
    {
        Assert.Equal("['a', 'b']", TextListTool.Format(["a", "b"], ListFormat.PythonList, QuoteChar.Single));
    }

    [Fact]
    public void Format_PythonList_DoubleQuote_EscapesEmbeddedQuote()
    {
        var result = TextListTool.Format(["he said \"hi\""], ListFormat.PythonList, QuoteChar.Double);
        Assert.Equal("[\"he said \\\"hi\\\"\"]", result);
    }

    [Fact]
    public void Format_PythonList_NoQuote_LeavesItemsBare()
    {
        Assert.Equal("[a, b]", TextListTool.Format(["a", "b"], ListFormat.PythonList, QuoteChar.None));
    }

    [Fact]
    public void Format_PythonList_EscapesBackslash()
    {
        Assert.Equal("['a\\\\b']", TextListTool.Format(["a\\b"], ListFormat.PythonList, QuoteChar.Single));
    }

    [Fact]
    public void Format_JsArray_DoubleQuote()
    {
        Assert.Equal("[\"a\", \"b\"]", TextListTool.Format(["a", "b"], ListFormat.JsArray, QuoteChar.Double));
    }

    // ---------------------------------------------------------------- Format: JsonArray

    [Fact]
    public void Format_JsonArray_AlwaysDoubleQuotes_IgnoresQuoteOption()
    {
        var result = TextListTool.Format(["O'Brien", "he said \"hi\""], ListFormat.JsonArray, QuoteChar.Single);
        Assert.Equal("[\"O'Brien\",\"he said \\\"hi\\\"\"]", result);
    }

    [Fact]
    public void Format_JsonArray_Empty_ReturnsEmptyArray()
    {
        Assert.Equal("[]", TextListTool.Format([], ListFormat.JsonArray, QuoteChar.Double));
    }

    // ---------------------------------------------------------------- Format: CSharpArray

    [Fact]
    public void Format_CSharpArray_AlwaysDoubleQuotes()
    {
        Assert.Equal("new[] { \"a\", \"b\" }", TextListTool.Format(["a", "b"], ListFormat.CSharpArray, QuoteChar.Single));
    }

    [Fact]
    public void Format_CSharpArray_Empty()
    {
        Assert.Equal("new[] { }", TextListTool.Format([], ListFormat.CSharpArray, QuoteChar.Double));
    }

    // ---------------------------------------------------------------- Format: SqlIn

    [Fact]
    public void Format_SqlIn_DoublesEmbeddedSingleQuote_IgnoresQuoteOption()
    {
        var result = TextListTool.Format(["O'Brien"], ListFormat.SqlIn, QuoteChar.Double);
        Assert.Equal("('O''Brien')", result);
    }

    [Fact]
    public void Format_SqlIn_Empty()
    {
        Assert.Equal("()", TextListTool.Format([], ListFormat.SqlIn, QuoteChar.Double));
    }

    // ---------------------------------------------------------------- Format: CsvLine / CsvColumn

    [Fact]
    public void Format_CsvLine_QuotesFieldWithComma()
    {
        var result = TextListTool.Format(["a,b", "plain"], ListFormat.CsvLine, QuoteChar.Double);
        Assert.Equal("\"a,b\",plain", result);
    }

    [Fact]
    public void Format_CsvLine_QuotesFieldWithNewlineAndDoublesQuotes()
    {
        var result = TextListTool.Format(["line1\nline2", "has\"quote"], ListFormat.CsvLine, QuoteChar.Double);
        Assert.Equal("\"line1\nline2\",\"has\"\"quote\"", result);
    }

    [Fact]
    public void Format_CsvColumn_OnePerLine()
    {
        var result = TextListTool.Format(["a", "b,c"], ListFormat.CsvColumn, QuoteChar.Double);
        Assert.Equal("a\n\"b,c\"", result);
    }

    [Fact]
    public void Format_CsvLine_Empty_ReturnsEmptyString()
    {
        Assert.Equal("", TextListTool.Format([], ListFormat.CsvLine, QuoteChar.Double));
    }

    // ---------------------------------------------------------------- Format: PlainLines / SpaceJoined

    [Fact]
    public void Format_PlainLines_OneItemPerLine()
    {
        Assert.Equal("a\nb\nc", TextListTool.Format(["a", "b", "c"], ListFormat.PlainLines, QuoteChar.Double));
    }

    [Fact]
    public void Format_SpaceJoined_JoinsWithSingleSpace()
    {
        Assert.Equal("a b c", TextListTool.Format(["a", "b", "c"], ListFormat.SpaceJoined, QuoteChar.Double));
    }

    // ---------------------------------------------------------------- Empty input -> empty container per format

    [Theory]
    [InlineData(ListFormat.PythonList, "[]")]
    [InlineData(ListFormat.JsArray, "[]")]
    [InlineData(ListFormat.JsonArray, "[]")]
    [InlineData(ListFormat.CSharpArray, "new[] { }")]
    [InlineData(ListFormat.SqlIn, "()")]
    [InlineData(ListFormat.CsvLine, "")]
    [InlineData(ListFormat.CsvColumn, "")]
    [InlineData(ListFormat.PlainLines, "")]
    [InlineData(ListFormat.SpaceJoined, "")]
    public void Format_EmptyItems_ProducesEmptyContainer(ListFormat format, string expected)
    {
        Assert.Equal(expected, TextListTool.Format([], format, QuoteChar.Double));
    }
}
