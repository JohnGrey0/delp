namespace Delp.Core.Tools.DevUtilities;

public static partial class ShortcutCheatSheetData
{
    // The "Default for Windows" keymap shared by JetBrains IDEs (IntelliJ IDEA, Rider, PyCharm, WebStorm, ...).
    private static readonly IReadOnlyList<ShortcutEntry> JetBrainsEntries =
    [
        // Navigation
        new("Go to class", "Navigation", "Ctrl+N"),
        new("Go to file", "Navigation", "Ctrl+Shift+N"),
        new("Go to symbol", "Navigation", "Ctrl+Alt+Shift+N"),
        new("Go to declaration", "Navigation", "Ctrl+B", "Or Ctrl+Click."),
        new("Go to implementation", "Navigation", "Ctrl+Alt+B"),
        new("Find usages", "Navigation", "Alt+F7"),
        new("Find usages in file", "Navigation", "Ctrl+F7"),
        new("Highlight usages in file", "Navigation", "Ctrl+Shift+F7"),
        new("Go to super-method/class", "Navigation", "Ctrl+U"),
        new("Next highlighted error", "Navigation", "F2"),
        new("Previous highlighted error", "Navigation", "Shift+F2"),
        new("Go to line:column", "Navigation", "Ctrl+G"),
        new("Recent files", "Navigation", "Ctrl+E"),
        new("Recent locations", "Navigation", "Ctrl+Shift+E"),
        new("Navigate back / forward", "Navigation", "Ctrl+Alt+Left / Ctrl+Alt+Right"),
        new("Go to matching brace", "Navigation", "Ctrl+["),

        // Editing
        new("Basic code completion", "Editing", "Ctrl+Space"),
        new("Smart type-matching completion", "Editing", "Ctrl+Shift+Space"),
        new("Complete current statement", "Editing", "Ctrl+Shift+Enter"),
        new("Parameter info", "Editing", "Ctrl+P"),
        new("Quick documentation", "Editing", "Ctrl+Q"),
        new("Generate code (getters, constructors, ...)", "Editing", "Alt+Insert"),
        new("Reformat code", "Editing", "Ctrl+Alt+L"),
        new("Optimize imports", "Editing", "Ctrl+Alt+O"),
        new("Comment/uncomment line", "Editing", "Ctrl+/"),
        new("Comment/uncomment block", "Editing", "Ctrl+Shift+/"),
        new("Extend selection", "Editing", "Ctrl+W"),
        new("Shrink selection", "Editing", "Ctrl+Shift+W"),
        new("Move line up / down", "Editing", "Ctrl+Shift+Up / Ctrl+Shift+Down"),
        new("Duplicate line", "Editing", "Ctrl+D"),
        new("Delete line", "Editing", "Ctrl+Y"),
        new("Join lines", "Editing", "Ctrl+Shift+J"),
        new("Undo / Redo", "Editing", "Ctrl+Z / Ctrl+Shift+Z"),

        // Refactoring
        new("Rename", "Refactoring", "Shift+F6"),
        new("Extract method", "Refactoring", "Ctrl+Alt+M"),
        new("Extract variable", "Refactoring", "Ctrl+Alt+V"),
        new("Extract field", "Refactoring", "Ctrl+Alt+F"),
        new("Extract constant", "Refactoring", "Ctrl+Alt+C"),
        new("Extract parameter", "Refactoring", "Ctrl+Alt+P"),
        new("Move", "Refactoring", "F6"),
        new("Change signature", "Refactoring", "Ctrl+F6"),
        new("Safe delete", "Refactoring", "Alt+Delete"),

        // Search
        new("Find", "Search", "Ctrl+F"),
        new("Replace", "Search", "Ctrl+R"),
        new("Find in path (project-wide)", "Search", "Ctrl+Shift+F"),
        new("Replace in path (project-wide)", "Search", "Ctrl+Shift+R"),
        new("Find next / previous", "Search", "F3 / Shift+F3"),
        new("Search everywhere", "Search", "Double Shift"),
        new("Find action", "Search", "Ctrl+Shift+A"),

        // Debug
        new("Debug", "Debug", "Shift+F9"),
        new("Run", "Debug", "Shift+F10"),
        new("Step over", "Debug", "F8"),
        new("Step into", "Debug", "F7"),
        new("Step out", "Debug", "Shift+F8"),
        new("Run to cursor", "Debug", "Alt+F9"),
        new("Toggle breakpoint", "Debug", "Ctrl+F8"),
        new("View breakpoints", "Debug", "Ctrl+Shift+F8"),
        new("Evaluate expression", "Debug", "Alt+F8"),

        // Version Control
        new("Commit", "Version Control", "Ctrl+K"),
        new("Push", "Version Control", "Ctrl+Shift+K"),
        new("Update project (pull)", "Version Control", "Ctrl+T"),
        new("VCS operations popup", "Version Control", "Alt+`"),
        new("Rollback changes", "Version Control", "Ctrl+Alt+Z"),
        new("Open Version Control tool window", "Version Control", "Alt+9"),
    ];
}
