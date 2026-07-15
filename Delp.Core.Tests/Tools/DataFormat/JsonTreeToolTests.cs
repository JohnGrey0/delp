using Delp.Core.Tools.DataFormat;

namespace Delp.Core.Tests.Tools.DataFormat;

public class JsonTreeToolTests
{
    private const string Doc = """
        {
          "name": "Ada",
          "age": 36,
          "active": true,
          "middleName": null,
          "weird key": 1,
          "tags": ["x", "y", "z"],
          "address": { "city": "London" }
        }
        """;

    [Fact]
    public void Load_RootObject_HasExpectedKindAndChildCount()
    {
        using var tree = JsonTreeTool.Load(Doc);
        Assert.Equal(JsonNodeKind.Object, tree.Root.Kind);
        Assert.Equal(7, tree.Root.ChildCount);
        Assert.Equal("$", tree.Root.Path);
        Assert.Equal("", tree.Root.Pointer);
        Assert.Null(tree.Root.Key);
    }

    [Fact]
    public void Preview_String_IsQuoted()
    {
        using var tree = JsonTreeTool.Load(Doc);
        var name = tree.Root.Children().Single(c => c.Key == "name");
        Assert.Equal("\"Ada\"", name.Preview);
    }

    [Fact]
    public void Preview_LongString_TruncatesAt80CharsWithEllipsis()
    {
        var longValue = new string('a', 200);
        using var tree = JsonTreeTool.Load($$"""{"s": "{{longValue}}"}""");
        var node = tree.Root.Children().Single();
        Assert.Equal("\"" + new string('a', 80) + "…\"", node.Preview);
    }

    [Fact]
    public void Preview_NumberBoolNull_AreVerbatim()
    {
        using var tree = JsonTreeTool.Load(Doc);
        var children = tree.Root.Children().ToDictionary(c => c.Key!);
        Assert.Equal("36", children["age"].Preview);
        Assert.Equal("true", children["active"].Preview);
        Assert.Equal("null", children["middleName"].Preview);
    }

    [Fact]
    public void Preview_ObjectAndArray_ShowCounts()
    {
        using var tree = JsonTreeTool.Load(Doc);
        var children = tree.Root.Children().ToDictionary(c => c.Key!);
        Assert.Equal("[3 items]", children["tags"].Preview);
        Assert.Equal("{1 props}", children["address"].Preview);
    }

    [Fact]
    public void Preview_PreservesNumberExactlyAsWritten()
    {
        using var tree = JsonTreeTool.Load("""{"n": 1.500e2}""");
        Assert.Equal("1.500e2", tree.Root.Children().Single().Preview);
    }

    [Fact]
    public void Path_UsesDotNotationForSimpleKeysAndBracketsForArrays()
    {
        using var tree = JsonTreeTool.Load(Doc);
        var tags = tree.Root.Children().Single(c => c.Key == "tags");
        var second = tags.Children()[1];
        Assert.Equal("$.tags[1]", second.Path);
    }

    [Fact]
    public void Path_BracketQuotesKeysThatArentSimpleIdentifiers()
    {
        using var tree = JsonTreeTool.Load(Doc);
        var weird = tree.Root.Children().Single(c => c.Key == "weird key");
        Assert.Equal("$['weird key']", weird.Path);
    }

    [Fact]
    public void Pointer_EscapesTildeAndSlash()
    {
        using var tree = JsonTreeTool.Load("""{"a~b/c": 1}""");
        var node = tree.Root.Children().Single();
        Assert.Equal("/a~0b~1c", node.Pointer);
    }

    [Fact]
    public void Pointer_ArrayIndex_IsPlainNumber()
    {
        using var tree = JsonTreeTool.Load("""{"a": [10, 20]}""");
        var arr = tree.Root.Children().Single();
        Assert.Equal("/a/1", arr.Children()[1].Pointer);
    }

    [Fact]
    public void Children_ArrayItems_HaveNullKey()
    {
        using var tree = JsonTreeTool.Load("""[1, 2]""");
        Assert.All(tree.Root.Children(), c => Assert.Null(c.Key));
    }

    [Fact]
    public void ChildCount_IsAvailableWithoutCallingChildren()
    {
        // ChildCount comes from GetPropertyCount()/GetArrayLength() alone — this test documents
        // that reading it never requires (and is unaffected by) a Children() call.
        using var tree = JsonTreeTool.Load(Doc);
        var address = tree.Root.Children().Single(c => c.Key == "address");
        Assert.Equal(1, address.ChildCount);
        Assert.Single(address.Children());
    }

    [Fact]
    public void Children_Scalar_ReturnsEmpty()
    {
        using var tree = JsonTreeTool.Load("42");
        Assert.Equal(0, tree.Root.ChildCount);
        Assert.Empty(tree.Root.Children());
    }

    [Fact]
    public void Load_MalformedJson_ThrowsFormatExceptionWithLocation()
    {
        var ex = Assert.Throws<FormatException>(() => JsonTreeTool.Load("{not json"));
        Assert.Contains("Line", ex.Message);
    }

    [Fact]
    public void Load_EmptyInput_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => JsonTreeTool.Load(""));
    }

    [Fact]
    public void Search_FindsMatchInKeyAndValueCaseInsensitively()
    {
        using var tree = JsonTreeTool.Load(Doc);
        var byKey = JsonTreeTool.Search(tree, "MIDDLENAME");
        Assert.Contains("$.middleName", byKey);

        var byValue = JsonTreeTool.Search(tree, "ada");
        Assert.Contains("$.name", byValue);
    }

    [Fact]
    public void Search_MatchesSubstringPastPreviewTruncation()
    {
        var longValue = new string('a', 90) + "NEEDLE";
        using var tree = JsonTreeTool.Load($$"""{"s": "{{longValue}}"}""");
        var results = JsonTreeTool.Search(tree, "needle");
        Assert.Contains("$.s", results);
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsEmpty()
    {
        using var tree = JsonTreeTool.Load(Doc);
        Assert.Empty(JsonTreeTool.Search(tree, ""));
    }

    [Fact]
    public void Search_CapsAtMax()
    {
        var items = string.Join(",", Enumerable.Repeat("\"needle\"", 10));
        using var tree = JsonTreeTool.Load($"[{items}]");
        var results = JsonTreeTool.Search(tree, "needle", max: 3);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void FindFirstMatchChain_ReturnsRootToMatchChain()
    {
        using var tree = JsonTreeTool.Load(Doc);
        var chain = JsonTreeTool.FindFirstMatchChain(tree, "London");
        Assert.NotNull(chain);
        Assert.Equal(tree.Root, chain![0]);
        Assert.Equal("$.address.city", chain[^1].Path);
    }

    [Fact]
    public void FindFirstMatchChain_NoMatch_ReturnsNull()
    {
        using var tree = JsonTreeTool.Load(Doc);
        Assert.Null(JsonTreeTool.FindFirstMatchChain(tree, "does-not-exist"));
    }

    [Fact]
    public void SearchAll_ReturnsSamePathsAsSearch_AndSameChainAsFindFirst()
    {
        using var tree = JsonTreeTool.Load(Doc);
        var combined = JsonTreeTool.SearchAll(tree, "a", max: 500);
        var paths = JsonTreeTool.Search(tree, "a", max: 500);
        var chain = JsonTreeTool.FindFirstMatchChain(tree, "a");

        Assert.Equal(paths, combined.Paths);
        Assert.NotNull(combined.FirstChain);
        Assert.Equal(chain!.Select(n => n.Path), combined.FirstChain!.Select(n => n.Path));
    }

    [Fact]
    public void SearchAll_NoMatch_HasEmptyPathsAndNullChain()
    {
        using var tree = JsonTreeTool.Load(Doc);
        var result = JsonTreeTool.SearchAll(tree, "nowhere", max: 500);
        Assert.Empty(result.Paths);
        Assert.Null(result.FirstChain);
    }

    [Fact]
    public void FindFirstMatchChain_DeepInLargeArray_ReturnsMinimalAncestorChainOnly()
    {
        // A 20k-element array whose last element carries the needle. The reveal chain the view walks
        // must be just root -> matching-element -> matching-leaf (3 nodes) — it must never need the
        // 20k siblings materialized to describe the path to the match.
        var sb = new System.Text.StringBuilder();
        sb.Append('[');
        for (var i = 0; i < 20000; i++)
        {
            if (i > 0) sb.Append(',');
            var name = i == 19999 ? "zzz-needle-zzz" : "item" + i;
            sb.Append("{\"name\":\"").Append(name).Append("\"}");
        }
        sb.Append(']');

        using var tree = JsonTreeTool.Load(sb.ToString());
        var chain = JsonTreeTool.FindFirstMatchChain(tree, "needle");

        Assert.NotNull(chain);
        Assert.Equal(3, chain!.Count);
        Assert.Equal("$", chain[0].Path);
        Assert.Equal("$[19999].name", chain[^1].Path);
    }

    [Fact]
    public void DeepNesting_1000Levels_DoesNotStackOverflowOnLoadOrSearch()
    {
        var json = new string('[', 1000) + "\"deep\"" + new string(']', 1000);
        using var tree = JsonTreeTool.Load(json);

        // Walk all the way down iteratively (mirrors how the lazy TreeView would drill in).
        var node = tree.Root;
        for (var i = 0; i < 1000; i++)
        {
            Assert.Equal(1, node.ChildCount);
            node = node.Children()[0];
        }
        Assert.Equal(JsonNodeKind.String, node.Kind);
        Assert.Equal("\"deep\"", node.Preview);

        var results = JsonTreeTool.Search(tree, "deep");
        Assert.Single(results);
    }
}
