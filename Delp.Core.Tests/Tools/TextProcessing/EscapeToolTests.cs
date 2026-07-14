using Delp.Core.Tools.TextProcessing;

namespace Delp.Core.Tests.Tools.TextProcessing;

public class EscapeToolTests
{
    [Theory]
    [InlineData(EscapeTarget.Json)]
    [InlineData(EscapeTarget.XmlHtml)]
    [InlineData(EscapeTarget.CSharp)]
    [InlineData(EscapeTarget.JavaScript)]
    [InlineData(EscapeTarget.Sql)]
    [InlineData(EscapeTarget.Regex)]
    [InlineData(EscapeTarget.Url)]
    public void RoundTrip_HoldsForEveryTarget(EscapeTarget target)
    {
        const string text = "héllo \"world\" 'quote' <tag> & 世界 🚀\nline2\ttab";
        var escaped = EscapeTool.Escape(target, text);
        Assert.Equal(text, EscapeTool.Unescape(target, escaped));
    }

    [Fact]
    public void Json_EscapesControlChars()
    {
        var withControlChar = "a" + (char)1 + "b";
        var escaped = EscapeTool.Escape(EscapeTarget.Json, withControlChar);
        Assert.Contains("\\u0001", escaped);
        Assert.Equal(withControlChar, EscapeTool.Unescape(EscapeTarget.Json, escaped));
    }

    [Fact]
    public void Json_Unescape_InvalidInput_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => EscapeTool.Unescape(EscapeTarget.Json, "unterminated \\"));
    }

    [Fact]
    public void XmlHtml_EscapesFiveEntities()
    {
        Assert.Equal("&amp;&lt;&gt;&quot;&apos;", EscapeTool.Escape(EscapeTarget.XmlHtml, "&<>\"'"));
    }

    [Fact]
    public void XmlHtml_Unescape_DoesNotDoubleUnescape()
    {
        // "&amp;lt;" must decode to "&lt;", not "<".
        Assert.Equal("&lt;", EscapeTool.Unescape(EscapeTarget.XmlHtml, "&amp;lt;"));
    }

    [Fact]
    public void Csv_FieldWithQuoteCommaNewline_IsQuotedAndEscaped()
    {
        const string field = "he said \"hi\", ok\nnext";
        var escaped = EscapeTool.Escape(EscapeTarget.Csv, field);
        Assert.Equal("\"he said \"\"hi\"\", ok\nnext\"", escaped);
        Assert.Equal(field, EscapeTool.Unescape(EscapeTarget.Csv, escaped));
    }

    [Fact]
    public void Csv_PlainField_IsUnchanged()
    {
        Assert.Equal("plain", EscapeTool.Escape(EscapeTarget.Csv, "plain"));
    }

    [Fact]
    public void Sql_OBrien_DoublesSingleQuote()
    {
        Assert.Equal("O''Brien", EscapeTool.Escape(EscapeTarget.Sql, "O'Brien"));
        Assert.Equal("O'Brien", EscapeTool.Unescape(EscapeTarget.Sql, "O''Brien"));
    }

    [Fact]
    public void CSharp_EscapesBackslashAndQuote()
    {
        var escaped = EscapeTool.Escape(EscapeTarget.CSharp, "path\\to\\\"file\"");
        Assert.Equal("path\\\\to\\\\\\\"file\\\"", escaped);
        Assert.Equal("path\\to\\\"file\"", EscapeTool.Unescape(EscapeTarget.CSharp, escaped));
    }

    [Fact]
    public void JavaScript_EscapesSingleQuote()
    {
        var escaped = EscapeTool.Escape(EscapeTarget.JavaScript, "it's here");
        Assert.Equal("it\\'s here", escaped);
        Assert.Equal("it's here", EscapeTool.Unescape(EscapeTarget.JavaScript, escaped));
    }

    [Fact]
    public void Unescape_UnicodeEscape_Decodes()
    {
        Assert.Equal("😀", EscapeTool.Unescape(EscapeTarget.CSharp, "\\U0001F600"));
    }

    [Fact]
    public void Unescape_UnknownSequence_Throws()
    {
        Assert.Throws<FormatException>(() => EscapeTool.Unescape(EscapeTarget.CSharp, "\\q"));
    }

    [Fact]
    public void Regex_Escape_UsesFrameworkImplementation()
    {
        Assert.Equal(System.Text.RegularExpressions.Regex.Escape("a.b*c"), EscapeTool.Escape(EscapeTarget.Regex, "a.b*c"));
    }

    [Fact]
    public void Url_Escape_EncodesReservedChars()
    {
        Assert.Equal("a%20b%2Fc", EscapeTool.Escape(EscapeTarget.Url, "a b/c"));
        Assert.Equal("a b/c", EscapeTool.Unescape(EscapeTarget.Url, "a%20b%2Fc"));
    }

    [Fact]
    public void Escape_EmptyInput_ReturnsEmpty()
    {
        foreach (var target in Enum.GetValues<EscapeTarget>())
            Assert.Equal("", EscapeTool.Escape(target, ""));
    }
}
