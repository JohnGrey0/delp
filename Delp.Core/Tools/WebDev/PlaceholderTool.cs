namespace Delp.Core.Tools.WebDev;

public enum ImageKind
{
    Png,
    Jpeg,
}

public sealed record PlaceholderOptions(
    int Width,
    int Height,
    string Background,
    string Foreground,
    string? Label,
    ImageKind Kind);

/// <summary>
/// Validates placeholder-image options. Rendering itself needs WPF (DrawingVisual /
/// RenderTargetBitmap) so it lives in the App layer — this class stays pure and testable.
/// </summary>
public static class PlaceholderTool
{
    public const int MinDimension = 1;
    public const int MaxDimension = 4096;

    /// <exception cref="ArgumentException">Width/height are out of range, or a color is not a valid hex color.</exception>
    public static void Validate(PlaceholderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Width < MinDimension || options.Width > MaxDimension)
            throw new ArgumentException(
                $"Width must be between {MinDimension} and {MaxDimension} pixels (got {options.Width}).");
        if (options.Height < MinDimension || options.Height > MaxDimension)
            throw new ArgumentException(
                $"Height must be between {MinDimension} and {MaxDimension} pixels (got {options.Height}).");

        ValidateHexColor(options.Background, "Background color");
        ValidateHexColor(options.Foreground, "Foreground color");
    }

    /// <summary>Default label shown when no custom label is supplied: "W×H".</summary>
    public static string DefaultLabel(int width, int height) => $"{width}×{height}";

    /// <exception cref="ArgumentException">The value is not a #RGB or #RRGGBB hex color.</exception>
    public static void ValidateHexColor(string? color, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException($"{fieldName} is required.");

        var s = color.StartsWith('#') ? color[1..] : color;
        if (s.Length != 3 && s.Length != 6)
            throw new ArgumentException($"{fieldName} '{color}' must be a #RGB or #RRGGBB hex color.");

        foreach (var c in s)
        {
            if (!Uri.IsHexDigit(c))
                throw new ArgumentException($"{fieldName} '{color}' contains an invalid hex digit '{c}'.");
        }
    }
}
