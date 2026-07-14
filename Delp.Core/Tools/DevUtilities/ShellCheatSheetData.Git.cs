namespace Delp.Core.Tools.DevUtilities;

public static partial class ShellCheatSheetData
{
    internal static IReadOnlyList<ShellEntry> GitEntries { get; } = new List<ShellEntry>
    {
        new("Undo the last commit but keep changes staged", "Git",
            "git reset --soft HEAD~1",
            "git reset --soft HEAD~1",
            "The git command line is identical in both shells; only surrounding piping/quoting differs."),

        new("Amend the last commit's message", "Git",
            "git commit --amend -m \"new message\"",
            "git commit --amend -m \"new message\"",
            null),

        new("Stash uncommitted changes", "Git",
            "git stash",
            "git stash",
            null),

        new("Re-apply and drop the latest stash", "Git",
            "git stash pop",
            "git stash pop",
            null),

        new("Delete local branches already merged into main", "Git",
            "git branch --merged main | grep -vE '^\\*?\\s*main$' | xargs -r git branch -d",
            "git branch --merged main | Where-Object { $_ -notmatch '^\\*?\\s*main$' } | ForEach-Object { git branch -d $_.Trim() }",
            "-E enables extended regex for grep; xargs -r (GNU) skips the call entirely when there is no input."),

        new("Prune remote-tracking branches that no longer exist", "Git",
            "git fetch --prune",
            "git fetch --prune",
            null),

        new("Show a one-line commit graph", "Git",
            "git log --oneline --graph --all",
            "git log --oneline --graph --all",
            null),

        new("Discard all local uncommitted changes", "Git",
            "git checkout -- .",
            "git checkout -- .",
            "git restore . is the modern equivalent on Git 2.23+, identical in both shells."),

        new("Find who last changed each line of a file", "Git",
            "git blame file.txt",
            "git blame file.txt",
            null),

        new("Search commit history for a string that was added or removed", "Git",
            "git log -S\"needle\" --oneline",
            "git log -S\"needle\" --oneline",
            "-S is the pickaxe search: it finds commits that change the number of occurrences of the string."),
    };

    internal static IReadOnlyList<ShellEntry> MiscEntries { get; } = new List<ShellEntry>
    {
        new("Search command history", "Misc",
            "history | grep ssh",
            "Get-History | Where-Object { $_.CommandLine -match 'ssh' }",
            null),

        new("Define a reusable command alias", "Misc",
            "echo \"alias ll='ls -la'\" >> ~/.bashrc",
            "function ll { Get-ChildItem -Force @Args }",
            "PowerShell's Set-Alias can't bundle arguments the way a bash alias can - a function is the accurate equivalent."),

        new("Redirect stderr into stdout", "Misc",
            "mycommand 2>&1",
            "mycommand 2>&1",
            "PowerShell has supported numbered stream redirection like 2>&1 since v3."),

        new("Redirect stderr to a file and discard stdout", "Misc",
            "mycommand 2>error.log >/dev/null",
            "mycommand 2>error.log 1>$null",
            null),

        new("Run a second command only if the first succeeds", "Misc",
            "cmd1 && cmd2",
            "cmd1; if ($?) { cmd2 }",
            "&& / || short-circuit chaining is PowerShell 7+ only; the if ($?) form works on 5.1 too."),

        new("Run two commands unconditionally in sequence", "Misc",
            "cmd1 ; cmd2",
            "cmd1; cmd2",
            null),

        new("Feed a multi-line block of text to a command", "Misc",
            "cat <<EOF\nline one\nline two\nEOF",
            "@'\nline one\nline two\n'@",
            "The PowerShell here-string is usually assigned to a variable first, e.g. $text = @'...'@."),

        new("Measure how long a command takes to run", "Misc",
            "time mycommand",
            "Measure-Command { mycommand }",
            null),

        new("Repeat a command every few seconds", "Misc",
            "watch -n 2 mycommand",
            "while ($true) { mycommand; Start-Sleep -Seconds 2 }",
            "Windows PowerShell has no built-in watch; the while loop is the standard substitute."),

        new("Read a password without echoing it to the screen", "Misc",
            "read -s -p \"Password: \" pw",
            "$pw = Read-Host -AsSecureString -Prompt \"Password\"",
            null),
    };
}
