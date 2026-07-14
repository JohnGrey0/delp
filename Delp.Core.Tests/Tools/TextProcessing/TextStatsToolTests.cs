using Delp.Core.Tools.TextProcessing;

namespace Delp.Core.Tests.Tools.TextProcessing;

public class TextStatsToolTests
{
    // "The cat sat on the mat.\nThe cat ran fast!\n\nDogs bark. Cats meow."
    // 4 lines (3 non-empty), 2 blank-separated paragraphs, 14 words, 11 case-insensitive
    // unique words, 4 sentences, all ASCII (64 chars == 64 UTF-8 bytes, 50 non-space chars).
    private const string Fixture =
        "The cat sat on the mat.\nThe cat ran fast!\n\nDogs bark. Cats meow.";

    [Fact]
    public void Analyze_FixtureText_MatchesKnownCounts()
    {
        var stats = TextStatsTool.Analyze(Fixture);

        Assert.Equal(64, stats.Chars);
        Assert.Equal(50, stats.CharsNoSpaces);
        Assert.Equal(14, stats.Words);
        Assert.Equal(11, stats.UniqueWords);
        Assert.Equal(4, stats.Lines);
        Assert.Equal(3, stats.NonEmptyLines);
        Assert.Equal(4, stats.Sentences);
        Assert.Equal(2, stats.Paragraphs);
        Assert.Equal(64, stats.Utf8Bytes);
        Assert.Equal(46.0 / 14.0, stats.AvgWordLength, precision: 10);
        Assert.Equal(4.2, stats.ReadingTimeSeconds, precision: 10);
    }

    [Fact]
    public void Analyze_FixtureText_TopWordsExcludesStopwordsAndOrdersByFrequency()
    {
        var stats = TextStatsTool.Analyze(Fixture, topWordsCount: 10);

        Assert.Equal(9, stats.TopWords.Count);
        Assert.Equal("cat", stats.TopWords[0].Word);
        Assert.Equal(2, stats.TopWords[0].Count);
        Assert.DoesNotContain(stats.TopWords, w => w.Word is "the" or "on");
        Assert.All(stats.TopWords.Skip(1), w => Assert.Equal(1, w.Count));
    }

    [Fact]
    public void TopWords_RespectsRequestedCount()
    {
        var top2 = TextStatsTool.TopWords(Fixture, 2);
        Assert.Equal(2, top2.Count);
        Assert.Equal("cat", top2[0].Word);
    }

    [Fact]
    public void CountSentences_IgnoresCommonAbbreviations()
    {
        const string text = "I like fruits, e.g. apples and bananas. They are healthy.";
        Assert.Equal(2, TextStatsTool.CountSentences(text));
    }

    [Fact]
    public void CountSentences_HandlesMrAndDrAbbreviations()
    {
        const string text = "Dr. Smith met Mr. Jones. They shook hands.";
        Assert.Equal(2, TextStatsTool.CountSentences(text));
    }

    [Fact]
    public void CountSentences_TrailingClauseWithoutPunctuationStillCounts()
    {
        const string text = "First sentence. Second without a period";
        Assert.Equal(2, TextStatsTool.CountSentences(text));
    }

    [Fact]
    public void CountParagraphs_IgnoresLeadingAndTrailingBlankLines()
    {
        const string text = "\n\nfirst paragraph\n\n\nsecond paragraph\n\n";
        Assert.Equal(2, TextStatsTool.CountParagraphs(text));
    }

    [Fact]
    public void Analyze_EmptyText_AllZeros()
    {
        var stats = TextStatsTool.Analyze("");

        Assert.Equal(0, stats.Chars);
        Assert.Equal(0, stats.CharsNoSpaces);
        Assert.Equal(0, stats.Words);
        Assert.Equal(0, stats.UniqueWords);
        Assert.Equal(0, stats.Lines);
        Assert.Equal(0, stats.NonEmptyLines);
        Assert.Equal(0, stats.Sentences);
        Assert.Equal(0, stats.Paragraphs);
        Assert.Equal(0, stats.Utf8Bytes);
        Assert.Equal(0, stats.AvgWordLength);
        Assert.Equal(0, stats.ReadingTimeSeconds);
        Assert.Empty(stats.TopWords);
    }

    [Fact]
    public void Analyze_ReadingTimeMath_TwoHundredWordsPerMinute()
    {
        var text = string.Join(" ", Enumerable.Repeat("word", 200));
        var stats = TextStatsTool.Analyze(text);

        Assert.Equal(200, stats.Words);
        Assert.Equal(60.0, stats.ReadingTimeSeconds, precision: 10);
    }
}
