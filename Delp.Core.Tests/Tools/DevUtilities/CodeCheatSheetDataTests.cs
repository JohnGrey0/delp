using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class CodeCheatSheetDataTests
{
    [Fact]
    public void All_HasAtLeast18Topics()
    {
        Assert.True(CodeCheatSheetData.All.Count >= 18,
            $"Expected >= 18 topics, found {CodeCheatSheetData.All.Count}.");
    }

    [Fact]
    public void All_IdsAreUnique()
    {
        var ids = CodeCheatSheetData.All.Select(t => t.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void All_EveryTopicHasNonEmptyCoreFields()
    {
        Assert.All(CodeCheatSheetData.All, t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Id));
            Assert.False(string.IsNullOrWhiteSpace(t.Title));
            Assert.False(string.IsNullOrWhiteSpace(t.Category));
            Assert.False(string.IsNullOrWhiteSpace(t.Explanation));
        });
    }

    [Fact]
    public void All_EveryTopicHasAtLeast6Languages()
    {
        foreach (var topic in CodeCheatSheetData.All)
            Assert.True(topic.Snippets.Count >= 6,
                $"Topic '{topic.Id}' has only {topic.Snippets.Count} languages (need >= 6).");
    }

    [Fact]
    public void All_LanguageSetsFollowCanonicalOrderWithNoDuplicates()
    {
        var order = CodeCheatSheetData.LanguageOrder.ToList();

        foreach (var topic in CodeCheatSheetData.All)
        {
            var indices = topic.Snippets.Select(s => order.IndexOf(s.Language)).ToList();

            Assert.True(indices.All(i => i >= 0),
                $"Topic '{topic.Id}' uses a language outside the canonical LanguageOrder.");

            var sorted = indices.OrderBy(i => i).ToList();
            Assert.True(indices.SequenceEqual(sorted),
                $"Topic '{topic.Id}' languages are not listed in canonical order.");

            Assert.True(indices.Distinct().Count() == indices.Count,
                $"Topic '{topic.Id}' lists the same language more than once.");
        }
    }

    [Fact]
    public void All_NoEmptyOrWhitespaceSnippets()
    {
        foreach (var topic in CodeCheatSheetData.All)
            foreach (var snippet in topic.Snippets)
                Assert.False(string.IsNullOrWhiteSpace(snippet.Code),
                    $"Topic '{topic.Id}' language '{snippet.Language}' has an empty snippet.");
    }

    [Fact]
    public void All_EverySnippetUnder60Lines()
    {
        foreach (var topic in CodeCheatSheetData.All)
            foreach (var snippet in topic.Snippets)
            {
                var lineCount = snippet.Code.Split('\n').Length;
                Assert.True(lineCount < 60,
                    $"Topic '{topic.Id}' language '{snippet.Language}' snippet has {lineCount} lines (must be < 60).");
            }
    }

    [Fact]
    public void Search_MatchesTitle()
    {
        var sample = CodeCheatSheetData.All.First();
        var word = sample.Title.Split(' ', '/', '+')[0];
        var results = CodeCheatSheetData.Search(word);
        Assert.Contains(results, t => t.Id == sample.Id);
    }

    [Fact]
    public void Search_MatchesCategory()
    {
        // Search matches title/category/explanation, so a category-name query can pull in
        // unrelated topics whose explanation happens to mention the word too; just assert
        // the originating topic itself comes back rather than asserting every result's
        // Category field equals the query.
        var sample = CodeCheatSheetData.All.First();
        var results = CodeCheatSheetData.Search(sample.Category);
        Assert.Contains(results, t => t.Id == sample.Id);
    }

    [Fact]
    public void Search_NullOrWhitespace_ReturnsAll()
    {
        Assert.Equal(CodeCheatSheetData.All.Count, CodeCheatSheetData.Search(null).Count);
        Assert.Equal(CodeCheatSheetData.All.Count, CodeCheatSheetData.Search("   ").Count);
    }

    [Fact]
    public void Search_NoMatch_ReturnsEmpty()
    {
        Assert.Empty(CodeCheatSheetData.Search("zzzzzz_no_such_topic_zzzzzz"));
    }

    [Theory]
    [InlineData("Algorithms")]
    [InlineData("Data Structures")]
    [InlineData("Language Constructs")]
    [InlineData("Patterns")]
    [InlineData("Everyday")]
    public void All_ContainsExpectedCategory(string category)
    {
        Assert.Contains(CodeCheatSheetData.All, t => t.Category == category);
    }

    [Theory]
    [InlineData("bubble-sort")]
    [InlineData("insertion-sort")]
    [InlineData("merge-sort")]
    [InlineData("quicksort")]
    [InlineData("binary-search")]
    [InlineData("stack")]
    [InlineData("queue")]
    [InlineData("hash-map")]
    [InlineData("linked-list")]
    [InlineData("binary-tree-traversal")]
    [InlineData("class-inheritance")]
    [InlineData("interface-trait")]
    [InlineData("struct-record")]
    [InlineData("enum")]
    [InlineData("generics")]
    [InlineData("closure-lambda")]
    [InlineData("error-handling")]
    [InlineData("async-await")]
    [InlineData("singleton")]
    [InlineData("factory")]
    [InlineData("observer")]
    [InlineData("builder")]
    [InlineData("read-write-file")]
    [InlineData("parse-json")]
    [InlineData("http-get")]
    [InlineData("string-formatting")]
    public void All_ContainsExpectedTopicId(string id)
    {
        Assert.Contains(CodeCheatSheetData.All, t => t.Id == id);
    }

    [Fact]
    public void LanguageOrder_HasEightCanonicalLanguages()
    {
        Assert.Equal(new[] { "C#", "Python", "JavaScript", "TypeScript", "Java", "C++", "Go", "Rust" },
            CodeCheatSheetData.LanguageOrder);
    }
}
