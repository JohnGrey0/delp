using System.Text.RegularExpressions;
using Delp.Core.Tools.TextProcessing;

namespace Delp.Core.Tests.Tools.TextProcessing;

public class RegexLibraryTests
{
    [Fact]
    public void All_HasAtLeastTwentyTwoEntries()
    {
        Assert.True(RegexLibrary.All.Count >= 22, $"Expected >= 22 entries, found {RegexLibrary.All.Count}.");
    }

    [Fact]
    public void All_NamesAreUnique()
    {
        var names = RegexLibrary.All.Select(e => e.Name).ToList();
        Assert.Equal(names.Distinct(StringComparer.Ordinal).Count(), names.Count);
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public void Entry_PatternCompilesWithTimeout(RegexEntry entry)
    {
        var regex = new Regex(entry.Pattern, RegexOptions.None, TimeSpan.FromSeconds(2));
        Assert.NotNull(regex);
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public void Entry_PatternMatchesItsOwnExample(RegexEntry entry)
    {
        var regex = new Regex(entry.Pattern, RegexOptions.None, TimeSpan.FromSeconds(2));
        Assert.True(regex.IsMatch(entry.Example),
            $"'{entry.Name}' pattern {entry.Pattern} did not match its own example '{entry.Example}'.");
    }

    public static IEnumerable<object[]> Entries() => RegexLibrary.All.Select(e => new object[] { e });

    [Fact]
    public void Search_NullOrEmpty_ReturnsAll()
    {
        Assert.Equal(RegexLibrary.All.Count, RegexLibrary.Search(null).Count);
        Assert.Equal(RegexLibrary.All.Count, RegexLibrary.Search("").Count);
        Assert.Equal(RegexLibrary.All.Count, RegexLibrary.Search("   ").Count);
    }

    [Fact]
    public void Search_FiltersByNameCaseInsensitive()
    {
        var results = RegexLibrary.Search("email");
        Assert.Contains(results, e => e.Name == "Email");
        Assert.DoesNotContain(results, e => e.Name == "UUID");
    }

    [Fact]
    public void Search_FiltersByDescription()
    {
        var results = RegexLibrary.Search("IPv4");
        Assert.Contains(results, e => e.Name == "IPv4 Address");
    }

    [Fact]
    public void Search_NoMatches_ReturnsEmpty()
    {
        Assert.Empty(RegexLibrary.Search("zzz_no_such_pattern_zzz"));
    }
}
