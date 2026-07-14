using Delp.Core.Tools.TextProcessing;

namespace Delp.Core.Tests.Tools.TextProcessing;

public class CaseToolTests
{
    [Fact]
    public void Tokenize_SplitsAcronymRunsAndDigitBoundaries()
    {
        Assert.Equal(["http", "server", "2", "go"], CaseTool.Tokenize("HTTPServer2Go"));
    }

    [Fact]
    public void Tokenize_SplitsOnMixedSeparators()
    {
        Assert.Equal(["foo", "bar", "baz", "qux", "quux"], CaseTool.Tokenize("foo_bar-baz.qux/quux"));
    }

    [Fact]
    public void Tokenize_SplitsOnWhitespaceRuns()
    {
        Assert.Equal(["hello", "world"], CaseTool.Tokenize("  hello   world  "));
    }

    [Fact]
    public void Tokenize_LowerToUpperBoundary()
    {
        Assert.Equal(["my", "variable", "name"], CaseTool.Tokenize("myVariableName"));
    }

    [Fact]
    public void Tokenize_DigitLetterBoundaries()
    {
        Assert.Equal(["version", "2", "point", "0"], CaseTool.Tokenize("Version2Point0"));
    }

    [Fact]
    public void Tokenize_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(CaseTool.Tokenize(""));
        Assert.Empty(CaseTool.Tokenize(null));
    }

    [Fact]
    public void Tokenize_UnicodeLetters_KeepWordsIntact()
    {
        Assert.Equal(["café", "world", "日本語"], CaseTool.Tokenize("café World 日本語"));
    }

    private const string Fixture = "hello world example";

    [Fact]
    public void ToCamelCase_Fixture()
    {
        Assert.Equal("helloWorldExample", CaseTool.ToCamelCase(Fixture));
    }

    [Fact]
    public void ToPascalCase_Fixture()
    {
        Assert.Equal("HelloWorldExample", CaseTool.ToPascalCase(Fixture));
    }

    [Fact]
    public void ToSnakeCase_Fixture()
    {
        Assert.Equal("hello_world_example", CaseTool.ToSnakeCase(Fixture));
    }

    [Fact]
    public void ToScreamingSnakeCase_Fixture()
    {
        Assert.Equal("HELLO_WORLD_EXAMPLE", CaseTool.ToScreamingSnakeCase(Fixture));
    }

    [Fact]
    public void ToKebabCase_Fixture()
    {
        Assert.Equal("hello-world-example", CaseTool.ToKebabCase(Fixture));
    }

    [Fact]
    public void ToTrainCase_Fixture()
    {
        Assert.Equal("Hello-World-Example", CaseTool.ToTrainCase(Fixture));
    }

    [Fact]
    public void ToTitleCase_Fixture()
    {
        Assert.Equal("Hello World Example", CaseTool.ToTitleCase(Fixture));
    }

    [Fact]
    public void ToSentenceCase_Fixture()
    {
        Assert.Equal("Hello world example", CaseTool.ToSentenceCase(Fixture));
    }

    [Fact]
    public void ToLowercase_Fixture()
    {
        Assert.Equal("hello world example", CaseTool.ToLowercase("Hello WORLD Example"));
    }

    [Fact]
    public void ToUppercase_Fixture()
    {
        Assert.Equal("HELLO WORLD EXAMPLE", CaseTool.ToUppercase(Fixture));
    }

    [Fact]
    public void ToDotCase_Fixture()
    {
        Assert.Equal("hello.world.example", CaseTool.ToDotCase(Fixture));
    }

    [Fact]
    public void ToPathCase_Fixture()
    {
        Assert.Equal("hello/world/example", CaseTool.ToPathCase(Fixture));
    }

    [Fact]
    public void ConvertAll_ReturnsAllTwelveStylesInOrder()
    {
        var results = CaseTool.ConvertAll(Fixture);

        Assert.Equal(12, results.Count);
        Assert.Equal(
        [
            "camelCase", "PascalCase", "snake_case", "SCREAMING_SNAKE", "kebab-case",
            "Train-Case", "Title Case", "Sentence case", "lowercase", "UPPERCASE",
            "dot.case", "path/case",
        ], results.Select(r => r.Style));
        Assert.Equal("helloWorldExample", results[0].Value);
        Assert.Equal("hello/world/example", results[^1].Value);
    }

    [Fact]
    public void AllConverters_EmptyInput_ReturnEmptyString()
    {
        Assert.Equal("", CaseTool.ToCamelCase(""));
        Assert.Equal("", CaseTool.ToPascalCase(""));
        Assert.Equal("", CaseTool.ToSnakeCase(""));
        Assert.Equal("", CaseTool.ToTitleCase(""));
        Assert.Equal("", CaseTool.ToSentenceCase(""));
    }
}
