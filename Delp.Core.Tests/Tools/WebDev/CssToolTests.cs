using Delp.Core.Tools.WebDev;

namespace Delp.Core.Tests.Tools.WebDev;

public class CssToolTests
{
    [Fact]
    public void Minify_SimpleRule_ProducesSmallerValidOutput()
    {
        const string css = ".a {\n  color: red;\n  margin: 0 auto;\n}\n@media (min-width: 600px) {\n  .b { color: blue; }\n}\n";
        var result = CssTool.Minify(css);

        Assert.Empty(result.Errors);
        Assert.Equal(".a{color:#f00;margin:0 auto}@media(min-width:600px){.b{color:#00f}}", result.Code);
        Assert.True(result.AfterBytes < result.BeforeBytes);
        Assert.True(result.SavingsPercent > 0);
    }

    [Fact]
    public void Minify_MalformedCss_SurfacesErrors()
    {
        var result = CssTool.Minify(".a { color: ; }}}");
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Line", StringComparison.Ordinal));
    }

    [Fact]
    public void Beautify_SimpleRule_PinnedOutput()
    {
        const string css = ".a{color:red;margin:0 auto}.b,.c{color:blue}";
        var expected =
            ".a {\n" +
            "  color: red;\n" +
            "  margin: 0 auto;\n" +
            "}\n" +
            "\n" +
            ".b,\n" +
            ".c {\n" +
            "  color: blue;\n" +
            "}\n";

        Assert.Equal(expected, CssTool.Beautify(css, 2));
    }

    [Fact]
    public void Beautify_StringWithBraceInside_IsUntouched()
    {
        const string css = ".a { content: \"{ not a block }\"; }";
        var result = CssTool.Beautify(css, 2);
        Assert.Contains("content: \"{ not a block }\";", result);
    }

    [Fact]
    public void Beautify_MediaQuery_IndentsNestedRules()
    {
        const string css = "@media (min-width: 600px){.a{color:red}.b{color:blue}}";
        var expected =
            "@media (min-width: 600px) {\n" +
            "  .a {\n" +
            "    color: red;\n" +
            "  }\n" +
            "\n" +
            "  .b {\n" +
            "    color: blue;\n" +
            "  }\n" +
            "}\n";

        Assert.Equal(expected, CssTool.Beautify(css, 2));
    }

    [Fact]
    public void Beautify_PreservesBangComments()
    {
        const string css = "/*! keep me */\n.a{color:red}";
        var result = CssTool.Beautify(css, 2);
        Assert.Contains("/*! keep me */", result);
    }

    [Fact]
    public void SavingsMath_IsConsistentWithByteCounts()
    {
        var result = CssTool.Minify(".a { color: red; }");
        var expectedSavings = Math.Round((1 - (double)result.AfterBytes / result.BeforeBytes) * 100, 1);
        Assert.Equal(expectedSavings, result.SavingsPercent);
    }
}
