namespace Delp.Core.Tools.DevUtilities;

/// <summary>One shell task with its bash and PowerShell equivalents.</summary>
public sealed record ShellEntry(
    string Task,
    string Category,
    string Bash,
    string PowerShell,
    string? Notes = null);

/// <summary>
/// Static reference data for the Shell Command Cheat Sheet tool. Entries are
/// split across partial-class files by category (ShellCheatSheetData.Files.cs,
/// etc.) so no single file is enormous.
/// </summary>
public static partial class ShellCheatSheetData
{
    // Lazy on purpose: static field initializers spread across partial-class
    // files run in an unspecified cross-file order, so eagerly evaluating
    // this against the per-category fields (defined in the other
    // ShellCheatSheetData.*.cs files) could observe them before they're
    // assigned. Deferring to first access guarantees every partial file's
    // static initializers have already run.
    private static readonly Lazy<IReadOnlyList<ShellEntry>> LazyAll = new(BuildAll);

    public static IReadOnlyList<ShellEntry> All => LazyAll.Value;

    private static IReadOnlyList<ShellEntry> BuildAll()
    {
        var entries = new List<ShellEntry>();
        entries.AddRange(FileEntries);
        entries.AddRange(TextEntries);
        entries.AddRange(ProcessEntries);
        entries.AddRange(NetworkEntries);
        entries.AddRange(ArchiveEntries);
        entries.AddRange(EnvironmentEntries);
        entries.AddRange(SystemEntries);
        entries.AddRange(GitEntries);
        entries.AddRange(MiscEntries);
        return entries;
    }

    /// <summary>Distinct categories in first-seen (canonical) order.</summary>
    private static readonly Lazy<IReadOnlyList<string>> LazyCategories =
        new(() => All.Select(e => e.Category).Distinct().ToList());

    public static IReadOnlyList<string> Categories => LazyCategories.Value;

    /// <summary>Case-insensitive filter by category and/or a substring of task, bash, or PowerShell.</summary>
    public static IReadOnlyList<ShellEntry> Search(string? query, string? category = null)
    {
        IEnumerable<ShellEntry> result = All;

        if (!string.IsNullOrWhiteSpace(category))
            result = result.Where(e => e.Category.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            result = result.Where(e =>
                e.Task.Contains(q, StringComparison.OrdinalIgnoreCase)
                || e.Bash.Contains(q, StringComparison.OrdinalIgnoreCase)
                || e.PowerShell.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (e.Notes is not null && e.Notes.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        return result.ToList();
    }
}
