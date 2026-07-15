namespace Delp.Core.Tools.DevUtilities;

public static partial class ShortcutCheatSheetData
{
    // Windows default keybindings for Visual Studio Code.
    private static readonly IReadOnlyList<ShortcutEntry> VsCodeEntries =
    [
        // General
        new("Show Command Palette", "General", "Ctrl+Shift+P"),
        new("Quick Open, go to file", "General", "Ctrl+P"),
        new("Open Settings", "General", "Ctrl+,"),
        new("Open Keyboard Shortcuts", "General", "Ctrl+K Ctrl+S"),
        new("New window", "General", "Ctrl+Shift+N"),
        new("Close window", "General", "Ctrl+Shift+W"),
        new("Close editor", "General", "Ctrl+W"),
        new("Reopen closed editor", "General", "Ctrl+Shift+T"),
        new("New file", "General", "Ctrl+N"),
        new("Open file", "General", "Ctrl+O"),
        new("Save", "General", "Ctrl+S"),
        new("Save all", "General", "Ctrl+K S"),
        new("Save as", "General", "Ctrl+Shift+S"),

        // Editing
        new("Cut line (empty selection)", "Editing", "Ctrl+X"),
        new("Copy line (empty selection)", "Editing", "Ctrl+C"),
        new("Move line up / down", "Editing", "Alt+Up / Alt+Down"),
        new("Copy line up / down", "Editing", "Shift+Alt+Up / Shift+Alt+Down"),
        new("Delete line", "Editing", "Ctrl+Shift+K"),
        new("Insert line below", "Editing", "Ctrl+Enter"),
        new("Insert line above", "Editing", "Ctrl+Shift+Enter"),
        new("Jump to matching bracket", "Editing", "Ctrl+Shift+\\"),
        new("Indent / outdent line", "Editing", "Ctrl+] / Ctrl+["),
        new("Go to beginning / end of line", "Editing", "Home / End"),
        new("Go to beginning / end of file", "Editing", "Ctrl+Home / Ctrl+End"),
        new("Toggle line comment", "Editing", "Ctrl+/"),
        new("Toggle block comment", "Editing", "Shift+Alt+A"),
        new("Format document", "Editing", "Shift+Alt+F"),
        new("Format selection", "Editing", "Ctrl+K Ctrl+F"),
        new("Undo / Redo", "Editing", "Ctrl+Z / Ctrl+Y"),
        new("Trigger suggestion (IntelliSense)", "Editing", "Ctrl+Space"),
        new("Rename symbol", "Editing", "F2"),

        // Navigation
        new("Go to line", "Navigation", "Ctrl+G"),
        new("Go to symbol in workspace", "Navigation", "Ctrl+T"),
        new("Go to symbol in file", "Navigation", "Ctrl+Shift+O"),
        new("Show Problems panel", "Navigation", "Ctrl+Shift+M"),
        new("Go to definition", "Navigation", "F12"),
        new("Peek definition", "Navigation", "Alt+F12"),
        new("Show references", "Navigation", "Shift+F12"),
        new("Open definition to the side", "Navigation", "Ctrl+K F12"),
        new("Go back / forward", "Navigation", "Alt+Left / Alt+Right"),
        new("Navigate editor group history", "Navigation", "Ctrl+Tab"),
        new("Split editor", "Navigation", "Ctrl+\\"),
        new("Focus editor group 1 / 2 / 3", "Navigation", "Ctrl+1 / Ctrl+2 / Ctrl+3"),

        // Multi-Cursor & Selection
        new("Insert cursor", "Multi-Cursor & Selection", "Alt+Click"),
        new("Insert cursor above / below", "Multi-Cursor & Selection", "Ctrl+Alt+Up / Ctrl+Alt+Down"),
        new("Add selection to next match", "Multi-Cursor & Selection", "Ctrl+D"),
        new("Select all occurrences of current selection", "Multi-Cursor & Selection", "Ctrl+Shift+L"),
        new("Undo last cursor operation", "Multi-Cursor & Selection", "Ctrl+U"),
        new("Insert cursor at end of each selected line", "Multi-Cursor & Selection", "Shift+Alt+I"),
        new("Select current line", "Multi-Cursor & Selection", "Ctrl+L"),
        new("Expand / shrink selection", "Multi-Cursor & Selection", "Shift+Alt+Right / Shift+Alt+Left"),
        new("Select all", "Multi-Cursor & Selection", "Ctrl+A"),
        new("Column (box) selection", "Multi-Cursor & Selection", "Shift+Alt+Drag"),

        // Search & Replace
        new("Find", "Search & Replace", "Ctrl+F"),
        new("Replace", "Search & Replace", "Ctrl+H"),
        new("Find in files", "Search & Replace", "Ctrl+Shift+F"),
        new("Replace in files", "Search & Replace", "Ctrl+Shift+H"),
        new("Find next / previous", "Search & Replace", "F3 / Shift+F3"),
        new("Select all occurrences of Find match", "Search & Replace", "Alt+Enter"),
        new("Toggle search details (include/exclude)", "Search & Replace", "Ctrl+Shift+J"),
        new("Toggle case-sensitive / regex / whole word in Find", "Search & Replace", "Alt+C / Alt+R / Alt+W"),

        // Display
        new("Toggle sidebar visibility", "Display", "Ctrl+B"),
        new("Toggle panel visibility", "Display", "Ctrl+J"),
        new("Show Explorer", "Display", "Ctrl+Shift+E"),
        new("Show Source Control", "Display", "Ctrl+Shift+G"),
        new("Show Run and Debug", "Display", "Ctrl+Shift+D"),
        new("Show Extensions", "Display", "Ctrl+Shift+X"),
        new("Toggle Zen Mode", "Display", "Ctrl+K Z"),
        new("Zoom in / out", "Display", "Ctrl+= / Ctrl+-"),
        new("Toggle full screen", "Display", "F11"),

        // Debug
        new("Start / Continue debugging", "Debug", "F5"),
        new("Stop debugging", "Debug", "Shift+F5"),
        new("Restart debugging", "Debug", "Ctrl+Shift+F5"),
        new("Toggle breakpoint", "Debug", "F9"),
        new("Step over", "Debug", "F10"),
        new("Step into", "Debug", "F11"),
        new("Step out", "Debug", "Shift+F11"),
        new("Show hover / Quick Info", "Debug", "Ctrl+K Ctrl+I"),

        // Integrated Terminal
        new("Toggle integrated terminal", "Integrated Terminal", "Ctrl+`"),
        new("Create new terminal", "Integrated Terminal", "Ctrl+Shift+`"),
        new("Split terminal", "Integrated Terminal", "Ctrl+Shift+5"),
        new("Focus next / previous terminal", "Integrated Terminal", "Alt+Right / Alt+Left", "Only while the terminal panel has focus."),
        new("Scroll terminal up / down a line", "Integrated Terminal", "Ctrl+Up / Ctrl+Down"),
        new("Clear terminal", "Integrated Terminal", "Ctrl+K", "Only while the terminal panel has focus."),
    ];
}
