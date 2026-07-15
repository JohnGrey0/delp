using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class ShortcutCheatSheetDataTests
{
    private static readonly Dictionary<string, (int Min, int Max)> ExpectedRange = new()
    {
        ["Vim"] = (90, 150),
        ["VS Code"] = (60, 100),
        ["Visual Studio"] = (40, 70),
        ["JetBrains"] = (40, 70),
        ["Terminal"] = (25, 50),
    };

    [Fact]
    public void Editors_ListsAllFiveEditors()
    {
        Assert.Equal(["Vim", "VS Code", "Visual Studio", "JetBrains", "Terminal"], ShortcutCheatSheetData.Editors);
    }

    [Theory]
    [InlineData("Vim")]
    [InlineData("VS Code")]
    [InlineData("Visual Studio")]
    [InlineData("JetBrains")]
    [InlineData("Terminal")]
    public void For_EachEditor_IsWithinExpectedSize(string editor)
    {
        var entries = ShortcutCheatSheetData.For(editor);
        var (min, max) = ExpectedRange[editor];
        Assert.InRange(entries.Count, min, max);
    }

    [Theory]
    [InlineData("Vim")]
    [InlineData("VS Code")]
    [InlineData("Visual Studio")]
    [InlineData("JetBrains")]
    [InlineData("Terminal")]
    public void For_EachEditor_NoEntryHasEmptyKeysOrAction(string editor)
    {
        foreach (var entry in ShortcutCheatSheetData.For(editor))
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Keys), $"Empty Keys for action '{entry.Action}'.");
            Assert.False(string.IsNullOrWhiteSpace(entry.Action));
            Assert.False(string.IsNullOrWhiteSpace(entry.Category));
        }
    }

    [Fact]
    public void AllEntries_HaveUniqueEditorActionPairs()
    {
        var seen = new HashSet<(string Editor, string Action)>();
        foreach (var editor in ShortcutCheatSheetData.Editors)
        {
            foreach (var entry in ShortcutCheatSheetData.For(editor))
            {
                Assert.True(seen.Add((editor, entry.Action)), $"Duplicate action '{entry.Action}' in editor '{editor}'.");
            }
        }
    }

    [Fact]
    public void For_UnknownEditor_ReturnsEmpty()
    {
        Assert.Empty(ShortcutCheatSheetData.For("Sublime Text"));
    }

    [Fact]
    public void Search_DeleteWord_FindsVimDw()
    {
        var results = ShortcutCheatSheetData.Search("Vim", "delete word");
        Assert.Contains(results, e => e.Keys == "dw");
    }

    [Fact]
    public void Search_Rename_FindsVsCodeF2()
    {
        var results = ShortcutCheatSheetData.Search("VS Code", "rename");
        Assert.Contains(results, e => e.Keys == "F2");
    }

    [Fact]
    public void Search_MatchesOnKeysToo()
    {
        var results = ShortcutCheatSheetData.Search("Vim", "Ctrl+r");
        Assert.Contains(results, e => e.Action == "Redo");
    }

    [Fact]
    public void Search_NullOrWhitespaceQuery_ReturnsAllEntries()
    {
        Assert.Equal(ShortcutCheatSheetData.For("Terminal").Count, ShortcutCheatSheetData.Search("Terminal", null).Count);
        Assert.Equal(ShortcutCheatSheetData.For("Terminal").Count, ShortcutCheatSheetData.Search("Terminal", "   ").Count);
    }

    [Fact]
    public void Search_NoMatches_ReturnsEmpty()
    {
        Assert.Empty(ShortcutCheatSheetData.Search("Vim", "xyzzy-not-a-real-shortcut"));
    }
}
