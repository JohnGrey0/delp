using Delp.Core.Tools.WebDev;

namespace Delp.Core.Tests.Tools.WebDev;

public class LoremToolTests
{
    [Fact]
    public void Generate_SeededOutput_IsDeterministic()
    {
        var options = new LoremOptions(LoremUnit.Paragraphs, 3, StartClassic: false, HtmlParagraphs: false, Seed: 42);
        var first = LoremTool.Generate(options);
        var second = LoremTool.Generate(options);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Generate_DifferentSeeds_ProduceDifferentOutput()
    {
        var a = LoremTool.Generate(new LoremOptions(LoremUnit.Sentences, 5, StartClassic: false, Seed: 1));
        var b = LoremTool.Generate(new LoremOptions(LoremUnit.Sentences, 5, StartClassic: false, Seed: 2));
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Generate_Words_RespectsCount()
    {
        var text = LoremTool.Generate(new LoremOptions(LoremUnit.Words, 12, StartClassic: false, Seed: 7));
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(12, words.Length);
    }

    [Fact]
    public void Generate_Sentences_RespectsCount()
    {
        var text = LoremTool.Generate(new LoremOptions(LoremUnit.Sentences, 4, StartClassic: false, Seed: 7));
        var sentenceCount = text.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
        Assert.Equal(4, sentenceCount);
    }

    [Fact]
    public void Generate_Paragraphs_RespectsCount()
    {
        var text = LoremTool.Generate(new LoremOptions(LoremUnit.Paragraphs, 3, StartClassic: false, Seed: 7));
        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, paragraphs.Length);
    }

    [Fact]
    public void Generate_StartClassic_BeginsWithClassicOpening()
    {
        var text = LoremTool.Generate(new LoremOptions(LoremUnit.Sentences, 2, StartClassic: true, Seed: 99));
        Assert.StartsWith("Lorem ipsum dolor sit amet", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_HtmlParagraphs_WrapsEachParagraphInPTags()
    {
        var text = LoremTool.Generate(new LoremOptions(LoremUnit.Paragraphs, 2, StartClassic: false, HtmlParagraphs: true, Seed: 3));
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.All(lines, l =>
        {
            Assert.StartsWith("<p>", l, StringComparison.Ordinal);
            Assert.EndsWith("</p>", l, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Generate_CountZero_Throws()
    {
        Assert.Throws<ArgumentException>(() => LoremTool.Generate(new LoremOptions(LoremUnit.Words, 0)));
    }

    [Fact]
    public void Generate_NegativeCount_Throws()
    {
        Assert.Throws<ArgumentException>(() => LoremTool.Generate(new LoremOptions(LoremUnit.Sentences, -1)));
    }

    [Fact]
    public void Generate_SentenceWordCounts_AreWithinSpecRange()
    {
        for (var seed = 0; seed < 20; seed++)
        {
            var text = LoremTool.Generate(new LoremOptions(LoremUnit.Sentences, 1, StartClassic: false, Seed: seed));
            var wordCount = text.TrimEnd('.').Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            Assert.InRange(wordCount, 6, 14);
        }
    }
}
