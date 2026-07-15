namespace Delp.Core.Tools.DevUtilities;

/// <summary>The parsed shape of one user-agent string.</summary>
public sealed record UserAgentResult(
    string Browser,
    string? BrowserVersion,
    string Engine,
    string? EngineVersion,
    string Os,
    string? OsVersion,
    string DeviceType,
    bool IsBot,
    string? BotName);

/// <summary>
/// Parses a browser user-agent string into browser, engine, OS, and device-type using the
/// curated signatures in <see cref="UserAgentData"/>. Never throws for unrecognized input —
/// unmatched fields fall back to "Unknown" — only for empty/whitespace input.
/// </summary>
public static class UserAgentTool
{
    public static UserAgentResult Parse(string? userAgent)
    {
        var ua = userAgent ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ua))
            throw new FormatException("Enter a user agent string.");

        var os = DetectOs(ua);

        foreach (var bot in UserAgentData.Bots)
        {
            var m = bot.Pattern.Match(ua);
            if (m.Success)
            {
                return new UserAgentResult(
                    bot.Label, Version(m), "—", null,
                    os?.Name ?? "Unknown", os?.Version, "Bot", true, bot.Label);
            }
        }

        var browser = Detect(UserAgentData.Browsers, ua);
        var engine = Detect(UserAgentData.Engines, ua);
        var deviceType = DetectDeviceType(ua, os);

        return new UserAgentResult(
            browser?.Label ?? "Unknown",
            browser is null ? null : Version(browser.Value.Match),
            engine?.Label ?? "Unknown",
            engine is null ? null : Version(engine.Value.Match),
            os?.Name ?? "Unknown",
            os?.Version,
            deviceType,
            false,
            null);
    }

    private static (string Label, System.Text.RegularExpressions.Match Match)? Detect(
        IReadOnlyList<UaSignature> signatures, string ua)
    {
        foreach (var sig in signatures)
        {
            var m = sig.Pattern.Match(ua);
            if (m.Success)
                return (sig.Label, m);
        }
        return null;
    }

    private static (string Name, string? Version)? DetectOs(string ua)
    {
        foreach (var os in UserAgentData.OperatingSystems)
        {
            var m = os.Pattern.Match(ua);
            if (m.Success)
                return (os.Name, os.Version(m));
        }
        return null;
    }

    private static string DetectDeviceType(string ua, (string Name, string? Version)? os)
    {
        if (UserAgentData.PlayStationToken.IsMatch(ua) ||
            UserAgentData.XboxToken.IsMatch(ua) ||
            UserAgentData.NintendoToken.IsMatch(ua))
            return "Console";

        var isTablet = UserAgentData.TabletToken.IsMatch(ua);
        var isMobile = UserAgentData.MobileToken.IsMatch(ua);

        if (os?.Name is "iOS" or "Android")
            return isTablet ? "Tablet" : isMobile ? "Mobile" : "Tablet";

        if (isTablet) return "Tablet";
        if (isMobile) return "Mobile";
        return "Desktop";
    }

    private static string? Version(System.Text.RegularExpressions.Match m) =>
        m.Groups.Count > 1 && m.Groups[1].Success ? m.Groups[1].Value : null;
}
