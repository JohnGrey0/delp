using Delp.Core.Tools.TextProcessing;

namespace Delp.Core.Tests.Tools.TextProcessing;

public class PorterStemmerTests
{
    [Theory]
    // Mandatory vectors from the tool spec.
    [InlineData("caresses", "caress")]
    [InlineData("ponies", "poni")]
    [InlineData("relational", "relat")]
    [InlineData("conditional", "condit")]
    [InlineData("rational", "ration")]
    [InlineData("flying", "fli")]
    [InlineData("dies", "die")]
    public void Stem_MandatoryVectors_MatchSpec(string input, string expected)
    {
        Assert.Equal(expected, PorterStemmer.Stem(input));
    }

    [Theory]
    // Additional classic Step 1a/1b examples from the original paper. Note:
    // the paper's inline "agreed -> agree" / "conflat(ed) -> conflate" /
    // "troubl(ed) -> trouble" illustrations show Step 1b's output in
    // isolation; running the full pipeline (verified against this
    // implementation), Step 5a's generic "(m>1) E ->" rule strips that
    // trailing E right back off for these particular stems, since none of
    // them end in a qualifying (non-w/x/y) CVC pattern.
    [InlineData("caress", "caress")]
    [InlineData("cats", "cat")]
    [InlineData("ties", "tie")]
    [InlineData("feed", "feed")]
    [InlineData("agreed", "agre")]
    [InlineData("plastered", "plaster")]
    [InlineData("bled", "bled")]
    [InlineData("motoring", "motor")]
    [InlineData("sing", "sing")]
    [InlineData("conflated", "conflat")]
    [InlineData("troubled", "troubl")]
    [InlineData("sized", "size")]
    [InlineData("hopping", "hop")]
    [InlineData("tanned", "tan")]
    [InlineData("falling", "fall")]
    [InlineData("hissing", "hiss")]
    [InlineData("fizzed", "fizz")]
    [InlineData("failing", "fail")]
    [InlineData("filing", "file")]
    [InlineData("happy", "happi")]
    public void Stem_ClassicPaperExamples_MatchSpec(string input, string expected)
    {
        Assert.Equal(expected, PorterStemmer.Stem(input));
    }

    [Theory]
    // Steps 2-5 examples from the original paper.
    [InlineData("valenci", "valenc")]
    [InlineData("hesitanci", "hesit")]
    [InlineData("digitizer", "digit")]
    [InlineData("conformabli", "conform")]
    [InlineData("radicalli", "radic")]
    [InlineData("differentli", "differ")]
    [InlineData("vileli", "vile")]
    [InlineData("analogousli", "analog")]
    [InlineData("vietnamization", "vietnam")]
    [InlineData("predication", "predic")]
    [InlineData("operator", "oper")]
    [InlineData("feudalism", "feudal")]
    [InlineData("decisiveness", "decis")]
    [InlineData("hopefulness", "hope")]
    [InlineData("callousness", "callous")]
    [InlineData("formaliti", "formal")]
    [InlineData("sensitiviti", "sensit")]
    [InlineData("sensibiliti", "sensibl")]
    [InlineData("triplicate", "triplic")]
    [InlineData("formative", "form")]
    [InlineData("formalize", "formal")]
    [InlineData("electriciti", "electr")]
    [InlineData("electrical", "electr")]
    [InlineData("hopeful", "hope")]
    [InlineData("goodness", "good")]
    [InlineData("revival", "reviv")]
    [InlineData("allowance", "allow")]
    [InlineData("inference", "infer")]
    [InlineData("airliner", "airlin")]
    [InlineData("gyroscopic", "gyroscop")]
    [InlineData("adjustable", "adjust")]
    [InlineData("defensible", "defens")]
    [InlineData("irritant", "irrit")]
    [InlineData("replacement", "replac")]
    [InlineData("adjustment", "adjust")]
    [InlineData("dependent", "depend")]
    [InlineData("adoption", "adopt")]
    [InlineData("homologou", "homolog")]
    [InlineData("communism", "commun")]
    [InlineData("activate", "activ")]
    [InlineData("angulariti", "angular")]
    [InlineData("homologous", "homolog")]
    [InlineData("effective", "effect")]
    [InlineData("bowdlerize", "bowdler")]
    [InlineData("probate", "probat")]
    [InlineData("rate", "rate")]
    [InlineData("cease", "ceas")]
    [InlineData("controll", "control")]
    [InlineData("roll", "roll")]
    public void Stem_StepTwoThroughFiveExamples_MatchSpec(string input, string expected)
    {
        Assert.Equal(expected, PorterStemmer.Stem(input));
    }

    [Theory]
    // Additional spot-check vectors beyond the paper's own examples, hand
    // traced against the reference (C/NLTK) algorithm to confirm this
    // implementation's step interactions (cascading Step2/3 rewrites,
    // Step4's m&gt;1 "ion" gate, Step5b double-consonant trim) hold up on
    // words the paper's vocabulary doesn't cover.
    [InlineData("running", "run")]
    [InlineData("happiness", "happi")]
    [InlineData("national", "nation")]
    [InlineData("sensational", "sensat")]
    [InlineData("controlling", "control")]
    [InlineData("possibly", "possibli")]
    [InlineData("generalization", "gener")]
    [InlineData("meetings", "meet")]
    [InlineData("capabilities", "capabl")]
    [InlineData("reflection", "reflect")]
    public void Stem_AdditionalSpotCheckVectors_MatchReferenceAlgorithm(string input, string expected)
    {
        Assert.Equal(expected, PorterStemmer.Stem(input));
    }

    [Fact]
    public void Stem_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal("", PorterStemmer.Stem(""));
    }

    [Fact]
    public void Stem_ShortWord_ReturnedLowercasedUnchanged()
    {
        Assert.Equal("at", PorterStemmer.Stem("AT"));
        Assert.Equal("i", PorterStemmer.Stem("i"));
    }

    [Fact]
    public void Stem_NonAlphabeticInput_ReturnedUnchanged()
    {
        Assert.Equal("don't", PorterStemmer.Stem("don't"));
        Assert.Equal("café", PorterStemmer.Stem("café"));
        Assert.Equal("2024", PorterStemmer.Stem("2024"));
    }

    [Fact]
    public void Stem_IsCaseInsensitiveAndLowercasesResult()
    {
        Assert.Equal("caress", PorterStemmer.Stem("CARESSES"));
        Assert.Equal("caress", PorterStemmer.Stem("Caresses"));
    }
}
