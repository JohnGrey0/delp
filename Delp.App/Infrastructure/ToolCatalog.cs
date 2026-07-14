using System.Reflection;
using System.Windows.Controls;

namespace Delp.App.Infrastructure;

public sealed record ToolInfo(
    string Id,
    string Name,
    string Description,
    ToolCategory Category,
    string Keywords,
    int Order,
    Type ViewType)
{
    public string CategoryName => ToolCategories.DisplayName(Category);
}

/// <summary>Reflection-based registry of every [Tool]-attributed UserControl in this assembly.</summary>
public static class ToolCatalog
{
    public static IReadOnlyList<ToolInfo> All { get; } = Discover();

    public static ToolInfo? Find(string id) =>
        All.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Every whitespace-separated term must match name, description, keywords or category.</summary>
    public static IReadOnlyList<ToolInfo> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return All;

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return All.Where(t => terms.All(term =>
                t.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || t.Description.Contains(term, StringComparison.OrdinalIgnoreCase)
                || t.Keywords.Contains(term, StringComparison.OrdinalIgnoreCase)
                || t.CategoryName.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public static UserControl CreateView(ToolInfo info) =>
        (UserControl)Activator.CreateInstance(info.ViewType)!;

    private static List<ToolInfo> Discover() =>
        typeof(ToolCatalog).Assembly.GetTypes()
            .Select(t => (Type: t, Attr: t.GetCustomAttribute<ToolAttribute>()))
            .Where(x => x.Attr is not null && typeof(UserControl).IsAssignableFrom(x.Type))
            .Select(x => new ToolInfo(
                x.Attr!.Id, x.Attr.Name, x.Attr.Description, x.Attr.Category,
                x.Attr.Keywords, x.Attr.Order, x.Type))
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Order)
            .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
