using Delp.Core.Tools.DataFormat;

namespace Delp.Core.Tests.Tools.DataFormat;

public class XmlJsonToolTests
{
    [Fact]
    public void XmlToJson_MapsAttributesAndRepeatedSiblingsAndMixedText()
    {
        const string xml = "<root id=\"5\"><a>1</a><a>2</a><b x=\"1\">hi</b></root>";
        var json = XmlJsonTool.XmlToJson(xml);

        Assert.Equal(
            "{\n  \"@id\": \"5\",\n  \"a\": [\n    \"1\",\n    \"2\"\n  ],\n  \"b\": {\n    \"@x\": \"1\",\n    \"#text\": \"hi\"\n  }\n}",
            json);
    }

    [Fact]
    public void XmlToJson_LeafElementWithoutAttrsOrChildren_BecomesDirectString()
    {
        const string xml = "<note><to>Tove</to><from>Jani</from></note>";
        var json = XmlJsonTool.XmlToJson(xml);
        Assert.Equal("{\n  \"to\": \"Tove\",\n  \"from\": \"Jani\"\n}", json);
    }

    [Fact]
    public void XmlToJson_MixedContent_UsesTextKey()
    {
        const string xml = "<p>Hello <b>world</b>!</p>";
        var json = XmlJsonTool.XmlToJson(xml);
        Assert.Equal("{\n  \"b\": \"world\",\n  \"#text\": \"Hello !\"\n}", json);
    }

    [Fact]
    public void JsonToXml_ScalarAtRoot_BecomesElementText()
    {
        var xml = XmlJsonTool.JsonToXml("\"hello\"", "root");
        Assert.Contains("<root>hello</root>", xml);
    }

    [Fact]
    public void JsonToXml_ArrayValue_BecomesRepeatedElements()
    {
        var xml = XmlJsonTool.JsonToXml("{\"tags\":[\"a\",\"b\"]}", "root");
        Assert.Contains("<tags>a</tags>", xml);
        Assert.Contains("<tags>b</tags>", xml);
    }

    [Fact]
    public void JsonToXml_InvalidKey_FallsBackToItemWithNameAttribute()
    {
        var xml = XmlJsonTool.JsonToXml("{\"3d-model\": \"x\"}", "root");
        Assert.Contains("<item name=\"3d-model\">x</item>", xml);
    }

    [Fact]
    public void JsonToXml_UsesGivenRootName()
    {
        var xml = XmlJsonTool.JsonToXml("{\"a\":1}", "envelope");
        Assert.Contains("<envelope>", xml);
        Assert.Contains("<a>1</a>", xml);
    }

    [Fact]
    public void RoundTrip_XmlToJsonToXmlToJson_IsStable()
    {
        const string xml = "<root id=\"5\"><a>1</a><a>2</a><b x=\"1\">hi</b></root>";
        var json = XmlJsonTool.XmlToJson(xml);
        var xmlBack = XmlJsonTool.JsonToXml(json, "root");
        var jsonAgain = XmlJsonTool.XmlToJson(xmlBack);
        Assert.Equal(json, jsonAgain);
    }

    [Fact]
    public void XmlToJson_NoRootElement_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => XmlJsonTool.XmlToJson(""));
    }

    [Fact]
    public void JsonToXml_InvalidJson_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => XmlJsonTool.JsonToXml("{not json", "root"));
    }
}
