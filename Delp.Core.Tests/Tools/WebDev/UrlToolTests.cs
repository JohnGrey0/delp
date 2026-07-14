using Delp.Core.Tools.WebDev;

namespace Delp.Core.Tests.Tools.WebDev;

public class UrlToolTests
{
    [Fact]
    public void Parse_Build_RoundTrips()
    {
        const string url = "https://user:pass@example.com:8443/a/b?x=1&y=2#frag";
        var parts = UrlTool.Parse(url);

        Assert.Equal("https", parts.Scheme);
        Assert.Equal("example.com", parts.Host);
        Assert.Equal(8443, parts.Port);
        Assert.Equal("/a/b", parts.Path);
        Assert.Equal("frag", parts.Fragment);
        Assert.Equal("user:pass", parts.UserInfo);

        var rebuilt = UrlTool.Build(parts);
        Assert.Equal(url, rebuilt);
    }

    [Fact]
    public void Parse_DuplicateQueryKeys_OrderAndDuplicatesPreserved()
    {
        var parts = UrlTool.Parse("https://example.com/?a=1&b=2&a=3");

        Assert.Equal(3, parts.Query.Count);
        Assert.Equal(new QueryParam("a", "1"), parts.Query[0]);
        Assert.Equal(new QueryParam("b", "2"), parts.Query[1]);
        Assert.Equal(new QueryParam("a", "3"), parts.Query[2]);
    }

    [Fact]
    public void Parse_IdnHost_UnicodeInput_ProducesAsciiAndUnicodeForms()
    {
        var parts = UrlTool.Parse("https://münchen.de/");
        Assert.Equal("xn--mnchen-3ya.de", parts.Host);
        Assert.Equal("münchen.de", parts.HostUnicode);
    }

    [Fact]
    public void Parse_IdnHost_AsciiInput_ProducesUnicodeForm()
    {
        var parts = UrlTool.Parse("https://xn--mnchen-3ya.de/");
        Assert.Equal("xn--mnchen-3ya.de", parts.Host);
        Assert.Equal("münchen.de", parts.HostUnicode);
    }

    [Theory]
    [InlineData("http://example.com:80/", null)]
    [InlineData("https://example.com:443/", null)]
    [InlineData("https://example.com:8080/", 8080)]
    public void Parse_DefaultPort_IsElided(string url, int? expectedPort)
    {
        var parts = UrlTool.Parse(url);
        Assert.Equal(expectedPort, parts.Port);
    }

    [Fact]
    public void Parse_FlagAndEmptyQueryParams_BothPreserved()
    {
        var parts = UrlTool.Parse("https://example.com/?a&b=");

        Assert.Equal(2, parts.Query.Count);
        Assert.Equal("a", parts.Query[0].Key);
        Assert.Equal("", parts.Query[0].Value);
        Assert.Equal("b", parts.Query[1].Key);
        Assert.Equal("", parts.Query[1].Value);
    }

    [Fact]
    public void Parse_NoScheme_AssumesHttps()
    {
        var parts = UrlTool.Parse("example.com/path");
        Assert.Equal("https", parts.Scheme);
        Assert.Equal("example.com", parts.Host);
        Assert.Equal("/path", parts.Path);
    }

    [Fact]
    public void Parse_FragmentContainingQuestionMark_KeptWhole()
    {
        var parts = UrlTool.Parse("https://example.com/x?y=1#sec?ion=2");
        Assert.Equal("sec?ion=2", parts.Fragment);
        Assert.Single(parts.Query);
        Assert.Equal(new QueryParam("y", "1"), parts.Query[0]);
    }

    [Fact]
    public void Parse_EmptyInput_Throws()
    {
        Assert.Throws<FormatException>(() => UrlTool.Parse(""));
    }

    [Fact]
    public void Parse_QueryValuesArePercentDecoded()
    {
        var parts = UrlTool.Parse("https://example.com/?q=hello%20world%2Fx");
        Assert.Equal("hello world/x", parts.Query[0].Value);
    }
}
