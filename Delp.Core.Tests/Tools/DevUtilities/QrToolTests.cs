using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class QrToolTests
{
    private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    [Fact]
    public void CreatePng_StartsWithPngMagicBytes()
    {
        var bytes = QrTool.CreatePng("https://example.com", 4, QrEccLevel.M);
        Assert.True(bytes.Length > PngMagic.Length);
        Assert.Equal(PngMagic, bytes[..PngMagic.Length]);
    }

    [Fact]
    public void CreatePng_EmptyContent_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => QrTool.CreatePng("", 4, QrEccLevel.M));
    }

    [Fact]
    public void CreatePng_InvalidPixelsPerModule_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => QrTool.CreatePng("hello", 0, QrEccLevel.M));
    }

    [Fact]
    public void CreatePng_DifferentEccLevels_ProduceIncreasinglyLargerImages()
    {
        const string content =
            "https://example.com/this-is-a-reasonably-long-url-to-force-multiple-qr-versions-depending-on-ecc-level";

        var l = QrTool.CreatePng(content, 4, QrEccLevel.L);
        var m = QrTool.CreatePng(content, 4, QrEccLevel.M);
        var q = QrTool.CreatePng(content, 4, QrEccLevel.Q);
        var h = QrTool.CreatePng(content, 4, QrEccLevel.H);

        Assert.True(l.Length < m.Length);
        Assert.True(m.Length < q.Length);
        Assert.True(q.Length < h.Length);
    }

    [Fact]
    public void WifiPayload_Wpa_ProducesExpectedShape()
    {
        var payload = QrTool.WifiPayload("HomeNet", "secret123", WifiAuth.Wpa, hidden: false);
        Assert.Equal("WIFI:T:WPA;S:HomeNet;P:secret123;;", payload);
    }

    [Fact]
    public void WifiPayload_Hidden_AddsHiddenFlag()
    {
        var payload = QrTool.WifiPayload("HiddenNet", "secret123", WifiAuth.Wep, hidden: true);
        Assert.Equal("WIFI:T:WEP;S:HiddenNet;P:secret123;H:true;;", payload);
    }

    [Fact]
    public void WifiPayload_NoAuth_OmitsPasswordField()
    {
        var payload = QrTool.WifiPayload("OpenNet", null, WifiAuth.None, hidden: false);
        Assert.Equal("WIFI:T:nopass;S:OpenNet;;", payload);
    }

    [Theory]
    [InlineData(';', "\\;")]
    [InlineData(',', "\\,")]
    [InlineData(':', "\\:")]
    [InlineData('"', "\\\"")]
    [InlineData('\\', "\\\\")]
    public void WifiPayload_EscapesSpecialCharactersInSsid(char special, string expectedEscaped)
    {
        var ssid = $"A{special}B";
        var payload = QrTool.WifiPayload(ssid, "secret", WifiAuth.Wpa, hidden: false);
        Assert.Contains($"S:A{expectedEscaped}B;", payload);
    }

    [Fact]
    public void WifiPayload_EmptySsid_Throws()
    {
        Assert.Throws<FormatException>(() => QrTool.WifiPayload("", "pw", WifiAuth.Wpa, false));
    }

    [Fact]
    public void WifiPayload_WpaWithoutPassword_Throws()
    {
        Assert.Throws<FormatException>(() => QrTool.WifiPayload("Net", null, WifiAuth.Wpa, false));
    }
}
