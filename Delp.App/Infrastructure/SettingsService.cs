using System.IO;
using System.Text.Json;

namespace Delp.App.Infrastructure;

public sealed class UserSettings
{
    public List<string> Favorites { get; set; } = new();
    public Dictionary<string, bool> CollapsedGroups { get; set; } = new();
}

/// <summary>
/// Tiny write-through user settings store (%LOCALAPPDATA%\Delp\settings.json).
/// UI-thread only. Corrupt or missing files silently reset to defaults.
/// </summary>
public static class SettingsService
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Delp", "settings.json");

    private static UserSettings? _current;

    public static UserSettings Current => _current ??= Load();

    /// <summary>Raised after a favorite is added or removed.</summary>
    public static event Action? FavoritesChanged;

    public static bool IsFavorite(string toolId) =>
        Current.Favorites.Contains(toolId, StringComparer.OrdinalIgnoreCase);

    public static void ToggleFavorite(string toolId)
    {
        var existing = Current.Favorites
            .FirstOrDefault(f => f.Equals(toolId, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
            Current.Favorites.Add(toolId);
        else
            Current.Favorites.Remove(existing);
        Save();
        FavoritesChanged?.Invoke();
    }

    public static bool IsGroupCollapsed(string groupName) =>
        Current.CollapsedGroups.TryGetValue(groupName, out var collapsed) && collapsed;

    public static void SetGroupCollapsed(string groupName, bool collapsed)
    {
        if (IsGroupCollapsed(groupName) == collapsed)
            return;
        Current.CollapsedGroups[groupName] = collapsed;
        Save();
    }

    private static UserSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(FilePath))
                       ?? new UserSettings();
        }
        catch
        {
            // fall through to defaults
        }
        return new UserSettings();
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath,
                JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // settings persistence is best-effort; never crash the app for it
        }
    }
}
