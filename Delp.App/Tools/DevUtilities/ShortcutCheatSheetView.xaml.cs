using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("shortcut-cheatsheet", "Keyboard Shortcut Cheat Sheet", ToolCategory.DevUtilities,
    "Look up keyboard shortcuts for Vim, VS Code, Visual Studio, JetBrains IDEs, and the terminal.",
    Keywords = "shortcuts,keybindings,vim,vscode,visual studio,jetbrains,terminal,hotkeys,cheatsheet", Order = 145)]
public partial class ShortcutCheatSheetView : UserControl
{
    private readonly Dictionary<string, ListBox> _lists;
    private readonly Dictionary<string, TextBlock> _statusTexts;

    /// <summary>The result set currently bound to each tab's list, so a filter re-run that produces
    /// the same rows can skip touching ItemsSource (and therefore skip regenerating every row's
    /// containers).</summary>
    private readonly Dictionary<string, IReadOnlyList<ShortcutEntry>> _shown = new();

    private bool _ready;

    public ShortcutCheatSheetView()
    {
        InitializeComponent();

        _lists = new Dictionary<string, ListBox>
        {
            ["Vim"] = VimList,
            ["VS Code"] = VsCodeList,
            ["Visual Studio"] = VisualStudioList,
            ["JetBrains"] = JetBrainsList,
            ["Terminal"] = TerminalList,
        };
        _statusTexts = new Dictionary<string, TextBlock>
        {
            ["Vim"] = VimStatusText,
            ["VS Code"] = VsCodeStatusText,
            ["Visual Studio"] = VisualStudioStatusText,
            ["JetBrains"] = JetBrainsStatusText,
            ["Terminal"] = TerminalStatusText,
        };

        _ready = true;
        ApplyFilterToAll();
    }

    private void Filter_Changed(object sender, TextChangedEventArgs e)
    {
        if (_ready)
            ApplyFilterToAll();
    }

    private void Tab_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_ready)
            ApplyFilterToAll();
    }

    private void ApplyFilterToAll()
    {
        foreach (var editor in ShortcutCheatSheetData.Editors)
            ApplyFilter(editor);
    }

    private void ApplyFilter(string editor)
    {
        var results = ShortcutCheatSheetData.Search(editor, SearchBox.Text);

        if (!_shown.TryGetValue(editor, out var previous) || !results.SequenceEqual(previous))
        {
            _shown[editor] = results;
            _lists[editor].ItemsSource = results;
        }

        _statusTexts[editor].Text = $"{results.Count} of {ShortcutCheatSheetData.For(editor).Count} shown";
    }

    private void CopyKeys_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ShortcutEntry entry } button)
            Ui.Copy(entry.Keys, button);
    }
}
