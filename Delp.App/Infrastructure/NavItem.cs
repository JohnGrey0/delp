namespace Delp.App.Infrastructure;

/// <summary>
/// A sidebar/flyout list entry. The same tool can appear twice — once under
/// "★ Favorites" and once under its category — as two distinct NavItems.
/// </summary>
public sealed record NavItem(ToolInfo Tool, string Group)
{
    public const string FavoritesGroup = "★ Favorites";

    public string Name => Tool.Name;
    public string Description => Tool.Description;

    /// <summary>Builds the grouped list: favorites first, then every tool by category.</summary>
    public static List<NavItem> BuildGrouped(IReadOnlyList<ToolInfo> tools)
    {
        var items = new List<NavItem>();
        items.AddRange(tools.Where(t => SettingsService.IsFavorite(t.Id))
            .Select(t => new NavItem(t, FavoritesGroup)));
        items.AddRange(tools.Select(t => new NavItem(t, t.CategoryName)));
        return items;
    }
}
