namespace Delp.App.Infrastructure;

/// <summary>
/// Marks a UserControl as a tool. The catalog discovers these by reflection,
/// so adding a tool never requires editing a shared registry file.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ToolAttribute : Attribute
{
    public ToolAttribute(string id, string name, ToolCategory category, string description)
    {
        Id = id;
        Name = name;
        Category = category;
        Description = description;
    }

    /// <summary>Stable kebab-case identifier, e.g. "base64".</summary>
    public string Id { get; }

    public string Name { get; }

    public ToolCategory Category { get; }

    /// <summary>One sentence shown under the tool name.</summary>
    public string Description { get; }

    /// <summary>Comma-separated extra search terms.</summary>
    public string Keywords { get; set; } = "";

    /// <summary>Sort position within the category (lower sorts first).</summary>
    public int Order { get; set; } = 100;
}
