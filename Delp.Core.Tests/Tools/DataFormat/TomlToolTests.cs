using Delp.Core.Tools.DataFormat;

namespace Delp.Core.Tests.Tools.DataFormat;

public class TomlToolTests
{
    [Fact]
    public void TomlToJson_Table_BecomesNestedObject()
    {
        const string toml = """
            title = "example"

            [owner]
            name = "Tom"
            """;
        var json = TomlTool.TomlToJson(toml);
        Assert.Contains("\"title\": \"example\"", json);
        Assert.Contains("\"owner\": {", json);
        Assert.Contains("\"name\": \"Tom\"", json);
    }

    [Fact]
    public void TomlToJson_ArrayOfTables_BecomesJsonArray()
    {
        const string toml = """
            [[fruit]]
            name = "apple"

            [[fruit]]
            name = "banana"
            """;
        var json = TomlTool.TomlToJson(toml);
        Assert.Contains("\"fruit\": [", json);
        Assert.Contains("\"apple\"", json);
        Assert.Contains("\"banana\"", json);
    }

    [Fact]
    public void TomlToJson_InlineTable_BecomesObject()
    {
        var json = TomlTool.TomlToJson("point = { x = 1, y = 2 }");
        Assert.Contains("\"point\": {", json);
        Assert.Contains("\"x\": 1", json);
    }

    [Fact]
    public void TomlToJson_DateTimeVariants_FormatAsIsoStrings()
    {
        const string toml = """
            odt = 1979-05-27T07:32:00Z
            ld = 1979-05-27
            lt = 07:32:00
            """;
        var json = TomlTool.TomlToJson(toml);
        Assert.Contains("1979-05-27", json);
        Assert.Contains("07:32:00", json);
    }

    [Fact]
    public void JsonToToml_ObjectRoot_ProducesTable()
    {
        var toml = TomlTool.JsonToToml("""{"title":"hi","count":5}""");
        Assert.Contains("title = \"hi\"", toml);
        Assert.Contains("count = 5", toml);
    }

    [Fact]
    public void JsonToToml_ArrayOfObjects_ProducesArrayOfTables()
    {
        var toml = TomlTool.JsonToToml("""{"fruit":[{"name":"apple"},{"name":"banana"}]}""");
        Assert.Contains("[[fruit]]", toml);
        Assert.Contains("name = \"apple\"", toml);
    }

    [Fact]
    public void RoundTrip_JsonToTomlToJson_PreservesShape()
    {
        const string json = """{"title":"hi","count":5,"ratio":1.5,"flag":true,"nested":{"x":1}}""";
        var toml = TomlTool.JsonToToml(json);
        var backToJson = TomlTool.TomlToJson(toml);
        Assert.Equal(JsonFormatTool.Format(json), backToJson);
    }

    [Fact]
    public void JsonToToml_NonObjectRoot_Throws()
    {
        var ex = Assert.Throws<FormatException>(() => TomlTool.JsonToToml("[1,2,3]"));
        Assert.Contains("object", ex.Message);
    }

    [Fact]
    public void JsonToToml_NullValue_ThrowsTomlHasNoNull()
    {
        var ex = Assert.Throws<FormatException>(() => TomlTool.JsonToToml("""{"a":null}"""));
        Assert.Contains("null", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ValidToml_ReturnsNull()
    {
        Assert.Null(TomlTool.Validate("a = 1"));
    }

    [Fact]
    public void Validate_MalformedToml_ReturnsLineColInfo()
    {
        var error = TomlTool.Validate("a = [1, 2\nb = 3");
        Assert.NotNull(error);
        Assert.True(error!.Line >= 1);
        Assert.True(error.Col >= 1);
    }

    [Fact]
    public void TomlToJson_MalformedToml_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => TomlTool.TomlToJson("a = [1, 2\nb = 3"));
    }
}
