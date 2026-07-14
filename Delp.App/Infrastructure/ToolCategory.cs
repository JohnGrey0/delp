namespace Delp.App.Infrastructure;

/// <summary>Top-level tool groups, in sidebar display order.</summary>
public enum ToolCategory
{
    Encoding,
    Hashing,
    DataFormat,
    WebDev,
    TextProcessing,
    DevUtilities,
}

public static class ToolCategories
{
    public static string DisplayName(ToolCategory category) => category switch
    {
        ToolCategory.Encoding => "Encoding & Decoding",
        ToolCategory.Hashing => "Hashing & Generation",
        ToolCategory.DataFormat => "Data Format",
        ToolCategory.WebDev => "Web Development",
        ToolCategory.TextProcessing => "Text Processing",
        ToolCategory.DevUtilities => "Developer Utilities",
        _ => category.ToString(),
    };
}
