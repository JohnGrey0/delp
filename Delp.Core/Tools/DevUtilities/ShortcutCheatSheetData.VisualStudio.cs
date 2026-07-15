namespace Delp.Core.Tools.DevUtilities;

public static partial class ShortcutCheatSheetData
{
    // Visual Studio's default "General" (Windows) keyboard scheme.
    private static readonly IReadOnlyList<ShortcutEntry> VisualStudioEntries =
    [
        // General
        new("New project", "General", "Ctrl+Shift+N"),
        new("Open project", "General", "Ctrl+Shift+O"),
        new("Save", "General", "Ctrl+S"),
        new("Save all", "General", "Ctrl+Shift+S"),
        new("Add new item", "General", "Ctrl+Shift+A"),
        new("Add existing item", "General", "Shift+Alt+A"),
        new("Quick Launch (search settings/commands)", "General", "Ctrl+Q"),
        new("Close active tab", "General", "Ctrl+F4"),

        // Editing
        new("Comment selection", "Editing", "Ctrl+K, Ctrl+C"),
        new("Uncomment selection", "Editing", "Ctrl+K, Ctrl+U"),
        new("Indent / outdent selection", "Editing", "Tab / Shift+Tab"),
        new("Toggle outlining expansion", "Editing", "Ctrl+M, Ctrl+M"),
        new("Toggle all outlining", "Editing", "Ctrl+M, Ctrl+L"),
        new("Format document", "Editing", "Ctrl+K, Ctrl+D"),
        new("Format selection", "Editing", "Ctrl+K, Ctrl+F"),
        new("Move selected lines up / down", "Editing", "Alt+Up / Alt+Down"),
        new("Rename (Refactor Rename)", "Editing", "Ctrl+R, Ctrl+R"),
        new("Quick Actions and Refactorings", "Editing", "Ctrl+."),
        new("Insert snippet", "Editing", "Ctrl+K, Ctrl+X"),
        new("Surround with snippet", "Editing", "Ctrl+K, Ctrl+S"),
        new("Complete word / trigger IntelliSense", "Editing", "Ctrl+Space"),
        new("List members", "Editing", "Ctrl+J"),
        new("Parameter info", "Editing", "Ctrl+Shift+Space"),
        new("Quick Info", "Editing", "Ctrl+K, Ctrl+I"),
        new("Undo / Redo", "Editing", "Ctrl+Z / Ctrl+Y"),

        // Navigation
        new("Go To (Navigate To)", "Navigation", "Ctrl+,"),
        new("Navigate backward / forward", "Navigation", "Ctrl+- / Ctrl+Shift+-"),
        new("Go to definition", "Navigation", "F12"),
        new("Go to implementation", "Navigation", "Ctrl+F12"),
        new("Find all references", "Navigation", "Shift+F12"),
        new("Peek definition", "Navigation", "Alt+F12"),
        new("Find", "Navigation", "Ctrl+F"),
        new("Replace", "Navigation", "Ctrl+H"),
        new("Find in files", "Navigation", "Ctrl+Shift+F"),
        new("Replace in files", "Navigation", "Ctrl+Shift+H"),
        new("Next / previous error or build message", "Navigation", "F8 / Shift+F8"),
        new("Go to line", "Navigation", "Ctrl+G"),

        // Debugging
        new("Start debugging", "Debugging", "F5"),
        new("Start without debugging", "Debugging", "Ctrl+F5"),
        new("Stop debugging", "Debugging", "Shift+F5"),
        new("Restart debugging", "Debugging", "Ctrl+Shift+F5"),
        new("Toggle breakpoint", "Debugging", "F9"),
        new("Step over", "Debugging", "F10"),
        new("Step into", "Debugging", "F11"),
        new("Step out", "Debugging", "Shift+F11"),
        new("QuickWatch", "Debugging", "Shift+F9"),
        new("Breakpoints window", "Debugging", "Ctrl+Alt+B"),
        new("Immediate window", "Debugging", "Ctrl+Alt+I"),

        // Build
        new("Build solution", "Build", "Ctrl+Shift+B"),
        new("Cancel build", "Build", "Ctrl+Break"),
        new("View code", "Build", "F7"),
        new("View designer", "Build", "Shift+F7"),

        // Window
        new("Solution Explorer", "Window", "Ctrl+Alt+L"),
        new("Output window", "Window", "Ctrl+Alt+O"),
        new("Error List", "Window", "Ctrl+\\, Ctrl+E"),
        new("Task List", "Window", "Ctrl+\\, Ctrl+T"),
        new("Navigate open documents/tool windows", "Window", "Ctrl+Tab"),
    ];
}
