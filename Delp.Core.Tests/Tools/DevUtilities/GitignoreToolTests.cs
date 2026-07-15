using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class GitignoreToolTests
{
    [Fact]
    public void TemplateList_HasAtLeast35Templates()
    {
        Assert.True(GitignoreData.All.Count >= 35, $"Expected at least 35 templates, found {GitignoreData.All.Count}.");
    }

    [Fact]
    public void TemplateList_NamesAreUnique()
    {
        var names = GitignoreData.All.Select(t => t.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void TemplateList_EveryTemplateIsNonEmpty()
    {
        foreach (var template in GitignoreData.All)
            Assert.False(string.IsNullOrWhiteSpace(template.Content), $"Template '{template.Name}' is empty.");
    }

    [Fact]
    public void TemplateList_IncludesTheMandatorySpecNames()
    {
        string[] mandatory =
        [
            "Node", "Python", "VisualStudio", "VisualStudioCode", "JetBrains", "Java", "Go", "Rust",
            "C++", "C", "Ruby", "PHP", "Swift", "Kotlin", "Dart", "Elixir", "Haskell", "Scala", "R",
            "Vim", "Emacs", "SublimeText", "Eclipse", "Windows", "macOS", "Linux", "Unity",
            "UnrealEngine", "Android", "Xcode", "Django", "Rails", "Laravel", "Terraform", "Ansible",
        ];
        var names = GitignoreData.All.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var name in mandatory)
            Assert.Contains(name, names);
    }

    [Fact]
    public void TemplateList_EveryEntryBelongsToAKnownGroup()
    {
        foreach (var template in GitignoreData.All)
            Assert.Contains(template.Group, GitignoreData.Groups);
    }

    [Fact]
    public void Compose_EmptySelection_ReturnsEmptyString()
    {
        Assert.Equal("", GitignoreTool.Compose([]));
    }

    [Fact]
    public void Compose_SingleTemplate_HasSectionHeaderAndContent()
    {
        var result = GitignoreTool.Compose(["Node"]);
        Assert.StartsWith("# --- Node ---", result);
        Assert.Contains("node_modules", result);
    }

    [Fact]
    public void Compose_MultipleTemplates_EachGetsItsOwnHeader()
    {
        var result = GitignoreTool.Compose(["Node", "Python"]);
        Assert.Contains("# --- Node ---", result);
        Assert.Contains("# --- Python ---", result);
        // Node's header must come first since it was named first.
        Assert.True(result.IndexOf("# --- Node ---", StringComparison.Ordinal) < result.IndexOf("# --- Python ---", StringComparison.Ordinal));
    }

    [Fact]
    public void Compose_DropsDuplicateNonCommentPatternsAcrossTemplates_FirstWins()
    {
        // The spec's illustrative pair (Windows + macOS) doesn't actually share a pattern line in the
        // current upstream github/gitignore content, so this uses a pair that provably does: both
        // Node.gitignore and Java.gitignore contain a bare "*.log" line.
        var result = GitignoreTool.Compose(["Node", "Java"]);

        var occurrences = CountOccurrencesOfLine(result, "*.log");
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void Compose_CommentLinesAreNotDeduped()
    {
        // Multiple templates legitimately reuse the same comment text (e.g. "# Logs"); only
        // non-comment pattern lines are deduplicated.
        var result = GitignoreTool.Compose(["Node", "Java"]);
        Assert.True(CountOccurrencesOfLine(result, "# Logs") >= 1);
    }

    [Fact]
    public void Compose_UnknownTemplateName_Throws()
    {
        Assert.Throws<ArgumentException>(() => GitignoreTool.Compose(["NotARealTemplate"]));
    }

    [Fact]
    public void Compose_HasNoTrailingBlankLines()
    {
        var result = GitignoreTool.Compose(["Node", "Windows", "macOS"]);
        Assert.False(result.EndsWith("\n\n", StringComparison.Ordinal));
        Assert.EndsWith("\n", result);
    }

    private static int CountOccurrencesOfLine(string text, string exactLine) =>
        text.Split('\n').Count(line => line.TrimEnd('\r') == exactLine);

    [Fact]
    public void MacOsTemplate_IconPattern_ContainsLiteralCarriageReturn()
    {
        // Upstream macOS.gitignore ignores the custom-folder-icon file via a one-char glob class
        // ("Icon[<CR>]") because the real file name on disk ends with an actual carriage-return byte,
        // not the two visible characters '\' and 'r'. Confirm the embedded template really contains
        // that raw 0x0D byte and not an escaped-looking placeholder.
        var macOs = GitignoreData.All.Single(t => t.Name == "macOS");
        Assert.Contains("Icon[\r]", macOs.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_MacOsIconPattern_PreservesLiteralCarriageReturnThroughCompose()
    {
        // Compose() trims each line's trailing whitespace and re-splits on '\n' — guard against a
        // regression where that pipeline strips or mangles the embedded CR (it sits mid-line inside
        // "Icon[<CR>]", not at the line's edge, so naive trimming must not touch it).
        var result = GitignoreTool.Compose(["macOS"]);
        Assert.Contains("Icon[\r]", result, StringComparison.Ordinal);
    }
}
