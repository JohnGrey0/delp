using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Delp.Core.Tools.WebDev;

namespace Delp.App.Tools.WebDev;

/// <summary>
/// Renders <see cref="PlaceholderOptions"/> to PNG/JPEG bytes. This is WPF-only (DrawingVisual +
/// RenderTargetBitmap), so — per Core's pure/UI-free rule — it lives in the App layer; Core only
/// validates the options.
/// </summary>
public static class PlaceholderRenderer
{
    /// <exception cref="ArgumentException">The options fail <see cref="PlaceholderTool.Validate"/>.</exception>
    public static byte[] Render(PlaceholderOptions options)
    {
        PlaceholderTool.Validate(options);

        var bg = ParseColor(options.Background);
        var fg = ParseColor(options.Foreground);
        var label = string.IsNullOrWhiteSpace(options.Label)
            ? PlaceholderTool.DefaultLabel(options.Width, options.Height)
            : options.Label;

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(new SolidColorBrush(bg), null, new Rect(0, 0, options.Width, options.Height));

            var fontSize = Math.Clamp(Math.Min(options.Width, options.Height) / 5.0, 10, 96);
            var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var formatted = new FormattedText(
                label,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                new SolidColorBrush(fg),
                1.0);

            var origin = new Point(
                (options.Width - formatted.Width) / 2,
                (options.Height - formatted.Height) / 2);
            dc.DrawText(formatted, origin);
        }

        var bitmap = new RenderTargetBitmap(options.Width, options.Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        BitmapEncoder encoder = options.Kind == ImageKind.Jpeg
            ? new JpegBitmapEncoder()
            : new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static Color ParseColor(string hex)
    {
        var s = hex.StartsWith('#') ? hex[1..] : hex;
        if (s.Length == 3)
            s = $"{s[0]}{s[0]}{s[1]}{s[1]}{s[2]}{s[2]}";
        return (Color)ColorConverter.ConvertFromString("#" + s)!;
    }
}
