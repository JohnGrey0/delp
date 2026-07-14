using Delp.Core.Tools.WebDev;

namespace Delp.Core.Tests.Tools.WebDev;

public class DataUriToolTests
{
    [Fact]
    public void Encode_Decode_RoundTripsBytes()
    {
        byte[] bytes = { 1, 2, 3, 255, 0, 128 };
        var uri = DataUriTool.Encode(bytes, "application/octet-stream");
        Assert.StartsWith("data:application/octet-stream;base64,", uri);

        var parts = DataUriTool.Decode(uri);
        Assert.True(parts.IsBase64);
        Assert.Equal("application/octet-stream", parts.MimeType);
        Assert.Equal(bytes, parts.Data);
    }

    [Fact]
    public void EncodeText_PercentEncodedForm_RoundTrips()
    {
        const string text = "hello world / café";
        var uri = DataUriTool.EncodeText(text, "text/plain");
        Assert.Contains("charset=utf-8,", uri);
        Assert.DoesNotContain(";base64,", uri);

        var parts = DataUriTool.Decode(uri);
        Assert.False(parts.IsBase64);
        Assert.Equal(text, System.Text.Encoding.UTF8.GetString(parts.Data));
    }

    [Fact]
    public void EncodeText_Base64Form_RoundTrips()
    {
        const string text = "some déjà vu 🎉";
        var uri = DataUriTool.EncodeText(text, "text/plain", asBase64: true);
        Assert.Contains(";base64,", uri);

        var parts = DataUriTool.Decode(uri);
        Assert.True(parts.IsBase64);
        Assert.Equal(text, System.Text.Encoding.UTF8.GetString(parts.Data));
    }

    [Theory]
    [InlineData("png", "image/png")]
    [InlineData(".JPG", "image/jpeg")]
    [InlineData("json", "application/json")]
    [InlineData("woff2", "font/woff2")]
    [InlineData("unknownext", "application/octet-stream")]
    public void GuessMime_ReturnsExpectedType(string ext, string expected)
    {
        Assert.Equal(expected, DataUriTool.GuessMime(ext));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a data uri")]
    [InlineData("data:text/plain")]
    [InlineData("data:text/plain;base64,not-valid-base64!!")]
    public void Decode_MalformedInput_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => DataUriTool.Decode(input));
    }
}
