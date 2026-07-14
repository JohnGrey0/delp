using Delp.Core.Tools.WebDev;

namespace Delp.Core.Tests.Tools.WebDev;

public class HtmlToolTests
{
    private const string Sample = "<!-- comment -->\n<div>\n  <p>Hello   World</p>\n  <pre>  keep   me  </pre>\n</div>\n";

    [Fact]
    public void Minify_RemoveCommentsOn_StripsComments()
    {
        var result = HtmlTool.Minify(Sample, new HtmlMinifyOptions(RemoveComments: true, CollapseWhitespace: true));
        Assert.Empty(result.Errors);
        Assert.DoesNotContain("<!--", result.Code);
    }

    [Fact]
    public void Minify_RemoveCommentsOff_KeepsComments()
    {
        var result = HtmlTool.Minify(Sample, new HtmlMinifyOptions(RemoveComments: false, CollapseWhitespace: false));
        Assert.Empty(result.Errors);
        Assert.Contains("<!-- comment -->", result.Code);
    }

    [Fact]
    public void Minify_CollapseWhitespaceOn_CollapsesRuns()
    {
        var result = HtmlTool.Minify(Sample, new HtmlMinifyOptions(RemoveComments: true, CollapseWhitespace: true));
        Assert.DoesNotContain("Hello   World", result.Code);
        Assert.Contains("Hello World", result.Code);
    }

    [Fact]
    public void Minify_PreContent_IsPreservedRegardlessOfCollapse()
    {
        var result = HtmlTool.Minify(Sample, new HtmlMinifyOptions(RemoveComments: true, CollapseWhitespace: true));
        Assert.Contains("<pre>  keep   me  </pre>", result.Code);
    }

    [Fact]
    public void Minify_SavingsMath_BeforeAfterBytesMatchOutput()
    {
        var result = HtmlTool.Minify(Sample, new HtmlMinifyOptions(RemoveComments: true, CollapseWhitespace: true));
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(Sample), result.BeforeBytes);
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(result.Code ?? ""), result.AfterBytes);
        Assert.True(result.AfterBytes < result.BeforeBytes);
    }
}
