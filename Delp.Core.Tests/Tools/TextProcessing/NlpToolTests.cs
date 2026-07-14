using Delp.Core.Tools.TextProcessing;

namespace Delp.Core.Tests.Tools.TextProcessing;

public class NlpToolTests
{
    private static readonly NlpTool.NlpOptions AllOn = new(
        Lowercase: true, RemoveStopwords: true, RemovePunctuation: true,
        RemoveNumbers: true, Stem: true);

    private static readonly NlpTool.NlpOptions None = new(
        Lowercase: false, RemoveStopwords: false, RemovePunctuation: false,
        RemoveNumbers: false, Stem: false);

    [Fact]
    public void Process_EmptyInput_ReturnsEmptyResult()
    {
        var result = NlpTool.Process("", None);
        Assert.Equal("", result.ProcessedText);
        Assert.Empty(result.Tokens);
        Assert.Empty(result.Frequencies);
        Assert.Equal(0, result.SentenceCount);
    }

    [Fact]
    public void Process_NoOptions_TokenizesOnly()
    {
        var result = NlpTool.Process("The Quick Brown Fox.", None);
        Assert.Equal(["The", "Quick", "Brown", "Fox"], result.Tokens);
        Assert.Equal("The Quick Brown Fox", result.ProcessedText);
    }

    [Fact]
    public void Process_RemoveStopwords_DropsCommonWords()
    {
        var options = new NlpTool.NlpOptions(Lowercase: true, RemoveStopwords: true,
            RemovePunctuation: false, RemoveNumbers: false, Stem: false);
        var result = NlpTool.Process("The cat and the dog are friends", options);
        Assert.DoesNotContain("the", result.Tokens);
        Assert.DoesNotContain("and", result.Tokens);
        Assert.DoesNotContain("are", result.Tokens);
        Assert.Contains("cat", result.Tokens);
        Assert.Contains("dog", result.Tokens);
        Assert.Contains("friends", result.Tokens);
    }

    [Fact]
    public void Process_ExtraStopwords_AreAlsoRemoved()
    {
        var options = new NlpTool.NlpOptions(Lowercase: true, RemoveStopwords: true,
            RemovePunctuation: false, RemoveNumbers: false, Stem: false,
            ExtraStopwords: "cat, dog");
        var result = NlpTool.Process("The cat and the dog are friends", options);
        Assert.DoesNotContain("cat", result.Tokens);
        Assert.DoesNotContain("dog", result.Tokens);
        Assert.Contains("friends", result.Tokens);
    }

    [Fact]
    public void Process_StemAppliesAfterStopwordRemoval()
    {
        // "is" is a stopword; if stemming ran before stopword removal it
        // could not change that, but this verifies the two interact
        // correctly: a word that is NOT a stopword pre-stem must survive
        // and end up stemmed.
        var options = new NlpTool.NlpOptions(Lowercase: true, RemoveStopwords: true,
            RemovePunctuation: false, RemoveNumbers: false, Stem: true);
        var result = NlpTool.Process("The ponies are running", options);
        Assert.Contains("poni", result.Tokens);
        Assert.Contains("run", result.Tokens);
        Assert.DoesNotContain("the", result.Tokens);
        Assert.DoesNotContain("are", result.Tokens);
    }

    [Fact]
    public void Process_RemovePunctuation_StripsApostrophes()
    {
        var options = new NlpTool.NlpOptions(Lowercase: true, RemoveStopwords: false,
            RemovePunctuation: true, RemoveNumbers: false, Stem: false);
        var result = NlpTool.Process("don't stop", options);
        Assert.Contains("dont", result.Tokens);
        Assert.DoesNotContain("don't", result.Tokens);
    }

    [Fact]
    public void Process_RemoveNumbers_DropsPureNumericTokens()
    {
        var options = new NlpTool.NlpOptions(Lowercase: false, RemoveStopwords: false,
            RemovePunctuation: false, RemoveNumbers: true, Stem: false);
        var result = NlpTool.Process("Room 42 has 3 chairs", options);
        Assert.DoesNotContain("42", result.Tokens);
        Assert.DoesNotContain("3", result.Tokens);
        Assert.Contains("Room", result.Tokens);
        Assert.Contains("chairs", result.Tokens);
    }

    [Fact]
    public void Process_PreservesLineBreaks()
    {
        var result = NlpTool.Process("hello world\nsecond line", None);
        Assert.Equal("hello world\nsecond line", result.ProcessedText);
    }

    [Fact]
    public void Process_Frequencies_OrderedByCountThenAlpha()
    {
        var result = NlpTool.Process("b a b c a b", None);
        Assert.Equal(("b", 3), result.Frequencies[0]);
        Assert.Equal(("a", 2), result.Frequencies[1]);
        Assert.Equal(("c", 1), result.Frequencies[2]);
    }

    [Fact]
    public void Process_SentenceCount_CountsTerminators()
    {
        var result = NlpTool.Process("Hello world. How are you? Fine!", None);
        Assert.Equal(3, result.SentenceCount);
    }

    [Fact]
    public void Process_SentenceCount_TrailingTextWithoutTerminatorCountsOnce()
    {
        var result = NlpTool.Process("No terminator here", None);
        Assert.Equal(1, result.SentenceCount);
    }

    [Fact]
    public void Process_SentenceCount_EmptyIsZero()
    {
        Assert.Equal(0, NlpTool.Process("   ", None).SentenceCount);
    }

    [Fact]
    public void Ngrams_Bigrams_CountedAndOrdered()
    {
        var tokens = new[] { "the", "cat", "sat", "on", "the", "cat" };
        var grams = NlpTool.Ngrams(tokens, 2);
        Assert.Equal("the cat", grams[0].Gram);
        Assert.Equal(2, grams[0].Count);
    }

    [Fact]
    public void Ngrams_Trigrams_CountedCorrectly()
    {
        var tokens = new[] { "a", "b", "c", "a", "b", "c" };
        var grams = NlpTool.Ngrams(tokens, 3);
        var abc = grams.Single(g => g.Gram == "a b c");
        Assert.Equal(2, abc.Count);
    }

    [Fact]
    public void Ngrams_FewerTokensThanN_ReturnsEmpty()
    {
        Assert.Empty(NlpTool.Ngrams(["only", "two"], 3));
    }

    [Fact]
    public void Ngrams_InvalidN_Throws()
    {
        Assert.Throws<ArgumentException>(() => NlpTool.Ngrams(["a", "b", "c"], 4));
        Assert.Throws<ArgumentException>(() => NlpTool.Ngrams(["a", "b", "c"], 1));
    }

    [Fact]
    public void Process_AllOptions_FullPipeline()
    {
        var result = NlpTool.Process("The Cats' 9 lives ARE amazing!", AllOn);
        // lowercase -> strip punct('9 stays, apostrophe stripped) -> remove numbers -> stopwords -> stem
        Assert.DoesNotContain("the", result.Tokens);
        Assert.DoesNotContain("are", result.Tokens);
        Assert.DoesNotContain("9", result.Tokens);
        Assert.Contains("cat", result.Tokens); // "cats'" -> "cats" -> stem "cat"
        Assert.Contains("live", result.Tokens); // "lives" -> stem "live"
        Assert.Contains("amaz", result.Tokens); // "amazing" -> stem "amaz"
    }
}
