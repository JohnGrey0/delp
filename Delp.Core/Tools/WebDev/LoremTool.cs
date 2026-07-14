namespace Delp.Core.Tools.WebDev;

public enum LoremUnit { Words, Sentences, Paragraphs }

public sealed record LoremOptions(
    LoremUnit Unit,
    int Count,
    bool StartClassic = true,
    bool HtmlParagraphs = false,
    int? Seed = null);

/// <summary>Deterministic (when seeded) classic Lorem Ipsum placeholder text generator.</summary>
public static class LoremTool
{
    // The traditional opening line, used when StartClassic is set and also folded into the
    // random corpus below so it keeps appearing naturally later in longer output.
    private static readonly string[] ClassicOpening =
        "lorem ipsum dolor sit amet consectetur adipiscing elit".Split(' ');

    private static readonly string[] Corpus =
    [
        "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit", "sed", "do",
        "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore", "magna", "aliqua", "enim",
        "ad", "minim", "veniam", "quis", "nostrud", "exercitation", "ullamco", "laboris", "nisi", "aliquip",
        "ex", "ea", "commodo", "consequat", "duis", "aute", "irure", "in", "reprehenderit", "voluptate",
        "velit", "esse", "cillum", "eu", "fugiat", "nulla", "pariatur", "excepteur", "sint", "occaecat",
        "cupidatat", "non", "proident", "sunt", "culpa", "qui", "officia", "deserunt", "mollit", "anim",
        "id", "est", "laborum", "at", "vero", "eos", "accusamus", "iusto", "odio", "dignissimos",
        "ducimus", "blanditiis",
    ];

    /// <exception cref="ArgumentException">Count is less than 1.</exception>
    public static string Generate(LoremOptions options)
    {
        if (options.Count < 1)
            throw new ArgumentException("Count must be at least 1.", nameof(options));

        var random = options.Seed.HasValue ? new Random(options.Seed.Value) : new Random();

        return options.Unit switch
        {
            LoremUnit.Words => CapitalizeFirst(JoinWords(BuildWords(options.Count, options.StartClassic, random))),
            LoremUnit.Sentences => GenerateSentences(options.Count, options.StartClassic, random),
            LoremUnit.Paragraphs => GenerateParagraphs(options.Count, options.StartClassic, options.HtmlParagraphs, random),
            _ => throw new ArgumentOutOfRangeException(nameof(options), options.Unit, "Unknown lorem unit."),
        };
    }

    private static string GenerateSentences(int count, bool startClassic, Random random)
    {
        var sentences = new List<string>(count);
        for (var i = 0; i < count; i++)
            sentences.Add(BuildSentence(random, startClassic && i == 0));
        return string.Join(' ', sentences);
    }

    private static string GenerateParagraphs(int count, bool startClassic, bool htmlParagraphs, Random random)
    {
        var paragraphs = new List<string>(count);
        for (var i = 0; i < count; i++)
            paragraphs.Add(BuildParagraph(random, startClassic && i == 0));

        return htmlParagraphs
            ? string.Join("\n", paragraphs.Select(p => $"<p>{p}</p>"))
            : string.Join("\n\n", paragraphs);
    }

    private static string BuildParagraph(Random random, bool forceClassicStart)
    {
        var sentenceCount = random.Next(4, 8); // 4..7 sentences
        var sentences = new List<string>(sentenceCount);
        for (var i = 0; i < sentenceCount; i++)
            sentences.Add(BuildSentence(random, forceClassicStart && i == 0));
        return string.Join(' ', sentences);
    }

    private static string BuildSentence(Random random, bool forceClassicStart)
    {
        var length = random.Next(6, 15); // 6..14 words
        var words = BuildWords(length, forceClassicStart, random);
        return CapitalizeFirst(JoinWords(words)) + ".";
    }

    private static List<string> BuildWords(int count, bool startClassic, Random random)
    {
        var words = new List<string>(count);

        if (startClassic)
        {
            foreach (var w in ClassicOpening)
            {
                if (words.Count >= count) break;
                words.Add(w);
            }
        }

        while (words.Count < count)
            words.Add(Corpus[random.Next(Corpus.Length)]);

        return words;
    }

    private static string JoinWords(IEnumerable<string> words) => string.Join(' ', words);

    private static string CapitalizeFirst(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
