using Delp.Core.Tools.WebDev;

namespace Delp.Core.Tests.Tools.WebDev;

public class CurlToolTests
{
    [Fact]
    public void Parse_SimpleGet_DefaultsMethodAndHasNoBody()
    {
        var r = CurlTool.Parse("curl https://example.com/api");
        Assert.Equal("GET", r.Method);
        Assert.Equal("https://example.com/api", r.Url);
        Assert.Equal(CurlBodyKind.None, r.BodyKind);
        Assert.Empty(r.Warnings);
    }

    [Fact]
    public void Parse_QuotingAndLineContinuations_ProduceExpectedFlags()
    {
        var r = CurlTool.Parse("curl \\\n  -X POST 'https://example.com/a b' \\\n  -d 'x=1'");
        Assert.Equal("POST", r.Method);
        Assert.Equal("https://example.com/a b", r.Url);
        Assert.Equal("x=1", r.Body);
    }

    [Fact]
    public void Parse_MultipleHeaders_PreservesOrderAndAllEntries()
    {
        var r = CurlTool.Parse("curl -H 'Accept: application/json' -H 'X-Api-Key: abc123' https://example.com");
        Assert.Equal(2, r.Headers.Count);
        Assert.Equal("Accept", r.Headers[0].Name);
        Assert.Equal("application/json", r.Headers[0].Value);
        Assert.Equal("X-Api-Key", r.Headers[1].Name);
        Assert.Equal("abc123", r.Headers[1].Value);
    }

    [Fact]
    public void Parse_DataUrlencode_EncodesValueButKeepsNameLiteral()
    {
        var r = CurlTool.Parse("curl --data-urlencode 'q=hello world&foo' https://example.com/search");
        Assert.Equal(CurlBodyKind.UrlEncodedForm, r.BodyKind);
        Assert.Equal("q=hello%20world%26foo", r.Body);
        Assert.Equal("POST", r.Method);
    }

    [Fact]
    public void Parse_DataUrlencode_BareEqualsForm_EncodesWholeContentWithoutName()
    {
        var r = CurlTool.Parse("curl --data-urlencode '=a b' https://example.com");
        Assert.Equal("a%20b", r.Body);
    }

    [Fact]
    public void Parse_UserFlag_SplitsOnFirstColonOnly()
    {
        var r = CurlTool.Parse("curl -u 'alice:p@ss:word' https://example.com");
        Assert.NotNull(r.UserAuth);
        Assert.Equal("alice", r.UserAuth!.Value.User);
        Assert.Equal("p@ss:word", r.UserAuth!.Value.Password);
    }

    [Theory]
    [InlineData(CurlTarget.CSharp, "AuthenticationHeaderValue(\"Basic\"")]
    [InlineData(CurlTarget.Python, "auth=(\"alice\", \"secret\")")]
    [InlineData(CurlTarget.JavaScript, "\"Authorization\": \"Basic \" + btoa(\"alice:secret\")")]
    [InlineData(CurlTarget.PowerShell, "Authentication = \"Basic\"")]
    [InlineData(CurlTarget.Go, "req.SetBasicAuth(\"alice\", \"secret\")")]
    public void Generate_UserAuth_UsesTargetsNativeBasicAuthMechanism(CurlTarget target, string expectedFragment)
    {
        var r = CurlTool.Parse("curl -u alice:secret https://example.com");
        var code = CurlTool.Generate(r, target);
        Assert.Contains(expectedFragment, code);
    }

    [Fact]
    public void Parse_GetFlag_MovesDataToQueryStringAndClearsBody()
    {
        var r = CurlTool.Parse("curl -G -d 'a=1' -d 'b=2' https://example.com/search");
        Assert.Equal("GET", r.Method);
        Assert.Equal(CurlBodyKind.None, r.BodyKind);
        Assert.Equal("https://example.com/search?a=1&b=2", r.Url);
    }

    [Fact]
    public void Parse_GetFlag_AppendsToExistingQueryString()
    {
        var r = CurlTool.Parse("curl -G -d 'b=2' 'https://example.com/search?a=1'");
        Assert.Equal("https://example.com/search?a=1&b=2", r.Url);
    }

    [Fact]
    public void Parse_JsonShorthand_SetsBodyKindAndContentTypeAndAcceptHeaders()
    {
        var r = CurlTool.Parse("curl --json '{\"a\":1}' https://example.com");
        Assert.Equal(CurlBodyKind.Json, r.BodyKind);
        Assert.Equal("POST", r.Method);
        Assert.Contains(r.Headers, h => h.Name == "Content-Type" && h.Value == "application/json");
        Assert.Contains(r.Headers, h => h.Name == "Accept" && h.Value == "application/json");
        Assert.Contains("\"a\": 1", r.Body);
    }

    [Fact]
    public void Parse_JsonShorthand_DoesNotDuplicateUserSuppliedContentType()
    {
        var r = CurlTool.Parse("curl -H 'Content-Type: application/json; charset=utf-8' --json '{}' https://example.com");
        Assert.Single(r.Headers, h => h.Name == "Content-Type");
    }

    [Fact]
    public void Parse_UnknownFlag_NeverThrows_AddsWarning()
    {
        var r = CurlTool.Parse("curl --totally-not-a-flag https://example.com");
        Assert.Contains(r.Warnings, w => w.Contains("--totally-not-a-flag"));
    }

    [Fact]
    public void ToCurl_RoundTrip_PreservesMethodUrlHeadersAndBody()
    {
        var original = CurlTool.Parse(
            "curl -X PUT -H 'Accept: application/json' -H 'X-Api-Key: k1' " +
            "-u bob:hunter2 -k -L --compressed --json '{\"a\":1,\"b\":\"x\"}' https://example.com/thing");

        var curlLine = CurlTool.ToCurl(original);
        var reparsed = CurlTool.Parse(curlLine);

        Assert.Equal(original.Method, reparsed.Method);
        Assert.Equal(original.Url, reparsed.Url);
        Assert.Equal(original.BodyKind, reparsed.BodyKind);
        Assert.Equal(original.Body, reparsed.Body);
        Assert.Equal(original.Headers.Count, reparsed.Headers.Count);
        Assert.Equal(original.UserAuth, reparsed.UserAuth);
        Assert.Equal(original.Insecure, reparsed.Insecure);
        Assert.Equal(original.FollowRedirects, reparsed.FollowRedirects);
        Assert.Equal(original.Compressed, reparsed.Compressed);
        Assert.Empty(reparsed.Warnings);
    }

    [Fact]
    public void ToCurl_RoundTrip_UrlEncodedFormBody()
    {
        var original = CurlTool.Parse("curl -d 'a=1&b=2' https://example.com/form");
        var reparsed = CurlTool.Parse(CurlTool.ToCurl(original));

        Assert.Equal(original.BodyKind, reparsed.BodyKind);
        Assert.Equal(original.Body, reparsed.Body);
        Assert.Equal(CurlBodyKind.UrlEncodedForm, reparsed.BodyKind);
    }

    [Fact]
    public void ToCurl_RoundTrip_MultipartForm()
    {
        var original = CurlTool.Parse("curl -F 'name=alice' -F 'file=@photo.png' https://example.com/upload");
        var reparsed = CurlTool.Parse(CurlTool.ToCurl(original));

        Assert.Equal(CurlBodyKind.Multipart, reparsed.BodyKind);
        Assert.Equal(original.FormParts.Count, reparsed.FormParts.Count);
        Assert.Equal(original.FormParts[1].IsFile, reparsed.FormParts[1].IsFile);
        Assert.Equal(original.FormParts[1].Value, reparsed.FormParts[1].Value);
    }

    [Theory]
    [InlineData(CurlTarget.CSharp, "HttpClient")]
    [InlineData(CurlTarget.Python, "requests.request(")]
    [InlineData(CurlTarget.JavaScript, "fetch(")]
    [InlineData(CurlTarget.PowerShell, "Invoke-RestMethod")]
    [InlineData(CurlTarget.Go, "http.NewRequest(")]
    public void Generate_EachTarget_ContainsIdiomaticFragment(CurlTarget target, string expectedFragment)
    {
        var r = CurlTool.Parse("curl -X POST -H 'Accept: application/json' -d 'a=1&b=2' https://example.com/api");
        var code = CurlTool.Generate(r, target);
        Assert.Contains(expectedFragment, code);
    }

    [Fact]
    public void Generate_JsonBody_PrettyPrintsAndProducesValidLookingPayload()
    {
        var r = CurlTool.Parse("curl --json '{\"a\":1,\"nested\":{\"b\":2}}' https://example.com");
        var code = CurlTool.Generate(r, CurlTarget.Python);
        Assert.Contains("json.loads", code);
        Assert.Contains("\"nested\"", code);
    }

    [Fact]
    public void Parse_HeadFlag_SetsMethodToHead()
    {
        var r = CurlTool.Parse("curl -I https://example.com");
        Assert.Equal("HEAD", r.Method);
    }

    [Fact]
    public void Parse_ExplicitMethodOverridesEverythingElse()
    {
        var r = CurlTool.Parse("curl -X DELETE -d 'a=1' https://example.com");
        Assert.Equal("DELETE", r.Method);
    }

    [Fact]
    public void Parse_EmptyCommand_ReturnsEmptyUrlAndWarns()
    {
        var r = CurlTool.Parse("");
        Assert.Equal("", r.Url);
        Assert.Contains(r.Warnings, w => w.Contains("No URL"));
    }

    [Fact]
    public void Parse_InsecureAndLocationFlags_AreCaptured()
    {
        var r = CurlTool.Parse("curl -k -L https://example.com");
        Assert.True(r.Insecure);
        Assert.True(r.FollowRedirects);
    }

    [Fact]
    public void Parse_RawJsonLookingBody_WithoutJsonFlag_IsClassifiedAsRaw()
    {
        var r = CurlTool.Parse("curl -d '{\"a\":1}' https://example.com");
        Assert.Equal(CurlBodyKind.Raw, r.BodyKind);
    }
}
