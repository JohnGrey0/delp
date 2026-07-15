using System.Text.RegularExpressions;

namespace Delp.Core.Tools.DevUtilities;

/// <summary>One labeled pattern: matches a token in a UA string, optionally capturing a version in group 1.</summary>
public sealed record UaSignature(Regex Pattern, string Label);

/// <summary>One OS pattern, plus how to turn its match into a display version string.</summary>
public sealed record UaOsSignature(Regex Pattern, string Name, Func<Match, string?> Version);

/// <summary>
/// Curated regex signatures for user-agent parsing: bots/crawlers, browsers, rendering engines,
/// and operating systems, plus a handful of device-class tokens. Every <see cref="Regex"/> here
/// is compiled with a 2 second match timeout — user-agent strings are attacker-controlled input
/// on any server that logs them, so no pattern is allowed to run unbounded.
///
/// List order matters: within a category, more specific signatures are placed before the generic
/// ones they'd otherwise be shadowed by (e.g. Edge/Opera/Samsung Internet before Chrome, since
/// their UA strings also carry a legacy "Chrome/x" compatibility token; Chrome before Safari,
/// since Chrome's UA also carries a trailing "Safari/x" token but never Safari's "Version/x").
/// </summary>
public static class UserAgentData
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);
    private const RegexOptions Opts = RegexOptions.IgnoreCase | RegexOptions.Compiled;

    private static Regex R(string pattern) => new(pattern, Opts, Timeout);

    // ---- Bots & crawlers (checked before browsers — a crawler's UA often also contains
    // legitimate-looking browser tokens, e.g. Googlebot Smartphone spoofs a Chrome/Android UA). ----
    public static readonly IReadOnlyList<UaSignature> Bots = new List<UaSignature>
    {
        new(R(@"Googlebot"), "Googlebot"),
        new(R(@"Google-InspectionTool"), "Google-InspectionTool"),
        new(R(@"AdsBot-Google"), "AdsBot-Google"),
        new(R(@"Mediapartners-Google"), "Google AdSense Bot"),
        new(R(@"bingbot|BingPreview"), "Bingbot"),
        new(R(@"Slurp"), "Yahoo Slurp"),
        new(R(@"DuckDuckBot"), "DuckDuckBot"),
        new(R(@"Baiduspider"), "Baiduspider"),
        new(R(@"YandexBot|YandexImages"), "YandexBot"),
        new(R(@"Sogou"), "Sogou Spider"),
        new(R(@"Exabot"), "Exabot"),
        new(R(@"facebookexternalhit|Facebot"), "Facebook Bot"),
        new(R(@"Twitterbot"), "Twitterbot"),
        new(R(@"LinkedInBot"), "LinkedInBot"),
        new(R(@"WhatsApp"), "WhatsApp"),
        new(R(@"TelegramBot"), "TelegramBot"),
        new(R(@"Discordbot"), "Discordbot"),
        new(R(@"SkypeUriPreview"), "Skype Bot"),
        new(R(@"Applebot"), "Applebot"),
        new(R(@"AhrefsBot"), "AhrefsBot"),
        new(R(@"SemrushBot"), "SemrushBot"),
        new(R(@"MJ12bot"), "MJ12bot"),
        new(R(@"DotBot"), "DotBot"),
        new(R(@"PetalBot"), "PetalBot"),
        new(R(@"BLEXBot"), "BLEXBot"),
        new(R(@"SeznamBot"), "SeznamBot"),
        new(R(@"UptimeRobot"), "UptimeRobot"),
        new(R(@"Pingdom"), "Pingdom Bot"),
        new(R(@"^curl/"), "curl"),
        new(R(@"^Wget/"), "Wget"),
        new(R(@"python-requests"), "python-requests"),
        new(R(@"PostmanRuntime"), "Postman"),
        new(R(@"Go-http-client"), "Go-http-client"),
        new(R(@"node-fetch"), "node-fetch"),
    };

    // ---- Browsers (order matters — see class remarks). ----
    public static readonly IReadOnlyList<UaSignature> Browsers = new List<UaSignature>
    {
        new(R(@"Edge/([\d.]+)"), "Edge (Legacy)"),
        new(R(@"EdgA/([\d.]+)"), "Edge"),
        new(R(@"EdgiOS/([\d.]+)"), "Edge"),
        new(R(@"Edg/([\d.]+)"), "Edge"),
        new(R(@"OPiOS/([\d.]+)"), "Opera"),
        new(R(@"OPR/([\d.]+)"), "Opera"),
        new(R(@"Opera Mini/([\d.]+)"), "Opera Mini"),
        new(R(@"Opera/([\d.]+)"), "Opera"),
        new(R(@"SamsungBrowser/([\d.]+)"), "Samsung Internet"),
        new(R(@"FxiOS/([\d.]+)"), "Firefox"),
        new(R(@"Firefox/([\d.]+)"), "Firefox"),
        new(R(@"Trident/.*rv:([\d.]+)"), "Internet Explorer"),
        new(R(@"MSIE ([\d.]+)"), "Internet Explorer"),
        new(R(@"CriOS/([\d.]+)"), "Chrome"),
        new(R(@"HeadlessChrome/([\d.]+)"), "Chrome (Headless)"),
        new(R(@"Chrome/([\d.]+)"), "Chrome"),
        new(R(@"Version/([\d.]+).*Safari/"), "Safari"),
    };

    // ---- Rendering engines (order matters — Blink tokens checked before the generic WebKit
    // fallback, since every Blink UA also still carries a legacy "AppleWebKit/x" token). ----
    public static readonly IReadOnlyList<UaSignature> Engines = new List<UaSignature>
    {
        new(R(@"Trident/([\d.]+)"), "Trident"),
        new(R(@"Edge/([\d.]+)"), "EdgeHTML"),
        new(R(@"Gecko/(\d+)"), "Gecko"),
        new(R(@"Chrome/([\d.]+)"), "Blink"),
        new(R(@"Chromium/([\d.]+)"), "Blink"),
        new(R(@"Presto/([\d.]+)"), "Presto"),
        new(R(@"AppleWebKit/([\d.]+)"), "WebKit"),
    };

    // ---- Operating systems (order matters — Android/ChromeOS checked before the generic Linux
    // fallback they'd otherwise be shadowed by, since both also carry a bare "Linux" token). ----
    public static readonly IReadOnlyList<UaOsSignature> OperatingSystems = new List<UaOsSignature>
    {
        new(R(@"Windows NT ([\d.]+)"), "Windows", m => MapWindowsVersion(m.Groups[1].Value)),
        new(R(@"iPhone OS (\d+[_.]\d+(?:[_.]\d+)?)"), "iOS", m => Dotted(m.Groups[1].Value)),
        new(R(@"CPU OS (\d+[_.]\d+(?:[_.]\d+)?)"), "iOS", m => Dotted(m.Groups[1].Value)),
        new(R(@"Android (\d+(?:\.\d+)?)"), "Android", m => m.Groups[1].Value),
        new(R(@"CrOS \S+ ([\d.]+)"), "ChromeOS", m => m.Groups[1].Value),
        new(R(@"Mac OS X (\d+[_.]\d+(?:[_.]\d+)?)"), "macOS", m => Dotted(m.Groups[1].Value)),
        new(R(@"Linux"), "Linux", _ => null),
    };

    // ---- Device-class tokens. ----
    public static readonly Regex MobileToken = R(@"Mobile");
    public static readonly Regex TabletToken = R(@"iPad|Tablet|SM-T\d");
    public static readonly Regex PlayStationToken = R(@"PlayStation");
    public static readonly Regex XboxToken = R(@"Xbox");
    public static readonly Regex NintendoToken = R(@"Nintendo");

    /// <summary>Five real-world user-agent strings covering desktop, mobile, tablet, and a bot — for the Sample button.</summary>
    public static readonly IReadOnlyList<string> SampleUserAgents = new List<string>
    {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/605.1.15",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)",
    };

    private static string MapWindowsVersion(string ntVersion) => ntVersion switch
    {
        "10.0" => "10/11",
        "6.3" => "8.1",
        "6.2" => "8",
        "6.1" => "7",
        "6.0" => "Vista",
        "5.1" or "5.2" => "XP",
        _ => ntVersion,
    };

    private static string Dotted(string raw) => raw.Replace('_', '.');
}
