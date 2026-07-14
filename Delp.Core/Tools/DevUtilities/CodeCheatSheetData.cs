namespace Delp.Core.Tools.DevUtilities;

/// <summary>One language-specific example within a <see cref="CheatTopic"/>.</summary>
public sealed record CodeSnippet(string Language, string Code);

/// <summary>One cheat-sheet topic: an explanation plus a code snippet per language.</summary>
public sealed record CheatTopic(
    string Id,
    string Title,
    string Category,
    string Explanation,
    IReadOnlyList<CodeSnippet> Snippets);

/// <summary>
/// Static reference data for the Code Cheat Sheet tool. Content is split across
/// partial-class files by category (CodeCheatSheetData.Algorithms.cs, etc.) so
/// no single file is enormous. This file only holds the shared model, the
/// canonical language order, and the aggregation/search surface.
/// </summary>
public static partial class CodeCheatSheetData
{
    /// <summary>
    /// Canonical language display order, applied consistently to every topic's
    /// snippet list (a topic may include a subset, but never out of this order).
    /// </summary>
    public static readonly IReadOnlyList<string> LanguageOrder =
        ["C#", "Python", "JavaScript", "TypeScript", "Java", "C++", "Go", "Rust"];

    // Lazy on purpose: static field initializers spread across partial-class
    // files run in an unspecified cross-file order, so eagerly evaluating
    // this against the per-category fields (defined in the other
    // CodeCheatSheetData.*.cs files) could observe them before they're
    // assigned. Deferring to first access guarantees every partial file's
    // static initializers have already run.
    private static readonly Lazy<IReadOnlyList<CheatTopic>> LazyAll = new(BuildAll);

    public static IReadOnlyList<CheatTopic> All => LazyAll.Value;

    private static IReadOnlyList<CheatTopic> BuildAll()
    {
        var topics = new List<CheatTopic>();
        topics.AddRange(AlgorithmsTopics);
        topics.AddRange(DataStructuresTopics);
        topics.AddRange(LanguageConstructsTopics);
        topics.AddRange(PatternsTopics);
        topics.AddRange(EverydayTopics);
        return topics;
    }

    /// <summary>Case-insensitive substring match against title, category, or explanation.</summary>
    public static IReadOnlyList<CheatTopic> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return All;

        var q = query.Trim();
        return All.Where(t =>
                t.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                || t.Category.Contains(q, StringComparison.OrdinalIgnoreCase)
                || t.Explanation.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
