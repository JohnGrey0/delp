using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("code-shot", "Code Screenshot", ToolCategory.DevUtilities,
    "Turn a code snippet into a shareable, syntax-highlighted PNG on a gradient card.",
    Keywords = "code,screenshot,snippet,image,carbon,share,png", Order = 55)]
public partial class CodeShotView : UserControl
{
    private const string SampleCode = """
        public static int Fibonacci(int n)
        {
            if (n <= 1) return n;
            return Fibonacci(n - 1) + Fibonacci(n - 2);
        }
        """;

    // Rendering runs a full Measure/Arrange + RenderTargetBitmap pass, so it's too
    // expensive to run on every keystroke; debounce it like QrCodeView does.
    private readonly DispatcherTimer _debounce = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private bool _ready;
    private RenderTargetBitmap? _lastBitmap;

    public CodeShotView()
    {
        InitializeComponent();

        LanguageCombo.Items.Add("Plain");
        foreach (var definition in HighlightingManager.Instance.HighlightingDefinitions)
            LanguageCombo.Items.Add(definition.Name);
        LanguageCombo.SelectedItem = LanguageCombo.Items.Contains("C#") ? "C#" : LanguageCombo.Items[0];

        foreach (var theme in CodeShotThemes.All)
            ThemeCombo.Items.Add(theme.Name);
        ThemeCombo.SelectedIndex = 0;

        CodeBox.Text = SampleCode;

        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Render();
        };

        _ready = true;
        Render();
    }

    private void CodeBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_ready)
            Debounce();
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (_ready)
            Debounce();
    }

    private void Combo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_ready)
            Debounce();
    }

    private void Chrome_Changed(object sender, RoutedEventArgs e)
    {
        if (!_ready)
            return;
        TitleBox.IsEnabled = ChromeBox.IsChecked == true;
        Debounce();
    }

    private void Debounce()
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private void Render()
    {
        try
        {
            // Live preview always rasterizes at 1x. RenderTargetBitmap runs synchronously
            // on the UI thread, and 2x is ~4x the pixel count of 1x for the same content
            // -- real cost either way, so it's still deferred to export rather than run
            // on every debounced keystroke. The user's chosen scale is only applied when
            // they actually export (Save_Click / CopyImage_Click), a one-off action
            // where that extra cost is expected and acceptable.
            var bitmap = CodeShotRenderer.Render(BuildOptions(scale: 1));
            _lastBitmap = bitmap;
            PreviewImage.Source = bitmap;
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            _lastBitmap = null;
            PreviewImage.Source = null;
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private CodeShotOptions BuildOptions(int scale)
    {
        var language = LanguageCombo.SelectedItem as string ?? "Plain";
        var themeName = ThemeCombo.SelectedItem as string;
        var theme = CodeShotThemes.All.FirstOrDefault(t => t.Name == themeName) ?? CodeShotThemes.All[0];
        var padding = ParsePadding((PaddingCombo.SelectedItem as ComboBoxItem)?.Tag as string);
        var showChrome = ChromeBox.IsChecked == true;
        var title = showChrome ? TitleBox.Text : null;

        return new CodeShotOptions(
            CodeBox.Text,
            language,
            theme,
            showChrome,
            title,
            LineNumbersBox.IsChecked == true,
            padding,
            scale);
    }

    private int SelectedScale() =>
        int.TryParse((ScaleCombo.SelectedItem as ComboBoxItem)?.Tag as string, out var s) ? s : 1;

    private static CodeShotPadding ParsePadding(string? tag) => tag switch
    {
        "Small" => CodeShotPadding.Small,
        "Large" => CodeShotPadding.Large,
        _ => CodeShotPadding.Medium,
    };

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_lastBitmap is null)
            return;

        var dialog = new SaveFileDialog { Filter = "PNG image (*.png)|*.png", FileName = "code-shot.png" };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            // Re-render at the scale the user actually picked (preview always used 1x).
            var bitmap = CodeShotRenderer.Render(BuildOptions(SelectedScale()));
            File.WriteAllBytes(dialog.FileName, CodeShotRenderer.ToPngBytes(bitmap));
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void CopyImage_Click(object sender, RoutedEventArgs e)
    {
        if (_lastBitmap is null)
            return;

        try
        {
            var bitmap = CodeShotRenderer.Render(BuildOptions(SelectedScale()));
            Clipboard.SetImage(bitmap);
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            // Clipboard can be transiently locked by another process (remote desktop
            // clipboard sync, another app mid-copy, etc.) -- surface it, don't crash.
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
