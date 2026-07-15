using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

public enum CodeShotPadding
{
    Small,
    Medium,
    Large,
}

/// <summary>Everything needed to render one code screenshot.</summary>
public sealed record CodeShotOptions(
    string Code,
    string Language,
    CodeShotTheme Theme,
    bool ShowChrome,
    string? Title,
    bool ShowLineNumbers,
    CodeShotPadding Padding,
    int Scale);

/// <summary>
/// Renders <see cref="CodeShotOptions"/> to a bitmap: gradient background → rounded
/// card with drop shadow → optional window-chrome bar (traffic dots + title) →
/// read-only AvalonEdit editor sized to its content. This builds a real WPF element
/// tree and rasterizes it with Measure/Arrange + RenderTargetBitmap, so — per Core's
/// pure/UI-free rule — it lives in the App layer; Core only holds the theme data.
/// The colors below come entirely from the selected <see cref="CodeShotTheme"/> (the
/// rendered image is content, not app chrome), never from the app's theme resources.
/// </summary>
public static class CodeShotRenderer
{
    public const int MaxLines = 300;
    public const int MaxChars = 20_000;

    private const double CardRadius = 14;

    // AvalonEdit's bundled highlighting definitions assume a light editor background.
    // A handful of named colors are near-black (contrast ratio ~1.0-1.4, i.e. almost
    // the same luminance as the text underneath it) and are effectively invisible on
    // code-shot's dark card themes -- verified by rendering "12345" and watching the
    // digits disappear against a dark card. These replacements (all >5:1 contrast
    // against every dark theme's CardBg) are applied only while rendering a dark
    // theme, and only reach editors whose language actually defines that named color.
    private static readonly (string Name, Color Color)[] DarkCardColorOverrides =
    [
        ("NumberLiteral", Color.FromRgb(0xB5, 0xCE, 0xA8)),
        ("MethodCall", Color.FromRgb(0xDC, 0xDC, 0xAA)),
        ("StringInterpolation", Color.FromRgb(0xCE, 0x91, 0x78)),
        ("GotoKeywords", Color.FromRgb(0x56, 0x9C, 0xD6)),
        ("ContextKeywords", Color.FromRgb(0x56, 0x9C, 0xD6)),
    ];

    // AvalonEdit's TextEditor never becomes collectible once discarded -- verified by
    // constructing and dropping 60 offscreen editors (highlighted, measured/arranged,
    // rasterized, then dereferenced): all 60 were still reachable after a forced GC,
    // ~3 MB each. The live preview re-renders on every debounced keystroke, so a fresh
    // TextEditor per call leaks unboundedly over an editing session. Reusing a single
    // cached instance (same test, same 60 renders) keeps growth to ~50 KB total.
    private static TextEditor? _cachedEditor;

    /// <exception cref="FormatException">The code exceeds the line/char cap.</exception>
    public static void Validate(string code)
    {
        if (code.Length > MaxChars)
        {
            throw new FormatException(
                $"That's {code.Length:N0} characters — code screenshots cap out at {MaxChars:N0}. Trim the input and try again.");
        }

        var lines = code.Length == 0 ? 0 : 1;
        foreach (var ch in code)
        {
            if (ch == '\n')
                lines++;
        }

        if (lines > MaxLines)
        {
            throw new FormatException(
                $"That's {lines:N0} lines — code screenshots cap out at {MaxLines}. Trim the input and try again.");
        }
    }

    /// <summary>Builds the offscreen visual and rasterizes it to a frozen bitmap at the requested scale.</summary>
    /// <exception cref="FormatException">The code exceeds the line/char cap.</exception>
    public static RenderTargetBitmap Render(CodeShotOptions options)
    {
        Validate(options.Code);

        var highlighting = options.Language.Equals("Plain", StringComparison.OrdinalIgnoreCase)
            ? null
            : HighlightingManager.Instance.GetDefinition(options.Language);

        var root = BuildVisual(options, highlighting);
        root.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        root.Arrange(new Rect(root.DesiredSize));

        var scale = options.Scale <= 0 ? 1 : options.Scale;
        var dpi = 96.0 * scale;
        var pixelWidth = Math.Max(1, (int)Math.Ceiling(root.DesiredSize.Width * scale));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(root.DesiredSize.Height * scale));

        var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, dpi, dpi, PixelFormats.Pbgra32);

        // The override table mutates AvalonEdit's shared, app-wide HighlightingManager
        // singleton (the same HighlightingColor objects the rule engine paints with),
        // so it must be reverted immediately after this render -- never left changed
        // for whatever renders next.
        var restore = ApplyDarkCardColorOverrides(highlighting, options.Theme.IsLight);
        try
        {
            bitmap.Render(root);
        }
        finally
        {
            RevertColorOverrides(restore);
        }

        bitmap.Freeze();
        return bitmap;
    }

    private static List<(HighlightingColor Color, HighlightingBrush? Original)>? ApplyDarkCardColorOverrides(
        IHighlightingDefinition? highlighting, bool isLightTheme)
    {
        if (highlighting is null || isLightTheme)
            return null;

        List<(HighlightingColor, HighlightingBrush?)>? saved = null;
        foreach (var (name, color) in DarkCardColorOverrides)
        {
            var namedColor = highlighting.GetNamedColor(name);
            if (namedColor is null)
                continue;

            saved ??= [];
            saved.Add((namedColor, namedColor.Foreground));
            namedColor.Foreground = new SimpleHighlightingBrush(color);
        }
        return saved;
    }

    private static void RevertColorOverrides(List<(HighlightingColor Color, HighlightingBrush? Original)>? saved)
    {
        if (saved is null)
            return;

        foreach (var (namedColor, original) in saved)
            namedColor.Foreground = original;
    }

    public static byte[] ToPngBytes(BitmapSource bitmap)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static Border BuildVisual(CodeShotOptions options, IHighlightingDefinition? highlighting)
    {
        var theme = options.Theme;
        var padding = options.Padding switch
        {
            CodeShotPadding.Small => 24,
            CodeShotPadding.Large => 88,
            _ => 48,
        };

        var editor = GetOrCreateEditor();
        editor.Text = options.Code;
        editor.ShowLineNumbers = options.ShowLineNumbers;
        editor.Foreground = new SolidColorBrush(ParseColor(theme.DefaultFg));
        editor.LineNumbersForeground = new SolidColorBrush(ParseColor(theme.LineNumberFg));
        editor.Padding = new Thickness(22, 18, 26, 20);
        editor.SyntaxHighlighting = highlighting;

        var body = new StackPanel { Orientation = Orientation.Vertical };
        if (options.ShowChrome)
            body.Children.Add(BuildChromeBar(theme, options.Title));
        body.Children.Add(editor);

        var card = new Border
        {
            Background = new SolidColorBrush(ParseColor(theme.CardBg)),
            CornerRadius = new CornerRadius(CardRadius),
            Child = body,
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                Opacity = 0.45,
                BlurRadius = 32,
                ShadowDepth = 12,
                Direction = 270,
            },
        };

        var gradient = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
        foreach (var stop in theme.GradientStops)
            gradient.GradientStops.Add(new GradientStop(ParseColor(stop.Hex), stop.Offset));

        return new Border
        {
            Background = gradient,
            Padding = new Thickness(padding),
            Child = card,
        };
    }

    /// <summary>
    /// Returns the cached offscreen editor, detaching it from whatever card it was
    /// last rendered into (a WPF element can only ever have one logical parent).
    /// See the leak note on <see cref="_cachedEditor"/> for why this is cached at all.
    /// </summary>
    private static TextEditor GetOrCreateEditor()
    {
        if (_cachedEditor is { } existing)
        {
            (existing.Parent as Panel)?.Children.Remove(existing);
            return existing;
        }

        var editor = new TextEditor
        {
            IsReadOnly = true,
            WordWrap = false,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            FontFamily = new FontFamily("Cascadia Mono, Cascadia Code, Consolas"),
            FontSize = 14,
            Background = Brushes.Transparent,
            MinWidth = 220,
        };
        editor.Options.EnableHyperlinks = false;
        editor.Options.EnableEmailHyperlinks = false;
        editor.Options.ShowTabs = false;
        editor.Options.ShowSpaces = false;

        _cachedEditor = editor;
        return editor;
    }

    private static UIElement BuildChromeBar(CodeShotTheme theme, string? title)
    {
        var dots = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(20, 0, 0, 0),
        };
        dots.Children.Add(Dot(theme.ChromeDotRed));
        dots.Children.Add(Dot(theme.ChromeDotYellow));
        dots.Children.Add(Dot(theme.ChromeDotGreen));

        var grid = new Grid { Height = 44 };
        grid.Children.Add(dots);

        if (!string.IsNullOrWhiteSpace(title))
        {
            grid.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = new SolidColorBrush(ParseColor(theme.TitleFg)),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
        }

        return new Border { CornerRadius = new CornerRadius(CardRadius, CardRadius, 0, 0), Child = grid };
    }

    private static Ellipse Dot(string hex) => new()
    {
        Width = 12,
        Height = 12,
        Margin = new Thickness(0, 0, 8, 0),
        Fill = new SolidColorBrush(ParseColor(hex)),
    };

    private static Color ParseColor(string hex)
    {
        var s = hex.StartsWith('#') ? hex[1..] : hex;
        if (s.Length == 3)
            s = $"{s[0]}{s[0]}{s[1]}{s[1]}{s[2]}{s[2]}";
        return (Color)ColorConverter.ConvertFromString("#" + s)!;
    }
}
