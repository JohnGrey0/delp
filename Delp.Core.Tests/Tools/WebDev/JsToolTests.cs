using Delp.Core.Tools.WebDev;

namespace Delp.Core.Tests.Tools.WebDev;

public class JsToolTests
{
    [Fact]
    public void Minify_BasicFunction_IsSmallerAndValid()
    {
        const string js = "function add(a, b) {\n  return a + b;\n}\n";
        var result = JsTool.Minify(js);

        Assert.Empty(result.Errors);
        Assert.NotNull(result.Code);
        Assert.Equal("function add(n,t){return n+t}", result.Code);
        Assert.True(result.AfterBytes < result.BeforeBytes);
    }

    [Fact]
    public void Minify_MalformedJs_SurfacesLineInfoErrors()
    {
        var result = JsTool.Minify("function( {{{ ");
        Assert.NotEmpty(result.Errors);
        Assert.All(result.Errors, e => Assert.Contains("Line", e, StringComparison.Ordinal));
    }

    [Fact]
    public void Minify_UnicodeStringLiteral_Survives()
    {
        var result = JsTool.Minify("var s = 'héllo 世界 🚀';");
        Assert.Empty(result.Errors);
        Assert.Contains("héllo 世界 🚀", result.Code);
    }

    [Fact]
    public void Minify_EmptyInput_ProducesEmptyOutputNoErrors()
    {
        var result = JsTool.Minify("");
        Assert.Empty(result.Errors);
        Assert.Equal(0, result.BeforeBytes);
        Assert.Equal(0, result.AfterBytes);
    }
}
