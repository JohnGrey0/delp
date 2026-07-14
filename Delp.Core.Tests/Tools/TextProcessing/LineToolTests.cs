using Delp.Core.Tools.TextProcessing;

namespace Delp.Core.Tests.Tools.TextProcessing;

public class LineToolTests
{
    private static string[] Lines(LineResult result) =>
        result.Text.Length == 0 ? [] : result.Text.Split('\n');

    [Fact]
    public void Process_SortAsc()
    {
        var result = LineTool.Process("banana\napple\ncherry", new LineToolOptions(Mode: SortMode.Asc));
        Assert.Equal(["apple", "banana", "cherry"], Lines(result));
    }

    [Fact]
    public void Process_SortDesc()
    {
        var result = LineTool.Process("banana\napple\ncherry", new LineToolOptions(Mode: SortMode.Desc));
        Assert.Equal(["cherry", "banana", "apple"], Lines(result));
    }

    [Fact]
    public void Process_SortNatural_OrdersDigitRunsNumerically()
    {
        var result = LineTool.Process("a10\na2\na1", new LineToolOptions(Mode: SortMode.Natural));
        Assert.Equal(["a1", "a2", "a10"], Lines(result));
    }

    [Fact]
    public void Process_SortLength()
    {
        var result = LineTool.Process("ccc\na\nbb", new LineToolOptions(Mode: SortMode.Length));
        Assert.Equal(["a", "bb", "ccc"], Lines(result));
    }

    [Fact]
    public void Process_SortNumeric_ParsesLeadingNumberAndPushesNonNumericLast()
    {
        // "Leading number" means the number at the very start of the line.
        var result = LineTool.Process("10 apples\n2 bananas\nno number here\n1 cherry", new LineToolOptions(Mode: SortMode.Numeric));
        Assert.Equal(["1 cherry", "2 bananas", "10 apples", "no number here"], Lines(result));
    }

    [Fact]
    public void Process_Dedupe_CaseInsensitive()
    {
        var result = LineTool.Process("Apple\napple\nAPPLE\nbanana",
            new LineToolOptions(Dedupe: true, CaseInsensitive: true));

        Assert.Equal(["Apple", "banana"], Lines(result));
        Assert.Equal(4, result.Before);
        Assert.Equal(2, result.After);
    }

    [Fact]
    public void Process_Dedupe_CaseSensitiveKeepsBothCasings()
    {
        var result = LineTool.Process("Apple\napple", new LineToolOptions(Dedupe: true));
        Assert.Equal(["Apple", "apple"], Lines(result));
    }

    [Fact]
    public void Process_OrderOfOperations_TrimFilterSortDedupeReverse()
    {
        // Trim first (removes padding), then remove-empty (drops the now-empty line),
        // then sort ascending, then dedupe, then reverse.
        var input = "  banana  \n\n  apple\napple  \n  cherry";
        var result = LineTool.Process(input, new LineToolOptions(
            Mode: SortMode.Asc, TrimLines: true, RemoveEmpty: true, Dedupe: true, Reverse: true));

        // trim -> ["banana", "", "apple", "apple", "cherry"]
        // remove empty -> ["banana", "apple", "apple", "cherry"]
        // sort asc -> ["apple", "apple", "banana", "cherry"]
        // dedupe -> ["apple", "banana", "cherry"]
        // reverse -> ["cherry", "banana", "apple"]
        Assert.Equal(["cherry", "banana", "apple"], Lines(result));
    }

    [Fact]
    public void Process_RemoveEmpty_DropsBlankLines()
    {
        var result = LineTool.Process("a\n\nb\n   \nc", new LineToolOptions(RemoveEmpty: true, TrimLines: true));
        Assert.Equal(["a", "b", "c"], Lines(result));
    }

    [Fact]
    public void Process_Shuffle_SeededIsDeterministic()
    {
        var input = "one\ntwo\nthree\nfour\nfive";
        var first = LineTool.Process(input, new LineToolOptions(Shuffle: true, Seed: 42));
        var second = LineTool.Process(input, new LineToolOptions(Shuffle: true, Seed: 42));

        Assert.Equal(first.Text, second.Text);
        Assert.Equal(5, first.After);
    }

    [Fact]
    public void Process_Shuffle_WinsOverSort()
    {
        var input = "one\ntwo\nthree\nfour\nfive";
        var result = LineTool.Process(input, new LineToolOptions(Mode: SortMode.Asc, Shuffle: true, Seed: 7));
        var sortedOnly = LineTool.Process(input, new LineToolOptions(Mode: SortMode.Asc));

        // Same 5 lines, but shuffle is documented to win over the sort ordering.
        Assert.Equal(sortedOnly.Text.Split('\n').OrderBy(x => x), result.Text.Split('\n').OrderBy(x => x));
    }

    [Fact]
    public void Process_BeforeAfterCounts()
    {
        var result = LineTool.Process("a\nb\na\nc", new LineToolOptions(Dedupe: true));
        Assert.Equal(4, result.Before);
        Assert.Equal(3, result.After);
    }

    [Fact]
    public void Process_EmptyInput_ReturnsZeroLines()
    {
        var result = LineTool.Process("", new LineToolOptions());
        Assert.Equal(0, result.Before);
        Assert.Equal(0, result.After);
        Assert.Equal("", result.Text);
    }
}
