namespace Delp.Core.Tools.DevUtilities;

/// <summary>One keyboard shortcut: an action, the category it's grouped under within its editor, the key combo, and an optional gotcha/tip.</summary>
public sealed record ShortcutEntry(string Action, string Category, string Keys, string? Notes = null);

/// <summary>
/// Static reference data for the Keyboard Shortcut Cheat Sheet tool. Entries are split across
/// partial-class files per editor (ShortcutCheatSheetData.Vim.cs, etc.) so no single file is enormous.
/// Keys are the Windows defaults throughout (Vim's are platform-agnostic).
/// </summary>
public static partial class ShortcutCheatSheetData
{
    // Lazy on purpose: static field initializers spread across partial-class files run in an
    // unspecified cross-file order, so eagerly evaluating this against the per-editor fields
    // (defined in the other ShortcutCheatSheetData.*.cs files) could observe them before they're
    // assigned. Deferring to first access guarantees every partial file's static initializers have
    // already run.
    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<ShortcutEntry>>> LazyByEditor = new(BuildByEditor);

    /// <summary>Editors in canonical (tab) order.</summary>
    public static IReadOnlyList<string> Editors { get; } = ["Vim", "VS Code", "Visual Studio", "JetBrains", "Terminal"];

    private static IReadOnlyDictionary<string, IReadOnlyList<ShortcutEntry>> BuildByEditor() =>
        new Dictionary<string, IReadOnlyList<ShortcutEntry>>
        {
            ["Vim"] = VimEntries,
            ["VS Code"] = VsCodeEntries,
            ["Visual Studio"] = VisualStudioEntries,
            ["JetBrains"] = JetBrainsEntries,
            ["Terminal"] = TerminalEntries,
        };

    /// <summary>All entries for one editor, or empty if the editor name isn't recognized.</summary>
    public static IReadOnlyList<ShortcutEntry> For(string editor) =>
        LazyByEditor.Value.TryGetValue(editor, out var entries) ? entries : [];

    /// <summary>Case-insensitive filter over one editor's entries, matching action, keys, or category.</summary>
    public static IReadOnlyList<ShortcutEntry> Search(string editor, string? query)
    {
        var entries = For(editor);
        if (string.IsNullOrWhiteSpace(query))
            return entries;

        var q = query.Trim();
        return entries.Where(e =>
            e.Action.Contains(q, StringComparison.OrdinalIgnoreCase)
            || e.Keys.Contains(q, StringComparison.OrdinalIgnoreCase)
            || e.Category.Contains(q, StringComparison.OrdinalIgnoreCase)
            || (e.Notes is not null && e.Notes.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}
