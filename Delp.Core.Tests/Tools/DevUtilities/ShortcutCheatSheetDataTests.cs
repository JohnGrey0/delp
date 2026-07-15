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

    [Fact]
    public void Search_ChangeInnerWord_FindsVimCiw()
    {
        // ciw (change inner word) is one of the most common Vim text-object idioms; it must appear
        // alongside its delete counterpart (diw) and not just be implied by "cw" (which behaves
        // differently: cw stops at the end of the current word from the cursor, ciw always affects
        // the whole word regardless of cursor position within it).
        var results = ShortcutCheatSheetData.Search("Vim", "ciw");
        Assert.Contains(results, e => e.Keys == "ci{obj}");
    }

    [Fact]
    public void VsCode_ClearTerminal_DoesNotClaimAWindowsDefaultBinding()
    {
        // Regression: this used to list "Ctrl+K" as the Windows default for
        // workbench.action.terminal.clear. That's actually the macOS-only binding (VS Code registers
        // it with `primary: 0` — i.e. no default — on Windows/Linux); shipping it as a Windows default
        // is misinformation for the exact audience this tool serves.
        var entry = ShortcutCheatSheetData.For("VS Code").Single(e => e.Action == "Clear terminal");
        Assert.NotEqual("Ctrl+K", entry.Keys);
        Assert.Contains("macOS", entry.Notes ?? "", StringComparison.Ordinal);
    }
}
