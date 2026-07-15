using System.Reflection;
using System.Runtime.InteropServices;

namespace Delp.App.Infrastructure;

/// <summary>App metadata surfaced by the About dialog and diagnostics copy.</summary>
public static class AppInfo
{
    public const string RepositoryUrl = "https://github.com/JohnGrey0/delp";
    public const string IssuesUrl = "https://github.com/JohnGrey0/delp/issues";
    public const string LicenseLine = "MIT License © 2026 John Grey";

    public static string Version
    {
        get
        {
            var informational = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (string.IsNullOrEmpty(informational))
                return "unknown";
            var plus = informational.IndexOf('+');
            return plus > 0 ? informational[..plus] : informational;
        }
    }

    public static string Framework => RuntimeInformation.FrameworkDescription;

    public static string Os => RuntimeInformation.OSDescription;

    public static string InstallDirectory => AppContext.BaseDirectory.TrimEnd('\\');

    public static string SettingsPath => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Delp", "settings.json");

    public static int ToolCount => ToolCatalog.All.Count;

    public static string DiagnosticsText =>
        $"Delp {Version}{Environment.NewLine}" +
        $"Tools: {ToolCount}{Environment.NewLine}" +
        $"Runtime: {Framework}{Environment.NewLine}" +
        $"OS: {Os}{Environment.NewLine}" +
        $"Install: {InstallDirectory}";
}
