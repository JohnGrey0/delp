using Delp.Core.Tools.DataFormat;

namespace Delp.Core.Tests.Tools.DataFormat;

public class JsonFormatToolTests
{
    [Fact]
    public void Format_NestedSample_ProducesStableExpectedString()
    {
        const string input = """{"b":2,"a":{"x":1,"y":[1,2,3]},"c":null}""";
        const string expected = """
            {
              "b": 2,
              "a": {
                "x": 1,
                "y": [
                  1,
                  2,
                  3
                ]
              },
              "c": null
            }
            """;
        Assert.Equal(Normalize(expected), Normalize(JsonFormatTool.Format(input)));
    }

    [Fact]
    public void Format_EmptyObjectAndArray_StayOnOneLine()
    {
        Assert.Equal("{}", JsonFormatTool.Format("{}"));
        Assert.Equal("[]", JsonFormatTool.Format("[]"));
    }

    [Fact]
    public void Format_UsesTabsWhenRequested()
    {
        var result = JsonFormatTool.Format("""{"a":1}""", new JsonFormatTool.JsonFormatOptions(UseTabs: true));
        Assert.Contains("\t\"a\": 1", result);
    }

    [Fact]
    public void Format_FourSpaceIndent()
    {
        var result = JsonFormatTool.Format("""{"a":1}""", new JsonFormatTool.JsonFormatOptions(IndentSize: 4));
        Assert.Contains("    \"a\": 1", result);
    }

    [Fact]
    public void Minify_RoundTripsThroughFormat()
    {
        const string input = """{"b": 2, "a": [1, 2, 3]}""";
        var minified = JsonFormatTool.Minify(input);
        Assert.Equal("""{"b":2,"a":[1,2,3]}""", minified);
        Assert.Equal(JsonFormatTool.Format(input), JsonFormatTool.Format(minified));
    }

    [Fact]
    public void Format_SortKeys_RecursesIntoNestedObjects()
    {
        const string input = """{"z":1,"a":{"y":1,"b":2}}""";
        var result = JsonFormatTool.Format(input, new JsonFormatTool.JsonFormatOptions(SortKeys: true));
        var aIndex = result.IndexOf("\"a\"", StringComparison.Ordinal);
        var zIndex = result.IndexOf("\"z\"", StringComparison.Ordinal);
        Assert.True(aIndex < zIndex);
        var bIndex = result.IndexOf("\"b\"", StringComparison.Ordinal);
        var yIndex = result.IndexOf("\"y\"", StringComparison.Ordinal);
        Assert.True(bIndex < yIndex);
    }

    [Fact]
    public void Format_EscapeNonAscii_EscapesEmojiAndAccents()
    {
        var result = JsonFormatTool.Format("""{"s":"café 🚀"}""", new JsonFormatTool.JsonFormatOptions(EscapeNonAscii: true));
        Assert.DoesNotContain("é", result);
        Assert.DoesNotContain("🚀", result);
        Assert.Contains("\\u00e9", result);
        // Astral character encodes as a UTF-16 surrogate pair.
        Assert.Contains("\\ud83d\\ude80", result);
    }

    [Fact]
    public void Format_PreservesNumberRawText()
    {
        var result = JsonFormatTool.Format("""{"a":1.50,"b":1e10,"c":123456789012345678901234567890}""");
        Assert.Contains("1.50", result);
        Assert.Contains("1e10", result);
        Assert.Contains("123456789012345678901234567890", result);
    }

    [Fact]
    public void Validate_ValidInput_ReturnsNull()
    {
        Assert.Null(JsonFormatTool.Validate("""{"a":1}"""));
    }

    [Fact]
    public void Validate_MissingValue_ReturnsLineAndCol()
    {
        var error = JsonFormatTool.Validate("""{"a":}""");
        Assert.NotNull(error);
        Assert.Equal(1, error!.Line);
        Assert.Equal(6, error.Col);
    }

    [Fact]
    public void Validate_DuplicateKey_ReturnsError()
    {
        var error = JsonFormatTool.Validate("""{"a":1,"a":2}""");
        Assert.NotNull(error);
        Assert.Contains("a", error!.Message);
    }

    [Fact]
    public void Format_DuplicateKey_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => JsonFormatTool.Format("""{"a":1,"a":2}"""));
    }

    [Fact]
    public void Format_MalformedJson_ThrowsFormatExceptionWithLineCol()
    {
        var ex = Assert.Throws<FormatException>(() => JsonFormatTool.Format("""{"a":}"""));
        Assert.Contains("Line 1, Col 6", ex.Message);
    }

    [Fact]
    public void Format_TrailingComma_IsRejected()
    {
        Assert.Throws<FormatException>(() => JsonFormatTool.Format("""{"a":1,}"""));
    }

    [Fact]
    public void Format_Comments_AreRejected()
    {
        Assert.Throws<FormatException>(() => JsonFormatTool.Format("{\"a\":1 /* c */}"));
    }

    private static string Normalize(string s) => s.Replace("\r\n", "\n").Trim();
}
