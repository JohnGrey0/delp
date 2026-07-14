using Delp.Core.Tools.DataFormat;

namespace Delp.Core.Tests.Tools.DataFormat;

public class XmlFormatToolTests
{
    [Fact]
    public void Format_ProducesStableIndentedOutput()
    {
        const string xml = "<root a=\"1\"><a>1</a><a>2</a><b>hi</b></root>";
        var result = XmlFormatTool.Format(xml, new XmlFormatOptions(IndentSize: 2, OmitDeclaration: true));

        Assert.Equal(
            "<root a=\"1\">\n  <a>1</a>\n  <a>2</a>\n  <b>hi</b>\n</root>",
            result);
    }

    [Fact]
    public void Format_UsesTabsWhenRequested()
    {
        const string xml = "<root><a>1</a></root>";
        var result = XmlFormatTool.Format(xml, new XmlFormatOptions(UseTabs: true, OmitDeclaration: true));
        Assert.Equal("<root>\n\t<a>1</a>\n</root>", result);
    }

    [Fact]
    public void Format_IncludesDeclarationAsUtf8WhenNotOmitted()
    {
        const string xml = "<?xml version=\"1.0\"?><root/>";
        var result = XmlFormatTool.Format(xml, new XmlFormatOptions(OmitDeclaration: false));
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", result);
    }

    [Fact]
    public void Format_PreservesCData()
    {
        const string xml = "<root><data><![CDATA[<b>raw</b> & stuff]]></data></root>";
        var result = XmlFormatTool.Format(xml, new XmlFormatOptions(OmitDeclaration: true));
        Assert.Contains("<![CDATA[<b>raw</b> & stuff]]>", result);
    }

    [Fact]
    public void Format_PreservesComments()
    {
        const string xml = "<root><!-- keep me --><a>1</a></root>";
        var result = XmlFormatTool.Format(xml, new XmlFormatOptions(OmitDeclaration: true));
        Assert.Contains("<!-- keep me -->", result);
    }

    [Fact]
    public void Minify_ProducesSingleLineOutput()
    {
        const string xml = "<root>\n  <a>1</a>\n  <b>2</b>\n</root>";
        var result = XmlFormatTool.Minify(xml);
        Assert.Equal("<root><a>1</a><b>2</b></root>", result);
    }

    [Fact]
    public void Validate_ReturnsNullForWellFormedXml()
    {
        Assert.Null(XmlFormatTool.Validate("<root><a/></root>"));
    }

    [Fact]
    public void Validate_ReturnsLineAndColumnForMalformedXml()
    {
        var error = XmlFormatTool.Validate("<root><unclosed></root>");
        Assert.NotNull(error);
        Assert.Equal(1, error!.Line);
        Assert.True(error.Col > 0);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact]
    public void Format_RejectsDtdSafely()
    {
        const string xxe = "<?xml version=\"1.0\"?><!DOCTYPE foo [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]><foo>&xxe;</foo>";
        var ex = Assert.Throws<FormatException>(() => XmlFormatTool.Format(xxe, new XmlFormatOptions()));
        Assert.Contains("DTD", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsDtdSafely()
    {
        const string dtd = "<!DOCTYPE foo SYSTEM \"foo.dtd\"><foo/>";
        var error = XmlFormatTool.Validate(dtd);
        Assert.NotNull(error);
        Assert.Contains("DTD", error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_EmptyInput_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => XmlFormatTool.Format("", new XmlFormatOptions()));
    }

    [Fact]
    public void Format_PreservesUnicodeContent()
    {
        const string xml = "<root><name>Müller 日本語 🎉</name></root>";
        var result = XmlFormatTool.Format(xml, new XmlFormatOptions(OmitDeclaration: true));
        Assert.Contains("Müller 日本語 🎉", result);
    }
}
