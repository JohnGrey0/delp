using System.Diagnostics;
using Delp.Core.Tools.TextProcessing;

namespace Delp.Core.Tests.Tools.TextProcessing;

public class RegexToolTests
{
    private static readonly RegexToolOptions Default = new();

    [Fact]
    public void Run_NamedAndNumberedGroups()
    {
        var result = RegexTool.Run(@"(?<year>\d{4})-(?<month>\d{2})-(\d{2})", "2024-01-15", Default);

        Assert.Null(result.Error);
        var match = Assert.Single(result.Matches);
        Assert.Equal("2024-01-15", match.Value);
        Assert.Equal(0, match.Index);

        var year = match.Groups.Single(g => g.Name == "year");
        Assert.Equal("2024", year.Value);
        Assert.True(year.Success);

        var month = match.Groups.Single(g => g.Name == "month");
        Assert.Equal("01", month.Value);

        // .NET numbers unnamed groups first (left to right), then named groups after,
        // so the trailing unnamed "(\d{2})" day group is exposed as group "1", not "3".
        var day = match.Groups.Single(g => g.Name == "1");
        Assert.Equal("15", day.Value);
    }

    [Fact]
    public void Run_IgnoreCase_ControlsMatching()
    {
        var caseSensitive = RegexTool.Run("ABC", "abc", Default);
        Assert.Empty(caseSensitive.Matches);

        var ignoreCase = RegexTool.Run("ABC", "abc", Default with { IgnoreCase = true });
        Assert.Single(ignoreCase.Matches);
    }

    [Fact]
    public void Run_Multiline_AnchorsPerLine()
    {
        var withoutMultiline = RegexTool.Run("^b", "a\nb", Default);
        Assert.Empty(withoutMultiline.Matches);

        var withMultiline = RegexTool.Run("^b", "a\nb", Default with { Multiline = true });
        var match = Assert.Single(withMultiline.Matches);
        Assert.Equal(2, match.Index);
    }

    [Fact]
    public void Run_Singleline_DotMatchesNewline()
    {
        var withoutSingleline = RegexTool.Run("a.b", "a\nb", Default);
        Assert.Empty(withoutSingleline.Matches);

        var withSingleline = RegexTool.Run("a.b", "a\nb", Default with { Singleline = true });
        Assert.Single(withSingleline.Matches);
    }

    [Fact]
    public void Run_IgnoreWhitespace_StripsPatternWhitespace()
    {
        var withoutFlag = RegexTool.Run("a b c", "abc", Default);
        Assert.Empty(withoutFlag.Matches);

        var withFlag = RegexTool.Run("a b c", "abc", Default with { IgnoreWhitespace = true });
        Assert.Single(withFlag.Matches);
    }

    [Fact]
    public void Run_InvalidPattern_ReturnsErrorNotException()
    {
        var result = RegexTool.Run("(unterminated", "anything", Default);

        Assert.Empty(result.Matches);
        Assert.NotNull(result.Error);
        Assert.Contains("Invalid pattern", result.Error);
    }

    [Fact(Timeout = 10_000)]
    public void Run_CatastrophicBacktracking_TimesOutInsteadOfHanging()
    {
        var input = new string('a', 32) + "!";
        var sw = Stopwatch.StartNew();

        var result = RegexTool.Run("(a+)+$", input, Default);

        sw.Stop();
        Assert.NotNull(result.Error);
        Assert.Contains("timed out", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Matches);
        // Proves the timeout actually fired rather than the pattern happening to finish fast or hang.
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(9), $"Expected the 2s match timeout to fire, took {sw.Elapsed}.");
    }

    [Fact]
    public void Replace_SupportsGroupReferences()
    {
        var result = RegexTool.Replace(@"(\w+)@(\w+)", "user@host", "$2@$1", Default);

        Assert.Null(result.Error);
        Assert.Equal("host@user", result.Result);
    }

    [Fact]
    public void Replace_InvalidPattern_ReturnsErrorNotException()
    {
        var result = RegexTool.Replace("[unterminated", "text", "x", Default);

        Assert.Null(result.Result);
        Assert.NotNull(result.Error);
    }

    [Fact(Timeout = 10_000)]
    public void Replace_CatastrophicBacktracking_TimesOutInsteadOfHanging()
    {
        var input = new string('a', 32) + "!";

        var result = RegexTool.Replace("(a+)+$", input, "x", Default);

        Assert.Null(result.Result);
        Assert.NotNull(result.Error);
        Assert.Contains("timed out", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_NoMatches_ReturnsEmptyListNoError()
    {
        var result = RegexTool.Run(@"\d+", "no digits here", Default);

        Assert.Empty(result.Matches);
        Assert.Null(result.Error);
    }
}
