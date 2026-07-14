using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class ShellCheatSheetDataTests
{
    [Fact]
    public void All_HasAtLeast70Entries()
    {
        Assert.True(ShellCheatSheetData.All.Count >= 70,
            $"Expected >= 70 entries, found {ShellCheatSheetData.All.Count}.");
    }

    [Fact]
    public void All_EveryEntryHasNonEmptyCategory()
    {
        Assert.All(ShellCheatSheetData.All, e => Assert.False(string.IsNullOrWhiteSpace(e.Category)));
    }

    [Fact]
    public void All_EveryEntryHasNonEmptyTask()
    {
        Assert.All(ShellCheatSheetData.All, e => Assert.False(string.IsNullOrWhiteSpace(e.Task)));
    }

    [Fact]
    public void All_EveryEntryHasNonEmptyBashAndPowerShell()
    {
        Assert.All(ShellCheatSheetData.All, e =>
        {
            Assert.False(string.IsNullOrWhiteSpace(e.Bash));
            Assert.False(string.IsNullOrWhiteSpace(e.PowerShell));
        });
    }

    [Fact]
    public void All_TasksAreUnique()
    {
        var tasks = ShellCheatSheetData.All.Select(e => e.Task).ToList();
        Assert.Equal(tasks.Count, tasks.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void All_CommandsHaveNoTabs()
    {
        foreach (var e in ShellCheatSheetData.All)
        {
            Assert.True(!e.Bash.Contains('\t'), $"Bash for '{e.Task}' contains a tab character.");
            Assert.True(!e.PowerShell.Contains('\t'), $"PowerShell for '{e.Task}' contains a tab character.");
            if (e.Notes is not null)
                Assert.True(!e.Notes.Contains('\t'), $"Notes for '{e.Task}' contains a tab character.");
        }
    }

    [Fact]
    public void All_CommandsHaveNoTrailingWhitespace()
    {
        foreach (var e in ShellCheatSheetData.All)
        {
            AssertNoTrailingWhitespace(e.Bash, e.Task, "Bash");
            AssertNoTrailingWhitespace(e.PowerShell, e.Task, "PowerShell");
            if (e.Notes is not null)
                AssertNoTrailingWhitespace(e.Notes, e.Task, "Notes");
        }
    }

    private static void AssertNoTrailingWhitespace(string text, string task, string field)
    {
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            Assert.True(line == line.TrimEnd(), $"{field} for '{task}' has a line with trailing whitespace.");
        }
    }

    [Fact]
    public void Categories_AreNonEmptyAndDistinct()
    {
        Assert.NotEmpty(ShellCheatSheetData.Categories);
        Assert.Equal(ShellCheatSheetData.Categories.Count, ShellCheatSheetData.Categories.Distinct().Count());
        Assert.All(ShellCheatSheetData.Categories, c => Assert.False(string.IsNullOrWhiteSpace(c)));
    }

    [Fact]
    public void Search_FiltersByCategory()
    {
        var category = ShellCheatSheetData.All[0].Category;
        var results = ShellCheatSheetData.Search(null, category);
        Assert.NotEmpty(results);
        Assert.All(results, e => Assert.Equal(category, e.Category, ignoreCase: true));
    }

    [Fact]
    public void Search_MatchesTaskText()
    {
        var sample = ShellCheatSheetData.All.First(e => e.Task.Contains("file", StringComparison.OrdinalIgnoreCase));
        var results = ShellCheatSheetData.Search("file");
        Assert.Contains(results, e => e.Task == sample.Task);
    }

    [Fact]
    public void Search_MatchesCommandText()
    {
        var results = ShellCheatSheetData.Search("git branch");
        Assert.NotEmpty(results);
    }

    [Fact]
    public void Search_NullOrWhitespaceQuery_ReturnsAll()
    {
        Assert.Equal(ShellCheatSheetData.All.Count, ShellCheatSheetData.Search(null).Count);
        Assert.Equal(ShellCheatSheetData.All.Count, ShellCheatSheetData.Search("   ").Count);
    }

    [Fact]
    public void Search_NoMatch_ReturnsEmpty()
    {
        Assert.Empty(ShellCheatSheetData.Search("zzzzzz_no_such_command_zzzzzz"));
    }

    [Fact]
    public void Search_CombinesQueryAndCategory()
    {
        var category = ShellCheatSheetData.All.First(e => e.Category == "Git").Category;
        var results = ShellCheatSheetData.Search("branch", category);
        Assert.NotEmpty(results);
        Assert.All(results, e => Assert.Equal("Git", e.Category));
    }
}
