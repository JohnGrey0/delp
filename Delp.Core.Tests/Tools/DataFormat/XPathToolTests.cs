using Delp.Core.Tools.DataFormat;

namespace Delp.Core.Tests.Tools.DataFormat;

public class XPathToolTests
{
    private const string Store = """
        <store>
          <book id="a1"><title>A</title><price>8</price></book>
          <book id="b2"><title>B</title><price>25</price></book>
        </store>
        """;

    [Fact]
    public void Evaluate_ElementSelection_ReturnsOuterXmlAndPath()
    {
        var result = XPathTool.Evaluate(Store, "//book");
        Assert.Equal(2, result.Count);
        Assert.False(result.Truncated);
        Assert.All(result.Matches, m => Assert.False(m.IsValue));
        Assert.Equal("/store[1]/book[1]", result.Matches[0].Path);
        Assert.Equal("/store[1]/book[2]", result.Matches[1].Path);
        Assert.Contains("id=\"a1\"", result.Matches[0].Snippet);
    }

    [Fact]
    public void Evaluate_AttributeSelection_ReturnsValueRow()
    {
        var result = XPathTool.Evaluate(Store, "//book/@id");
        Assert.Equal(2, result.Count);
        Assert.True(result.Matches[0].IsValue);
        Assert.Equal("a1", result.Matches[0].Snippet);
        Assert.Equal("/store[1]/book[1]/@id", result.Matches[0].Path);
        Assert.Equal("b2", result.Matches[1].Snippet);
    }

    [Fact]
    public void Evaluate_TextSelection_ReturnsValueRow()
    {
        var result = XPathTool.Evaluate(Store, "//title/text()");
        Assert.Equal(2, result.Count);
        Assert.True(result.Matches[0].IsValue);
        Assert.Equal("A", result.Matches[0].Snippet);
        Assert.Equal("B", result.Matches[1].Snippet);
    }

    [Fact]
    public void Evaluate_CountFunction_ReturnsSingleScalarValue()
    {
        var result = XPathTool.Evaluate(Store, "count(//book)");
        Assert.Equal(1, result.Count);
        Assert.False(result.Truncated);
        Assert.True(result.Matches[0].IsValue);
        Assert.Equal("2", result.Matches[0].Snippet);
    }

    [Fact]
    public void Evaluate_BooleanFunction_ReturnsTrueOrFalseValue()
    {
        var trueResult = XPathTool.Evaluate(Store, "boolean(//book)");
        Assert.Equal("true", trueResult.Matches[0].Snippet);

        var falseResult = XPathTool.Evaluate(Store, "boolean(//nope)");
        Assert.Equal("false", falseResult.Matches[0].Snippet);
    }

    [Fact]
    public void Evaluate_NoMatches_ReturnsEmptyResultWithoutThrowing()
    {
        var result = XPathTool.Evaluate(Store, "//nope");
        Assert.Equal(0, result.Count);
        Assert.Empty(result.Matches);
        Assert.False(result.Truncated);
    }

    [Fact]
    public void Evaluate_MaxCap_TruncatesAt1000AndReportsTruncated()
    {
        var items = string.Concat(Enumerable.Range(0, 1500).Select(i => $"<item>{i}</item>"));
        var xml = $"<root>{items}</root>";

        var result = XPathTool.Evaluate(xml, "//item");

        Assert.Equal(1000, result.Count);
        Assert.Equal(1000, result.Matches.Count);
        Assert.True(result.Truncated);
    }

    [Fact]
    public void Evaluate_DoctypeInput_IsRejectedEvenWithoutEntities()
    {
        const string xml = """
            <?xml version="1.0"?>
            <!DOCTYPE root [<!ELEMENT root ANY>]>
            <root>hi</root>
            """;

        var ex = Assert.Throws<FormatException>(() => XPathTool.Evaluate(xml, "//root"));
        Assert.Contains("XML", ex.Message);
    }

    [Fact]
    public void Evaluate_XxeEntityPayload_IsRejectedBeforeResolution()
    {
        // Classic XXE probe: if DTD/entity processing were allowed, this would attempt to read
        // a local file. DtdProcessing.Prohibit must reject the DOCTYPE outright, before the
        // ENTITY declaration (let alone the entity reference) is ever resolved.
        const string xml = """
            <?xml version="1.0"?>
            <!DOCTYPE root [<!ENTITY xxe SYSTEM "file:///C:/Windows/win.ini">]>
            <root>&xxe;</root>
            """;

        var ex = Assert.Throws<FormatException>(() => XPathTool.Evaluate(xml, "//root"));
        Assert.Contains("XML", ex.Message);
    }

    [Fact]
    public void Evaluate_InvalidExpression_ThrowsWithMessage()
    {
        var ex = Assert.Throws<FormatException>(() => XPathTool.Evaluate(Store, "//["));
        Assert.Contains("XPath", ex.Message);
    }

    [Fact]
    public void Evaluate_InvalidXml_ThrowsDistinguishableMessage()
    {
        var ex = Assert.Throws<FormatException>(() => XPathTool.Evaluate("<root><unclosed>", "//root"));
        Assert.Contains("XML", ex.Message);
    }

    [Fact]
    public void Evaluate_EmptyExpression_Throws()
    {
        Assert.Throws<FormatException>(() => XPathTool.Evaluate(Store, ""));
    }

    [Fact]
    public void Evaluate_EmptyXml_Throws()
    {
        Assert.Throws<FormatException>(() => XPathTool.Evaluate("", "//book"));
    }

    [Fact]
    public void Evaluate_NamespaceLocalNameRecipe_MatchesDespiteDefaultNamespace()
    {
        const string xml = """
            <root xmlns="urn:example:ns">
              <item>1</item>
              <item>2</item>
            </root>
            """;

        // A plain "//item" can't match because the elements are in a namespace and no prefix
        // is bound for it; local-name() sidesteps that entirely, per the tool's on-screen hint.
        var unqualified = XPathTool.Evaluate(xml, "//item");
        Assert.Equal(0, unqualified.Count);

        var byLocalName = XPathTool.Evaluate(xml, "//*[local-name()='item']");
        Assert.Equal(2, byLocalName.Count);
    }

    [Fact]
    public void Evaluate_ComputedPath_IndexesSameNameSiblings()
    {
        var result = XPathTool.Evaluate(Store, "//book[2]/title");
        Assert.Equal("/store[1]/book[2]/title[1]", result.Matches[0].Path);
    }
}
