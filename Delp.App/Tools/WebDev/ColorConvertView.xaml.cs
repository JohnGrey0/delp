using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Delp.App.Infrastructure;
using Delp.Core.Tools.WebDev;

namespace Delp.App.Tools.WebDev;

[Tool("color-convert", "Color Converter", ToolCategory.WebDev,
    "Convert colors between hex, RGB, HSL, and HSB, with a live swatch and contrast check.",
    Keywords = "color,hex,rgb,hsl,hsb,hsv,css", Order = 10)]
public partial class ColorConvertView : UserControl
{
    private static readonly ParsedColor White = new(255, 255, 255, 255);
    private static readonly ParsedColor Black = new(0, 0, 0, 255);

    private bool _updating;

    public ColorConvertView()
    {
        InitializeComponent();
        Run(ApplyFromInput);
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
}
