using Delp.Core.Tools.Encoding;

namespace Delp.Core.Tests.Tools.Encoding;

public class JwtToolTests
{
    private static string Base64Url(string json)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string MakeToken(string headerJson, string payloadJson, string signature = "sig") =>
        $"{Base64Url(headerJson)}.{Base64Url(payloadJson)}.{Base64Url(signature)}";

    [Fact]
    public void Decode_KnownToken_ProducesExpectedHeaderPayloadAndClaims()
    {
        const string header = "{\"alg\":\"HS256\",\"typ\":\"JWT\"}";
        const string payload = "{\"sub\":\"1234567890\",\"name\":\"John Doe\"}";
        var token = MakeToken(header, payload);

        var result = JwtTool.Decode(token);

        Assert.Contains("\"alg\"", result.HeaderJson);
        Assert.Contains("HS256", result.HeaderJson);
        Assert.Contains("John Doe", result.PayloadJson);
        Assert.Equal(2, result.Claims.Count);
        Assert.Contains(result.Claims, c => c.Name == "sub" && c.Value == "1234567890");
        Assert.Contains(result.Claims, c => c.Name == "name" && c.Value == "John Doe");
        Assert.NotEmpty(result.SignatureB64);
    }

    [Fact]
    public void Decode_StripsBearerPrefixAndWhitespace()
    {
        var token = MakeToken("{\"alg\":\"none\"}", "{\"a\":1}");
        var result = JwtTool.Decode($"  Bearer {token}  ");
        Assert.Contains("\"a\"", result.PayloadJson);
    }

    [Fact]
    public void Decode_TwoPartToken_IsUnsecuredWithEmptySignature()
    {
        var headerB64 = Base64Url("{\"alg\":\"none\"}");
        var payloadB64 = Base64Url("{\"x\":1}");
        var result = JwtTool.Decode($"{headerB64}.{payloadB64}");
        Assert.Equal("", result.SignatureB64);
    }

    [Fact]
    public void Decode_ExpInPast_NoteSaysExpired()
    {
        var pastEpoch = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds();
        var token = MakeToken("{\"alg\":\"none\"}", $"{{\"exp\":{pastEpoch}}}");

        var result = JwtTool.Decode(token);

        var expClaim = Assert.Single(result.Claims);
        Assert.Equal("exp", expClaim.Name);
        Assert.Contains("expired", expClaim.Note);
    }

    [Fact]
    public void Decode_ExpInFuture_NoteSaysExpiresIn()
    {
        var futureEpoch = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds();
        var token = MakeToken("{\"alg\":\"none\"}", $"{{\"exp\":{futureEpoch}}}");

        var result = JwtTool.Decode(token);

        var expClaim = Assert.Single(result.Claims);
        Assert.Contains("expires in", expClaim.Note);
    }

    [Fact]
    public void Decode_WrongPartCount_ThrowsClearError()
    {
        Assert.Throws<FormatException>(() => JwtTool.Decode("onlyonepart"));
        Assert.Throws<FormatException>(() => JwtTool.Decode("a.b.c.d"));
    }

    [Fact]
    public void Decode_InvalidBase64Part_ThrowsNamingPart()
    {
        var ex = Assert.Throws<FormatException>(() => JwtTool.Decode("not!valid!base64.alsoNot!valid.sig"));
        Assert.Contains("header", ex.Message);
    }

    [Fact]
    public void Decode_NonJsonPayload_ShowsRawString()
    {
        var headerB64 = Base64Url("{\"alg\":\"none\"}");
        var payloadB64 = Base64Url("not json at all");
        var result = JwtTool.Decode($"{headerB64}.{payloadB64}.sig");
        Assert.Equal("not json at all", result.PayloadJson);
        Assert.Empty(result.Claims);
    }
}
