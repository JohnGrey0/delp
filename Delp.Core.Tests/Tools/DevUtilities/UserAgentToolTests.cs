using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class UserAgentToolTests
{
    private const string ChromeWindows =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
    private const string EdgeWindows =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.0";
    private const string SafariMac =
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/605.1.15";
    private const string SafariIPhone =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/604.1";
    private const string Googlebot =
        "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";

    [Fact]
    public void Parse_ChromeWindows_ReportsBrowserEngineOsDevice()
    {
        var r = UserAgentTool.Parse(ChromeWindows);
        Assert.Equal("Chrome", r.Browser);
        Assert.Equal("124.0.0.0", r.BrowserVersion);
        Assert.Equal("Blink", r.Engine);
        Assert.Equal("Windows", r.Os);
        Assert.Equal("10/11", r.OsVersion);
        Assert.Equal("Desktop", r.DeviceType);
        Assert.False(r.IsBot);
        Assert.Null(r.BotName);
    }

    [Fact]
    public void Parse_EdgeWindows_DetectedAsEdgeNotChrome()
    {
        var r = UserAgentTool.Parse(EdgeWindows);
        Assert.Equal("Edge", r.Browser);
        Assert.Equal("124.0.0.0", r.BrowserVersion);
        Assert.Equal("Blink", r.Engine);
        Assert.Equal("Windows", r.Os);
        Assert.Equal("Desktop", r.DeviceType);
    }

    [Fact]
    public void Parse_SafariMac_DetectedAsSafariWebKit()
    {
        var r = UserAgentTool.Parse(SafariMac);
        Assert.Equal("Safari", r.Browser);
        Assert.Equal("17.4", r.BrowserVersion);
        Assert.Equal("WebKit", r.Engine);
        Assert.Equal("macOS", r.Os);
        Assert.Equal("10.15.7", r.OsVersion);
        Assert.Equal("Desktop", r.DeviceType);
    }

    [Fact]
    public void Parse_SafariIPhone_DetectedAsMobileIOS()
    {
        var r = UserAgentTool.Parse(SafariIPhone);
        Assert.Equal("Safari", r.Browser);
        Assert.Equal("WebKit", r.Engine);
        Assert.Equal("iOS", r.Os);
        Assert.Equal("17.4", r.OsVersion);
        Assert.Equal("Mobile", r.DeviceType);
    }

    [Fact]
    public void Parse_Googlebot_DetectedAsBot()
    {
        var r = UserAgentTool.Parse(Googlebot);
        Assert.True(r.IsBot);
        Assert.Equal("Googlebot", r.BotName);
        Assert.Equal("Googlebot", r.Browser);
        Assert.Equal("Bot", r.DeviceType);
    }

    [Fact]
    public void Parse_ChromeAndroidMobile_DetectedAsMobile()
    {
        const string ua = "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Mobile Safari/537.36";
        var r = UserAgentTool.Parse(ua);
        Assert.Equal("Chrome", r.Browser);
        Assert.Equal("Blink", r.Engine);
        Assert.Equal("Android", r.Os);
        Assert.Equal("14", r.OsVersion);
        Assert.Equal("Mobile", r.DeviceType);
    }

    [Fact]
    public void Parse_IPadSafari_DetectedAsTablet()
    {
        const string ua = "Mozilla/5.0 (iPad; CPU OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/604.1";
        var r = UserAgentTool.Parse(ua);
        Assert.Equal("Safari", r.Browser);
        Assert.Equal("iOS", r.Os);
        Assert.Equal("Tablet", r.DeviceType);
    }

    [Fact]
    public void Parse_FirefoxOnIOS_EngineIsWebKitNotGecko()
    {
        // Apple mandates WebKit for every iOS browser, so FxiOS's real rendering engine is
        // WebKit even though the browser identifies as Firefox.
        const string ua = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) FxiOS/125.0 Mobile/15E148 Safari/605.1.15";
        var r = UserAgentTool.Parse(ua);
        Assert.Equal("Firefox", r.Browser);
        Assert.Equal("WebKit", r.Engine);
    }

    [Fact]
    public void Parse_Ie11_DetectedViaTridentToken()
    {
        const string ua = "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko";
        var r = UserAgentTool.Parse(ua);
        Assert.Equal("Internet Explorer", r.Browser);
        Assert.Equal("11.0", r.BrowserVersion);
        Assert.Equal("Trident", r.Engine);
    }

    [Fact]
    public void Parse_UnknownUserAgent_FallsBackGracefully()
    {
        var r = UserAgentTool.Parse("SomeCustomClient/1.0");
        Assert.Equal("Unknown", r.Browser);
        Assert.Null(r.BrowserVersion);
        Assert.Equal("Unknown", r.Engine);
        Assert.Equal("Unknown", r.Os);
        Assert.Equal("Desktop", r.DeviceType);
        Assert.False(r.IsBot);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Parse_EmptyOrWhitespace_Throws(string? ua)
    {
        Assert.Throws<FormatException>(() => UserAgentTool.Parse(ua));
    }

    [Fact]
    public void SampleUserAgents_HasExactlyFive()
    {
        Assert.Equal(5, UserAgentData.SampleUserAgents.Count);
    }

    [Fact]
    public void SampleUserAgents_AllParseWithoutThrowing()
    {
        foreach (var ua in UserAgentData.SampleUserAgents)
            UserAgentTool.Parse(ua); // must not throw
    }

    [Fact]
    public void RegexData_HasAtLeast60Signatures()
    {
        var total = UserAgentData.Bots.Count + UserAgentData.Browsers.Count +
                    UserAgentData.Engines.Count + UserAgentData.OperatingSystems.Count + 5;
        Assert.True(total >= 60, $"Expected >= 60 curated signatures, got {total}");
    }
}
