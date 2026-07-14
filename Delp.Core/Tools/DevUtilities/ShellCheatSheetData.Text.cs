namespace Delp.Core.Tools.DevUtilities;

public static partial class ShellCheatSheetData
{
    internal static IReadOnlyList<ShellEntry> TextEntries { get; } = new List<ShellEntry>
    {
        new("Search for text in files recursively", "Text Processing",
            "grep -rn \"TODO\" .",
            "Get-ChildItem -Recurse -File | Select-String -Pattern \"TODO\"",
            "Piping Get-ChildItem into Select-String works on Windows PowerShell 5.1; Select-String itself gained -Recurse only later."),

        new("Replace text in a file in place", "Text Processing",
            "sed -i 's/foo/bar/g' file.txt",
            "(Get-Content file.txt) -replace 'foo','bar' | Set-Content file.txt",
            "-replace is a case-insensitive regex by default; sed's s/// is case-sensitive by default."),

        new("Sort lines of a file", "Text Processing",
            "sort file.txt",
            "Get-Content file.txt | Sort-Object",
            null),

        new("Remove duplicate lines", "Text Processing",
            "sort -u file.txt",
            "Get-Content file.txt | Sort-Object -Unique",
            "sort -u also sorts; use awk '!seen[$0]++' in bash to dedupe while preserving order."),

        new("Extract a column from delimited text", "Text Processing",
            "cut -d, -f2 file.csv",
            "Get-Content file.csv | ForEach-Object { ($_ -split ',')[1] }",
            "For quoted CSV fields prefer Import-Csv, which understands RFC 4180 quoting."),

        new("Count the lines in a file", "Text Processing",
            "wc -l file.txt",
            "(Get-Content file.txt | Measure-Object -Line).Lines",
            null),

        new("Show differences between two files", "Text Processing",
            "diff file1.txt file2.txt",
            "Compare-Object (Get-Content file1.txt) (Get-Content file2.txt)",
            "Compare-Object output uses <= for \"only in first\" and => for \"only in second\"."),

        new("Print matching lines with line numbers", "Text Processing",
            "grep -n \"error\" app.log",
            "Select-String -Pattern \"error\" -Path app.log",
            "Select-String includes the line number in its output object by default."),

        new("Concatenate multiple files", "Text Processing",
            "cat a.txt b.txt > combined.txt",
            "Get-Content a.txt, b.txt | Set-Content combined.txt",
            null),

        new("Search and replace across many files", "Text Processing",
            "grep -rl \"old\" . | xargs sed -i 's/old/new/g'",
            "Get-ChildItem -Recurse -File | Select-String -Pattern 'old' -List | ForEach-Object { (Get-Content $_.Path) -replace 'old','new' | Set-Content $_.Path }",
            "-List stops at the first match per file, which is enough to know the file needs editing."),

        new("Count word frequency in a file", "Text Processing",
            "tr -s ' ' '\\n' < file.txt | sort | uniq -c | sort -rn",
            "(Get-Content file.txt -Raw) -split '\\s+' | Where-Object { $_ } | Group-Object | Sort-Object Count -Descending",
            null),
    };
}
