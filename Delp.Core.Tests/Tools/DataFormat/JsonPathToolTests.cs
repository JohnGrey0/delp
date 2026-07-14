using Delp.Core.Tools.DataFormat;

namespace Delp.Core.Tests.Tools.DataFormat;

public class JsonPathToolTests
{
    private const string Store = """
        {
          "store": {
            "book": [
              { "title": "A", "price": 8 },
              { "title": "B", "price": 25 }
            ]
          }
        }
        """;

    [Fact]
    public void Query_Root_ReturnsWholeDocument()
    {
        var result = JsonPathTool.Query("""{"a":1}""", "$");
        Assert.Equal(1, result.Count);
        Assert.Contains("\"a\": 1", result.ResultJson);
    }

    [Fact]
    public void Query_RecursiveDescent_FindsAllPrices()
    {
        var result = JsonPathTool.Query(Store, "$..price");
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Query_ArrayIndex_ReturnsSingleElement()
    {
        var result = JsonPathTool.Query(Store, "$.store.book[0].title");
        Assert.Equal(1, result.Count);
        Assert.Contains("\"A\"", result.ResultJson);
    }

    [Fact]
    public void Query_ArraySlice_ReturnsRange()
    {
        var result = JsonPathTool.Query("""{"a":[0,1,2,3,4]}""", "$.a[1:3]");
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Query_FilterExpression_ReturnsMatchingElements()
    {
        var result = JsonPathTool.Query(Store, "$.store.book[?(@.price > 10)].title");
        Assert.Equal(1, result.Count);
        Assert.Contains("\"B\"", result.ResultJson);
    }

    [Fact]
    public void Query_NoMatches_ReturnsEmptyArrayAndZeroCount()
    {
        var result = JsonPathTool.Query(Store, "$.nope");
        Assert.Equal(0, result.Count);
        Assert.Equal("[]", result.ResultJson);
    }

    [Fact]
    public void Query_InvalidPath_ThrowsWithMessage()
    {
        var ex = Assert.Throws<FormatException>(() => JsonPathTool.Query("""{"a":1}""", "$.["));
        Assert.Contains("JSONPath", ex.Message);
    }

    [Fact]
    public void Query_InvalidJson_ThrowsDistinguishableMessage()
    {
        var ex = Assert.Throws<FormatException>(() => JsonPathTool.Query("{not json", "$"));
        Assert.Contains("JSON", ex.Message);
    }
}
