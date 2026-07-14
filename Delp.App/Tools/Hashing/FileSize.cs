using System.Globalization;

namespace Delp.App.Tools.Hashing;

/// <summary>Human-readable byte-count formatting shared by the file-based hashing tools.</summary>
internal static class FileSize
{
    public static string Format(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return unit == 0
            ? $"{bytes} B"
            : $"{size.ToString("0.##", CultureInfo.InvariantCulture)} {units[unit]}";
    }
}
