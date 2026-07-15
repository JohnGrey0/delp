using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Delp.App.Infrastructure;
using Delp.Core.Tools.WebDev;

namespace Delp.App.Tools.WebDev;

[Tool("color-convert", "Color Converter & Picker", ToolCategory.WebDev,
    "Convert colors between hex, RGB, HSL, and HSB, check WCAG contrast, simulate color vision " +
    "deficiency, or pick one straight off the screen with an eyedropper.",
    Keywords = "color,hex,rgb,hsl,hsb,hsv,css,eyedropper,picker,screen,blotter,pixel,color-blotter," +
               "contrast,wcag,accessibility,a11y,colorblind,protanopia,deuteranopia", Order = 10)]
public partial class ColorConvertView : UserControl
{
    private const int MaxHistory = 24;

    private static readonly ParsedColor White = new(255, 255, 255, 255);
    private static readonly ParsedColor Black = new(0, 0, 0, 255);

    private readonly ObservableCollection<HistoryEntry> _history = [];
    private readonly Brush _successBrush;
    private readonly Brush _dangerBrush;

    private bool _updating;
    private bool _contrastUpdating;
    private bool _visionUpdating;

    public ColorConvertView()
    {
        InitializeComponent();
        _successBrush = (Brush)FindResource("Brush.Success");
        _dangerBrush = (Brush)FindResource("Brush.Danger");
        HistoryStrip.ItemsSource = _history;
        Run(ApplyFromInput);
        RunContrast(ApplyContrast);
        RunVision(ApplyVision);
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e) => Run(ApplyFromInput);

    private void Channel_TextChanged(object sender, TextChangedEventArgs e) => Run(ApplyFromChannels);

    private void ApplyFromInput()
    {
        var c = ColorTool.Parse(InputBox.Text);
        RBox.Text = c.R.ToString(CultureInfo.InvariantCulture);
        GBox.Text = c.G.ToString(CultureInfo.InvariantCulture);
        BBox.Text = c.B.ToString(CultureInfo.InvariantCulture);
        ABox.Text = c.A.ToString(CultureInfo.InvariantCulture);
        UpdateDerived(c);
    }

    private void ApplyFromChannels()
    {
        var c = new ParsedColor(
            ParseChannel(RBox.Text, "R"),
            ParseChannel(GBox.Text, "G"),
            ParseChannel(BBox.Text, "B"),
            ParseChannel(ABox.Text, "A"));
        InputBox.Text = ColorTool.ToHex(c, alpha: false);
        UpdateDerived(c);
    }

    private static byte ParseChannel(string text, string name)
    {
        if (!int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) || v < 0 || v > 255)
            throw new FormatException($"{name} must be an integer from 0 to 255.");
        return (byte)v;
    }

    private void UpdateDerived(ParsedColor c)
    {
        HexBox.Text = ColorTool.ToHex(c, alpha: c.A != 255);
        RgbBox.Text = ColorTool.ToRgbCss(c);
        HslBox.Text = ColorTool.ToHslCss(c);
        HsbBox.Text = ColorTool.ToHsbCss(c);
        Swatch.Background = new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));

        var vsWhite = ColorTool.ContrastRatio(c, White);
        var vsBlack = ColorTool.ContrastRatio(c, Black);
        ContrastText.Text =
            $"Contrast vs white: {Fmt(vsWhite)}:1 ({Badge(vsWhite)})   ·   vs black: {Fmt(vsBlack)}:1 ({Badge(vsBlack)})";
    }

    private static string Fmt(double ratio) => ratio.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Badge(double ratio) => ratio switch
    {
        >= 7 => "AAA",
        >= 4.5 => "AA",
        >= 3 => "AA Large",
        _ => "Fail",
    };

    private void CopyHex_Click(object sender, RoutedEventArgs e) => Ui.Copy(HexBox.Text, CopyHexBtn);
    private void CopyRgb_Click(object sender, RoutedEventArgs e) => Ui.Copy(RgbBox.Text, CopyRgbBtn);
    private void CopyHsl_Click(object sender, RoutedEventArgs e) => Ui.Copy(HslBox.Text, CopyHslBtn);
    private void CopyHsb_Click(object sender, RoutedEventArgs e) => Ui.Copy(HsbBox.Text, CopyHsbBtn);

    /// <summary>
    /// Runs the eyedropper overlay (owned by this tool since the color-blotter merge) and, on a
    /// successful pick, feeds the sampled color into the converter input via the normal
    /// InputBox_TextChanged → Run(ApplyFromInput) path, so the reentrancy guard still applies.
    /// </summary>
    private void PickButton_Click(object sender, RoutedEventArgs e)
    {
        var overlay = new ColorPickerOverlayWindow { Owner = Window.GetWindow(this) };
        overlay.ShowDialog();
        if (overlay.Picked)
            RecordPick(overlay.PickedR, overlay.PickedG, overlay.PickedB);
    }

    /// <summary>Adds a fresh screen pick to the session history and loads it into the converter.</summary>
    private void RecordPick(byte r, byte g, byte b)
    {
        var hex = ColorTool.ToHex(new ParsedColor(r, g, b, 255), alpha: false);

        var entry = new HistoryEntry(r, g, b, hex);
        if (_history.Count == 0 || _history[0].Hex != entry.Hex)
        {
            _history.Insert(0, entry);
            while (_history.Count > MaxHistory)
                _history.RemoveAt(_history.Count - 1);
        }

        InputBox.Text = hex;
    }

    /// <summary>Clicking a history swatch re-feeds the converter without adding a duplicate entry.</summary>
    private void HistorySwatch_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: HistoryEntry entry })
            InputBox.Text = entry.Hex;
    }

    /// <summary>Runs a conversion with reentrancy protection and inline error reporting.</summary>
    private void Run(Action convert)
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            convert();
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            _updating = false;
        }
    }

    // ---- CONTRAST tab ----

    private void Contrast_TextChanged(object sender, TextChangedEventArgs e)
    {
        // XAML default Text fires this during InitializeComponent, before sibling elements exist;
        // the constructor runs the initial ApplyContrast explicitly instead.
        if (!IsLoaded)
            return;
        RunContrast(ApplyContrast);
    }

    /// <summary>
    /// Reads FG/BG, reuses ColorTool.Luminance/ContrastRatio (no reimplementation), and renders the
    /// ratio readout, the four WCAG pass/fail badges, and a live fg-on-bg sample.
    /// </summary>
    private void ApplyContrast()
    {
        var fg = ColorTool.Parse(FgBox.Text);
        var bg = ColorTool.Parse(BgBox.Text);

        var fgBrush = new SolidColorBrush(Color.FromRgb(fg.R, fg.G, fg.B));
        var bgBrush = new SolidColorBrush(Color.FromRgb(bg.R, bg.G, bg.B));
        FgSwatch.Background = fgBrush;
        BgSwatch.Background = bgBrush;

        var ratio = ColorTool.ContrastRatio(fg, bg);
        RatioText.Text = $"{Fmt(ratio)} : 1";

        var badges = WcagContrastTool.Evaluate(ratio);
        SetBadge(AaNormalText, badges.AaNormal, "AA normal (≥4.5:1)");
        SetBadge(AaLargeText, badges.AaLarge, "AA large (≥3:1)");
        SetBadge(AaaNormalText, badges.AaaNormal, "AAA normal (≥7:1)");
        SetBadge(AaaLargeText, badges.AaaLarge, "AAA large (≥4.5:1)");

        SampleBorder.Background = bgBrush;
        SampleLargeText.Foreground = fgBrush;
        SampleNormalText.Foreground = fgBrush;
    }

    private void SetBadge(TextBlock text, bool pass, string label)
    {
        text.Foreground = pass ? _successBrush : _dangerBrush;
        text.Text = (pass ? "✓ " : "✗ ") + label;
    }

    /// <summary>Reuses the same eyedropper overlay as the CONVERT tab's "Pick from screen".</summary>
    private void PickFg_Click(object sender, RoutedEventArgs e) => PickInto(FgBox);

    private void PickBg_Click(object sender, RoutedEventArgs e) => PickInto(BgBox);

    private void PickInto(TextBox target)
    {
        var overlay = new ColorPickerOverlayWindow { Owner = Window.GetWindow(this) };
        overlay.ShowDialog();
        if (overlay.Picked)
            target.Text = ColorTool.ToHex(new ParsedColor(overlay.PickedR, overlay.PickedG, overlay.PickedB, 255), alpha: false);
    }

    /// <summary>Runs a CONTRAST-tab update with its own reentrancy guard and error banner.</summary>
    private void RunContrast(Action apply)
    {
        if (_contrastUpdating)
            return;
        _contrastUpdating = true;
        try
        {
            apply();
            ContrastErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ContrastErrorText.Text = ex.Message;
            ContrastErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            _contrastUpdating = false;
        }
    }

    // ---- VISION tab ----

    private void Vision_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Same InitializeComponent guard as Contrast_TextChanged; initial render is in the constructor.
        if (!IsLoaded)
            return;
        RunVision(ApplyVision);
    }

    private void ApplyVision()
    {
        var c = ColorTool.Parse(VisionBox.Text);
        VisionSwatch.Background = new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B));

        RenderSimulation(ColorVisionKind.Protanopia, ProtanopiaSwatch, ProtanopiaHex, c);
        RenderSimulation(ColorVisionKind.Deuteranopia, DeuteranopiaSwatch, DeuteranopiaHex, c);
        RenderSimulation(ColorVisionKind.Tritanopia, TritanopiaSwatch, TritanopiaHex, c);
        RenderSimulation(ColorVisionKind.Achromatopsia, AchromatopsiaSwatch, AchromatopsiaHex, c);
    }

    private static void RenderSimulation(ColorVisionKind kind, Border swatch, TextBlock hexText, ParsedColor c)
    {
        var simulated = ColorVisionTool.Simulate(c, kind);
        swatch.Background = new SolidColorBrush(Color.FromRgb(simulated.R, simulated.G, simulated.B));
        hexText.Text = ColorTool.ToHex(simulated, alpha: false);
    }

    /// <summary>Runs a VISION-tab update with its own reentrancy guard and error banner.</summary>
    private void RunVision(Action apply)
    {
        if (_visionUpdating)
            return;
        _visionUpdating = true;
        try
        {
            apply();
            VisionErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            VisionErrorText.Text = ex.Message;
            VisionErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            _visionUpdating = false;
        }
    }

    private sealed record HistoryEntry(byte R, byte G, byte B, string Hex)
    {
        public Brush SwatchBrush { get; } = new SolidColorBrush(Color.FromRgb(R, G, B));
    }
}
