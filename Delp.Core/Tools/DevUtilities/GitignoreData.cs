// Template content in GitignoreData.Languages.cs, GitignoreData.Ides.cs, GitignoreData.Os.cs, and
// GitignoreData.Frameworks.cs is reference DATA (not code) taken verbatim from the community-maintained
// https://github.com/github/gitignore repository, licensed CC0-1.0 (public domain dedication) — see
// https://github.com/github/gitignore/blob/main/LICENSE. It was fetched once at authoring time and is
// embedded here so the shipped app never touches the network. Two templates that do not exist upstream
// at that repository's current HEAD (PHP, Django — both were present historically and are still commonly
// requested) were hand-written to match the style and content of their last known upstream versions.
//
// PHP.gitignore and Django.gitignore are the only entries not fetched verbatim.

namespace Delp.Core.Tools.DevUtilities;

/// <summary>One .gitignore template: a named, grouped block of patterns.</summary>
public sealed record GitignoreTemplate(string Name, string Group, string Content);

/// <summary>
/// Static reference data for the .gitignore Generator tool. Entries are split across partial-class
/// files by group (GitignoreData.Languages.cs, etc.) so no single file is enormous.
/// </summary>
public static partial class GitignoreData
{
    // Lazy on purpose: static field initializers spread across partial-class files run in an
    // unspecified cross-file order, so eagerly evaluating this against the per-group fields
    // (defined in the other GitignoreData.*.cs files) could observe them before they're assigned.
    // Deferring to first access guarantees every partial file's static initializers have already run.
    private static readonly Lazy<IReadOnlyList<GitignoreTemplate>> LazyAll = new(BuildAll);

    public static IReadOnlyList<GitignoreTemplate> All => LazyAll.Value;

    private static IReadOnlyList<GitignoreTemplate> BuildAll()
    {
        var entries = new List<GitignoreTemplate>();
        entries.AddRange(LanguageTemplates);
        entries.AddRange(IdeTemplates);
        entries.AddRange(OsTemplates);
        entries.AddRange(FrameworkTemplates);
        return entries;
    }

    /// <summary>Groups in canonical (fixed) display order.</summary>
    public static IReadOnlyList<string> Groups { get; } = ["Languages", "IDEs", "OS", "Frameworks"];

    /// <summary>Lightweight (Name, Group) projection for the UI's checklist, without the full template bodies.</summary>
    public static IReadOnlyList<(string Name, string Group)> Templates =>
        All.Select(t => (t.Name, t.Group)).ToList();
}
