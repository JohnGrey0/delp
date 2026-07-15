namespace Delp.Core.Tools.DevUtilities;

/// <summary>
/// A single stop in a top-left-to-bottom-right background gradient.
/// <paramref name="Offset"/> is 0..1; <paramref name="Hex"/> is "#RRGGBB".
/// </summary>
public sealed record CodeShotGradientStop(double Offset, string Hex);

/// <summary>
/// A code-screenshot color theme: pure data describing the "wallpaper" gradient
/// behind the card, the card itself, and the text/chrome colors drawn on top.
/// The App layer turns these hex strings into WPF brushes when it renders the
/// offscreen visual — Core stays UI-free. This is the *content* palette for the
/// rendered image, not the app's own chrome (which always uses theme resources).
/// </summary>
public sealed record CodeShotTheme(
    string Name,
    IReadOnlyList<CodeShotGradientStop> GradientStops,
    string CardBg,
    string DefaultFg,
    string LineNumberFg,
    string TitleFg,
    string ChromeDotRed,
    string ChromeDotYellow,
    string ChromeDotGreen,
    bool IsLight);

/// <summary>The built-in code-shot themes, in display order.</summary>
public static class CodeShotThemes
{
    public static IReadOnlyList<CodeShotTheme> All { get; } =
    [
        new CodeShotTheme(
            Name: "Midnight",
            GradientStops:
            [
                new CodeShotGradientStop(0, "#0F0C29"),
                new CodeShotGradientStop(0.5, "#302B63"),
                new CodeShotGradientStop(1, "#24243E"),
            ],
            CardBg: "#1E1E2E",
            DefaultFg: "#CDD6F4",
            LineNumberFg: "#6C7086",
            TitleFg: "#A6ADC8",
            ChromeDotRed: "#FF5F56",
            ChromeDotYellow: "#FFBD2E",
            ChromeDotGreen: "#27C93F",
            IsLight: false),

        new CodeShotTheme(
            Name: "Slate",
            GradientStops:
            [
                new CodeShotGradientStop(0, "#232526"),
                new CodeShotGradientStop(1, "#414345"),
            ],
            CardBg: "#1E2530",
            DefaultFg: "#E2E8F0",
            LineNumberFg: "#64748B",
            TitleFg: "#94A3B8",
            ChromeDotRed: "#FF5F56",
            ChromeDotYellow: "#FFBD2E",
            ChromeDotGreen: "#27C93F",
            IsLight: false),

        new CodeShotTheme(
            Name: "Sunset",
            GradientStops:
            [
                new CodeShotGradientStop(0, "#FF5F6D"),
                new CodeShotGradientStop(1, "#FFC371"),
            ],
            CardBg: "#2B1E33",
            DefaultFg: "#FBE8D3",
            LineNumberFg: "#B98CA6",
            TitleFg: "#F3C9DA",
            ChromeDotRed: "#FF5F56",
            ChromeDotYellow: "#FFBD2E",
            ChromeDotGreen: "#27C93F",
            IsLight: false),

        new CodeShotTheme(
            Name: "Paper-light",
            GradientStops:
            [
                new CodeShotGradientStop(0, "#E6EAF0"),
                new CodeShotGradientStop(1, "#C9D6E3"),
            ],
            CardBg: "#FFFFFF",
            DefaultFg: "#1F2328",
            LineNumberFg: "#8B949E",
            TitleFg: "#57606A",
            ChromeDotRed: "#FF5F56",
            ChromeDotYellow: "#FFBD2E",
            ChromeDotGreen: "#27C93F",
            IsLight: true),
    ];
}
