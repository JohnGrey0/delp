namespace Delp.Core.Tools.DevUtilities;

public static partial class ShellCheatSheetData
{
    internal static IReadOnlyList<ShellEntry> FileEntries { get; } = new List<ShellEntry>
    {
        new("List files with details", "Files & Directories",
            "ls -la",
            "Get-ChildItem -Force",
            "-Force shows hidden/dot files, matching ls -a; without it hidden items are skipped."),

        new("Find files by name recursively", "Files & Directories",
            "find . -iname \"*.txt\"",
            "Get-ChildItem -Path . -Recurse -Filter *.txt",
            "-iname is case-insensitive; Get-ChildItem -Filter is case-insensitive by default on Windows."),

        new("Copy a directory recursively", "Files & Directories",
            "cp -r src dst",
            "Copy-Item -Path src -Destination dst -Recurse",
            null),

        new("Move or rename a file or directory", "Files & Directories",
            "mv old new",
            "Move-Item -Path old -Destination new",
            null),

        new("Delete a directory recursively", "Files & Directories",
            "rm -rf dir",
            "Remove-Item -Path dir -Recurse -Force",
            "Both are irreversible - there is no recycle bin involved."),

        new("Show the total size of a directory", "Files & Directories",
            "du -sh dir",
            "\"{0:N2} MB\" -f ((Get-ChildItem dir -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB)",
            "PowerShell has no built-in human-readable du; sum the Length of every file and format it."),

        new("Follow a growing file (tail -f)", "Files & Directories",
            "tail -f app.log",
            "Get-Content app.log -Wait -Tail 10",
            "-Tail 10 starts from the last 10 lines, then -Wait streams new lines as they are appended."),

        new("Show the first N lines of a file (head)", "Files & Directories",
            "head -n 20 file.txt",
            "Get-Content file.txt -TotalCount 20",
            "-TotalCount has the alias -First / -Head."),

        new("Create a symbolic link", "Files & Directories",
            "ln -s /path/to/target linkname",
            "New-Item -ItemType SymbolicLink -Path linkname -Target /path/to/target",
            "On Windows this needs an elevated prompt or Developer Mode enabled."),

        new("View or change file permissions", "Files & Directories",
            "chmod 755 deploy.sh",
            "icacls deploy.sh /grant Everyone:RX",
            "Unix permission bits and Windows ACLs are different models - this only approximates read/execute access."),

        new("Create a directory including parent directories", "Files & Directories",
            "mkdir -p a/b/c",
            "New-Item -ItemType Directory -Force -Path a/b/c",
            "-Force also suppresses the error if the directory already exists."),

        new("Check whether a file exists", "Files & Directories",
            "test -f file.txt && echo yes",
            "Test-Path -Path file.txt -PathType Leaf",
            "Add -PathType Container to test for a directory instead."),

        new("Count files in a directory", "Files & Directories",
            "ls -1 | wc -l",
            "(Get-ChildItem -File).Count",
            null),

        new("Delete files older than N days", "Files & Directories",
            "find . -type f -mtime +30 -delete",
            "Get-ChildItem -Recurse -File | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } | Remove-Item -Force",
            "find -mtime uses last-modified time in whole 24h units, matching LastWriteTime here."),
    };
}
