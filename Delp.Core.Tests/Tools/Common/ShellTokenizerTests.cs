using Delp.Core.Tools.Common;

namespace Delp.Core.Tests.Tools.Common;

public class ShellTokenizerTests
{
    [Fact]
    public void Tokenize_SplitsOnWhitespace()
    {
        Assert.Equal(["curl", "-X", "GET", "https://example.com"],
            ShellTokenizer.Tokenize("curl  -X   GET https://example.com"));
    }

    [Fact]
    public void Tokenize_EmptyInput_ReturnsNoTokens()
    {
        Assert.Empty(ShellTokenizer.Tokenize(""));
        Assert.Empty(ShellTokenizer.Tokenize("   \t  "));
    }

    [Fact]
    public void Tokenize_SingleQuotes_AreLiteralNoEscapes()
    {
        var tokens = ShellTokenizer.Tokenize(@"curl -d 'a\nb ""quoted""'");
        Assert.Equal(["curl", "-d", @"a\nb ""quoted"""], tokens);
    }

    [Fact]
    public void Tokenize_EmptySingleQuotes_ProducesEmptyToken()
    {
        Assert.Equal(["-d", ""], ShellTokenizer.Tokenize("-d ''"));
    }

    [Fact]
    public void Tokenize_DoubleQuotes_HonorBackslashEscapes()
    {
        var tokens = ShellTokenizer.Tokenize(@"-H ""X-Name: say \""hi\"" $VAR""");
        Assert.Equal(["-H", "X-Name: say \"hi\" $VAR"], tokens);
    }

    [Fact]
    public void Tokenize_DoubleQuotes_BackslashBeforeOrdinaryChar_IsLiteral()
    {
        // Inside double quotes, a backslash only escapes " \ $ ` — anything else stays as-is.
        var tokens = ShellTokenizer.Tokenize(@"""C:\path\to\file""");
        Assert.Equal([@"C:\path\to\file"], tokens);
    }

    [Fact]
    public void Tokenize_UnquotedBackslash_EscapesNextChar()
    {
        Assert.Equal(["a b"], ShellTokenizer.Tokenize(@"a\ b"));
    }

    [Fact]
    public void Tokenize_BackslashNewlineContinuation_IsSpliced()
    {
        var tokens = ShellTokenizer.Tokenize("curl \\\n  -H 'X: 1' \\\n  https://example.com");
        Assert.Equal(["curl", "-H", "X: 1", "https://example.com"], tokens);
    }

    [Fact]
    public void Tokenize_CaretNewlineContinuation_IsSpliced()
    {
        var tokens = ShellTokenizer.Tokenize("curl ^\r\n  -H \"X: 1\" ^\r\n  https://example.com");
        Assert.Equal(["curl", "-H", "X: 1", "https://example.com"], tokens);
    }

    [Fact]
    public void Tokenize_UnterminatedQuote_DoesNotThrow_RunsToEndOfInput()
    {
        var tokens = ShellTokenizer.Tokenize("-d 'unterminated");
        Assert.Equal(["-d", "unterminated"], tokens);
    }

    [Fact]
    public void Tokenize_AdjacentQuotedAndUnquotedSegments_JoinIntoOneToken()
    {
        Assert.Equal(["foobar"], ShellTokenizer.Tokenize("foo'bar'"));
        Assert.Equal(["foobar"], ShellTokenizer.Tokenize("'foo'bar"));
    }

    [Fact]
    public void Tokenize_AnsiCQuoting_DecodesCommonBackslashEscapes()
    {
        var tokens = ShellTokenizer.Tokenize(@"-H $'X-Name: line1\nline2\ttabbed'");
        Assert.Equal(["-H", "X-Name: line1\nline2\ttabbed"], tokens);
    }

    [Fact]
    public void Tokenize_AnsiCQuoting_DecodesHexEscape()
    {
        var tokens = ShellTokenizer.Tokenize(@"$'\x41\x42'");
        Assert.Equal(["AB"], tokens);
    }

    [Fact]
    public void Tokenize_AnsiCQuoting_UnknownEscape_PassesThroughLiterally()
    {
        // Never corrupt silently in a way that drops information — an escape this tokenizer
        // doesn't know keeps both the backslash and the character.
        var tokens = ShellTokenizer.Tokenize(@"$'\q'");
        Assert.Equal(["\\q"], tokens);
    }

    [Fact]
    public void Tokenize_AnsiCQuoting_EscapedSingleQuoteDoesNotEndTheString()
    {
        var tokens = ShellTokenizer.Tokenize(@"$'it\'s here' rest");
        Assert.Equal(["it's here", "rest"], tokens);
    }

    [Fact]
    public void Tokenize_AnsiCQuoting_Unterminated_DoesNotThrow_RunsToEndOfInput()
    {
        var tokens = ShellTokenizer.Tokenize(@"$'unterminated\n");
        Assert.Equal(["unterminated\n"], tokens);
    }

    [Fact]
    public void Tokenize_DollarNotFollowedByQuote_IsOrdinaryCharacter()
    {
        Assert.Equal(["$VAR", "$(cmd)"], ShellTokenizer.Tokenize("$VAR $(cmd)"));
    }
}
