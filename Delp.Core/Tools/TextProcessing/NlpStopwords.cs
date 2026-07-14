namespace Delp.Core.Tools.TextProcessing;

/// <summary>Embedded English stopword list used by <see cref="NlpTool"/>.</summary>
public static class NlpStopwords
{
    /// <summary>
    /// ~175 standard English function words (articles, pronouns,
    /// prepositions, conjunctions, auxiliary verbs, and common contractions
    /// kept whole since the tokenizer keeps internal apostrophes).
    /// Lowercase, compared case-insensitively by the caller.
    /// </summary>
    public static IReadOnlySet<string> Words { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "a", "about", "above", "after", "again", "against", "all", "am", "an", "and",
        "any", "are", "aren't", "as", "at", "be", "because", "been", "before", "being",
        "below", "between", "both", "but", "by", "can", "can't", "cannot", "could",
        "couldn't", "did", "didn't", "do", "does", "doesn't", "doing", "don't", "down",
        "during", "each", "either", "else", "ever", "every", "few", "for", "from",
        "further", "had", "hadn't", "has", "hasn't", "have", "haven't", "having", "he",
        "he'd", "he'll", "he's", "her", "here", "here's", "hers", "herself", "him",
        "himself", "his", "how", "however", "i", "i'd", "i'll", "i'm", "i've", "if",
        "in", "into", "is", "isn't", "it", "it'll", "it's", "its", "itself", "just",
        "let's", "ma'am", "many", "may", "me", "might", "more", "most", "much", "must",
        "mustn't", "my", "myself", "neither", "never", "no", "nor", "not", "now", "of",
        "off", "on", "once", "one", "only", "or", "other", "ought", "our", "ours",
        "ourselves", "out", "over", "own", "same", "shall", "shan't", "she", "she'd",
        "she'll", "she's", "should", "should've", "shouldn't", "so", "some", "such",
        "than", "that", "that'll", "that's", "the", "their", "theirs", "them",
        "themselves", "then", "there", "there's", "these", "they", "they'd", "they'll",
        "they're", "they've", "this", "those", "through", "thus", "to", "too", "under",
        "until", "up", "upon", "very", "was", "wasn't", "we", "we'd", "we'll", "we're",
        "we've", "were", "weren't", "what", "what's", "whatever", "when", "where",
        "where's", "whereas", "whether", "which", "whichever", "while", "who", "who's",
        "whoever", "whom", "whomever", "whose", "why", "will", "with", "within",
        "without", "won't", "would", "wouldn't", "yet", "you", "you'd", "you'll",
        "you're", "you've", "your", "yours", "yourself", "yourselves",
    };
}
