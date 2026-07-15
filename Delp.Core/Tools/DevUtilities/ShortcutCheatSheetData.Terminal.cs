namespace Delp.Core.Tools.DevUtilities;

public static partial class ShortcutCheatSheetData
{
    private static readonly IReadOnlyList<ShortcutEntry> TerminalEntries =
    [
        // Readline / Bash (also zsh, and PowerShell in its default "emacs" edit mode)
        new("Move to beginning of line", "Readline / Bash", "Ctrl+A"),
        new("Move to end of line", "Readline / Bash", "Ctrl+E"),
        new("Delete from cursor to beginning of line", "Readline / Bash", "Ctrl+U"),
        new("Delete from cursor to end of line", "Readline / Bash", "Ctrl+K"),
        new("Delete word before cursor", "Readline / Bash", "Ctrl+W"),
        new("Delete word after cursor", "Readline / Bash", "Alt+D"),
        new("Paste last killed text (yank)", "Readline / Bash", "Ctrl+Y"),
        new("Reverse search command history", "Readline / Bash", "Ctrl+R"),
        new("Cancel search / abort current line", "Readline / Bash", "Ctrl+G"),
        new("Clear screen", "Readline / Bash", "Ctrl+L"),
        new("Interrupt current command (SIGINT)", "Readline / Bash", "Ctrl+C"),
        new("End of input / exit shell", "Readline / Bash", "Ctrl+D", "Only exits when the line is empty; otherwise it deletes the character under the cursor."),
        new("Suspend current process (SIGTSTP)", "Readline / Bash", "Ctrl+Z", "Resume it in the foreground with fg."),
        new("Move back / forward one word", "Readline / Bash", "Alt+B / Alt+F"),
        new("Transpose characters", "Readline / Bash", "Ctrl+T"),
        new("Undo last edit", "Readline / Bash", "Ctrl+/"),
        new("Repeat last command", "Readline / Bash", "!!"),
        new("Last argument of previous command", "Readline / Bash", "!$", "Alt+. also cycles through previous arguments."),
        new("Auto-complete command/path", "Readline / Bash", "Tab"),
        new("Edit current command in $EDITOR", "Readline / Bash", "Ctrl+X Ctrl+E"),

        // Windows Terminal
        new("New tab", "Windows Terminal", "Ctrl+Shift+T"),
        new("Close tab", "Windows Terminal", "Ctrl+Shift+W"),
        new("Next / previous tab", "Windows Terminal", "Ctrl+Tab / Ctrl+Shift+Tab"),
        new("Switch to tab N", "Windows Terminal", "Ctrl+Alt+{N}"),
        new("Split pane, auto direction", "Windows Terminal", "Alt+Shift+D"),
        new("Split pane horizontally", "Windows Terminal", "Alt+Shift+-"),
        new("Split pane vertically", "Windows Terminal", "Alt+Shift+="),
        new("Move focus between panes", "Windows Terminal", "Alt+Arrow"),
        new("Duplicate tab", "Windows Terminal", "Ctrl+Shift+D"),
        new("Open settings", "Windows Terminal", "Ctrl+,"),
        new("Search terminal output", "Windows Terminal", "Ctrl+Shift+F"),
        new("Copy / paste", "Windows Terminal", "Ctrl+Shift+C / Ctrl+Shift+V", "Plain Ctrl+C/Ctrl+V also work when 'legacy' copy-paste keys are enabled."),

        // tmux (default prefix Ctrl+b)
        new("Create new window", "tmux", "Ctrl+b c"),
        new("Rename current window", "tmux", "Ctrl+b ,"),
        new("Next / previous window", "tmux", "Ctrl+b n / Ctrl+b p"),
        new("Switch to window number N", "tmux", "Ctrl+b {N}"),
        new("Split pane vertically (side by side)", "tmux", "Ctrl+b %"),
        new("Split pane horizontally (stacked)", "tmux", "Ctrl+b \""),
        new("Move between panes", "tmux", "Ctrl+b Arrow"),
        new("Zoom pane (toggle fullscreen)", "tmux", "Ctrl+b z"),
        new("Detach session", "tmux", "Ctrl+b d"),
        new("Enter copy/scroll mode", "tmux", "Ctrl+b ["),
        new("Kill current pane", "tmux", "Ctrl+b x"),
        new("List sessions", "tmux", "Ctrl+b s"),
    ];
}
