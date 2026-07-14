using Delp.Core.Tools.DataFormat;

namespace Delp.Core.Tests.Tools.DataFormat;

public class JsonYamlToolTests
{
    [Fact]
    public void JsonToYaml_Object_ProducesBlockStyle()
    {
        var yaml = JsonYamlTool.JsonToYaml("""{"name":"Tom","age":42}""");
        Assert.Contains("name: Tom", yaml);
        Assert.Contains("age: 42", yaml);
    }

    [Fact]
    public void JsonToYaml_Array_ProducesDashItems()
    {
        var yaml = JsonYamlTool.JsonToYaml("""{"items":[1,2,3]}""");
        Assert.Contains("- 1", yaml);
        Assert.Contains("- 2", yaml);
        Assert.Contains("- 3", yaml);
    }

    [Fact]
    public void JsonToYaml_StringThatLooksNumeric_StaysQuoted()
    {
        var yaml = JsonYamlTool.JsonToYaml("""{"code":"42"}""");
        Assert.Contains("code: \"42\"", yaml);
    }

    [Fact]
    public void RoundTrip_ObjectArrayScalar_PreservesShape()
    {
        const string json = """{"name":"Tom","age":42,"active":true,"score":3.5,"tags":["a","b"],"meta":null}""";
        var yaml = JsonYamlTool.JsonToYaml(json);
        var backToJson = JsonYamlTool.YamlToJson(yaml);
        var reformattedOriginal = JsonFormatTool.Format(json);
        Assert.Equal(reformattedOriginal, backToJson);
    }

    [Fact]
    public void YamlToJson_TypePreservation_NumberVsString()
    {
        var json = JsonYamlTool.YamlToJson("a: 42\nb: \"42\"\n");
        Assert.Contains("\"a\": 42", json);
        Assert.Contains("\"b\": \"42\"", json);
    }

    [Fact]
    public void YamlToJson_NestedStructures()
    {
        const string yaml = """
            person:
              name: Tom
              tags:
                - a
                - b
            """;
        var json = JsonYamlTool.YamlToJson(yaml);
        Assert.Contains("\"name\": \"Tom\"", json);
        Assert.Contains("\"a\"", json);
        Assert.Contains("\"b\"", json);
    }

    [Fact]
    public void YamlToJson_AnchorAlias_ResolvesToSameValue()
    {
        const string yaml = """
            base: &b hello
            other: *b
            """;
        var json = JsonYamlTool.YamlToJson(yaml);
        Assert.Contains("\"base\": \"hello\"", json);
        Assert.Contains("\"other\": \"hello\"", json);
    }

    [Fact]
    public void YamlToJson_MultipleDocuments_Throws()
    {
        var ex = Assert.Throws<FormatException>(() => JsonYamlTool.YamlToJson("a: 1\n---\nb: 2\n"));
        Assert.Contains("Multiple", ex.Message);
    }

    [Fact]
    public void YamlToJson_InvalidYaml_Throws()
    {
        Assert.Throws<FormatException>(() => JsonYamlTool.YamlToJson("a: [1, 2\nb: 3"));
    }

    [Fact]
    public void JsonToYaml_InvalidJson_Throws()
    {
        Assert.Throws<FormatException>(() => JsonYamlTool.JsonToYaml("{not json"));
    }
}
