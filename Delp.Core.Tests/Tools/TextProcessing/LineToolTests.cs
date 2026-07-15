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

    // ---- Q-U5: filter ----

    [Fact]
    public void Process_FilterKeep_Plain_KeepsMatchingSubstring()
    {
        var result = LineTool.Process("apple\nbanana\ngrape",
            new LineToolOptions(Filter: LineFilterMode.Keep, FilterPattern: "an"));
        Assert.Equal(["banana"], Lines(result));
    }

    [Fact]
    public void Process_FilterRemove_Plain_DropsMatchingSubstring()
    {
        var result = LineTool.Process("apple\nbanana\ngrape",
            new LineToolOptions(Filter: LineFilterMode.Remove, FilterPattern: "an"));
        Assert.Equal(["apple", "grape"], Lines(result));
    }

    [Fact]
    public void Process_FilterKeep_Plain_HonorsCaseInsensitiveOption()
    {
        var noCi = LineTool.Process("Apple\nbanana", new LineToolOptions(Filter: LineFilterMode.Keep, FilterPattern: "APPLE"));
        Assert.Equal([], Lines(noCi));

        var withCi = LineTool.Process("Apple\nbanana",
            new LineToolOptions(Filter: LineFilterMode.Keep, FilterPattern: "APPLE", CaseInsensitive: true));
        Assert.Equal(["Apple"], Lines(withCi));
    }

    [Fact]
    public void Process_FilterKeep_Regex_MatchesPattern()
    {
        var result = LineTool.Process("a1\nb22\nc333",
            new LineToolOptions(Filter: LineFilterMode.Keep, FilterPattern: @"\d{2,}", FilterRegex: true));
        Assert.Equal(["b22", "c333"], Lines(result));
    }

    [Fact]
    public void Process_FilterRemove_Regex_CaseInsensitive()
    {
        var result = LineTool.Process("ERROR: bad\ninfo: ok\nError: also bad",
            new LineToolOptions(Filter: LineFilterMode.Remove, FilterPattern: "^error", FilterRegex: true, CaseInsensitive: true));
        Assert.Equal(["info: ok"], Lines(result));
    }

    [Fact]
    public void Process_FilterRegex_InvalidPattern_ThrowsFormatException()
    {
        var ex = Assert.Throws<FormatException>(() => LineTool.Process("a\nb",
            new LineToolOptions(Filter: LineFilterMode.Keep, FilterPattern: "[unterminated", FilterRegex: true)));
        Assert.Contains("pattern", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Process_Filter_EmptyPattern_IsNoOp()
    {
        var result = LineTool.Process("a\nb\nc",
            new LineToolOptions(Filter: LineFilterMode.Keep, FilterPattern: ""));
        Assert.Equal(["a", "b", "c"], Lines(result));
        Assert.Null(result.FilteredKept);
        Assert.Null(result.FilteredTotal);
    }

    [Fact]
    public void Process_FilterOff_LeavesFilteredCountsNull()
    {
        var result = LineTool.Process("a\nb", new LineToolOptions(Filter: LineFilterMode.Off, FilterPattern: "a"));
        Assert.Equal(["a", "b"], Lines(result));
        Assert.Null(result.FilteredKept);
        Assert.Null(result.FilteredTotal);
    }

    [Fact]
    public void Process_Filter_StatusCounts_ReportKeptOfTotal()
    {
        // "ap" is a substring of "apple" and "grape" but not "banana" or "cherry".
        var result = LineTool.Process("apple\nbanana\ngrape\ncherry",
            new LineToolOptions(Filter: LineFilterMode.Keep, FilterPattern: "ap"));
        Assert.Equal(2, result.FilteredKept);
        Assert.Equal(4, result.FilteredTotal);
    }

    [Fact]
    public void Process_Filter_RunsBeforeDedupe_StatusReflectsPreDedupeTotal()
    {
        // "a" appears twice; filter should see both (total=3) before dedupe collapses them.
        var result = LineTool.Process("a\na\nb",
            new LineToolOptions(Filter: LineFilterMode.Keep, FilterPattern: "a", Dedupe: true));

        Assert.Equal(3, result.FilteredTotal); // all 3 lines were fed to the filter
        Assert.Equal(2, result.FilteredKept);  // both "a" lines passed the filter
        Assert.Equal(1, result.After);         // dedupe then collapsed them to one
        Assert.Equal(["a"], Lines(result));
    }

    // ---- Q-U5: numbering ----

    [Fact]
    public void Process_NumberLines_DefaultStartAndStep()
    {
        var result = LineTool.Process("a\nb\nc", new LineToolOptions(NumberLines: true));
        Assert.Equal(["1. a", "2. b", "3. c"], Lines(result));
    }

    [Fact]
    public void Process_NumberLines_CustomStartStepAndPad()
    {
        var result = LineTool.Process("a\nb\nc",
            new LineToolOptions(NumberLines: true, NumberStart: 10, NumberStep: 5, NumberPad: 3));
        Assert.Equal(["010. a", "015. b", "020. c"], Lines(result));
    }

    [Fact]
    public void Process_NumberLines_AppliesAfterSortAndReverse()
    {
        // Numbering is the very last step, so the numbers must follow the final (sorted+reversed)
        // order, not the original input order.
        var result = LineTool.Process("banana\napple\ncherry",
            new LineToolOptions(Mode: SortMode.Asc, Reverse: true, NumberLines: true));
        Assert.Equal(["1. cherry", "2. banana", "3. apple"], Lines(result));
    }

    // ---- Q-U5: full pipeline ----

    [Fact]
    public void Process_FullPipeline_TrimRemoveEmptyFilterDedupeSortReverseNumber()
    {
        var input = "  banana  \n\n  apple\napple  \n  cherry\n  fig  ";
        var result = LineTool.Process(input, new LineToolOptions(
            Mode: SortMode.Asc,
            TrimLines: true,
            RemoveEmpty: true,
            Filter: LineFilterMode.Remove,
            FilterPattern: "fig",
            Dedupe: true,
            Reverse: true,
            NumberLines: true,
            NumberStart: 1));

        // trim -> ["banana", "", "apple", "apple", "cherry", "fig"]
        // remove empty -> ["banana", "apple", "apple", "cherry", "fig"]
        // filter (remove "fig") -> ["banana", "apple", "apple", "cherry"]
        // dedupe -> ["banana", "apple", "cherry"]
        // sort asc -> ["apple", "banana", "cherry"]
        // reverse -> ["cherry", "banana", "apple"]
        // number -> ["1. cherry", "2. banana", "3. apple"]
        Assert.Equal(["1. cherry", "2. banana", "3. apple"], Lines(result));
    }
}
