using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Delp.App.Infrastructure;
using Delp.Core.Tools.WebDev;

namespace Delp.App.Tools.WebDev;

[Tool("color-blotter", "Screen Color Picker", ToolCategory.WebDev,
    "Pick any pixel's color from anywhere on screen with an eyedropper overlay.",
    Keywords = "eyedropper,color picker,screen,pixel,blotter", Order = 15)]
public partial class ColorBlotterView : UserControl
{
    private const int MaxHistory = 24;
    private readonly ObservableCollection<HistoryEntry> _history = [];

    public ColorBlotterView()
    {
        InitializeComponent();
        HistoryList.ItemsSource = _history;
        ShowColor(10, 132, 255);
    }

    private void PickButton_Click(object sender, RoutedEventArgs e)
    {
        var overlay = new ColorPickerOverlayWindow { Owner = Window.GetWindow(this) };
        overlay.ShowDialog();
        if (overlay.Picked)
            SelectColor(overlay.PickedR, overlay.PickedG, overlay.PickedB);
    }

    private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HistoryList.SelectedItem is HistoryEntry entry)
            ShowColor(entry.R, entry.G, entry.B);
    }

    /// <summary>Displays a color and records it as a new history entry (a fresh pick).</summary>
    private void SelectColor(byte r, byte g, byte b)
    {
        ShowColor(r, g, b);

        var formats = ScreenColorTool.Formats(r, g, b);
        var entry = new HistoryEntry(r, g, b, formats.Hex, formats.Rgb, formats.Hsl);
        if (_history.Count == 0 || _history[0].Hex != entry.Hex)
        {
            _history.Insert(0, entry);
            while (_history.Count > MaxHistory)
                _history.RemoveAt(_history.Count - 1);
        }
    }

    /// <summary>Updates the current-color card only (no history mutation).</summary>
    private void ShowColor(byte r, byte g, byte b)
    {
        var formats = ScreenColorTool.Formats(r, g, b);
        CurrentSwatch.Background = new SolidColorBrush(Color.FromRgb(r, g, b));
        HexBox.Text = formats.Hex;
        RgbBox.Text = formats.Rgb;
        HslBox.Text = formats.Hsl;
    }

    private void CopyHex_Click(object sender, RoutedEventArgs e) => Ui.Copy(HexBox.Text, (Button)sender);
    private void CopyRgb_Click(object sender, RoutedEventArgs e) => Ui.Copy(RgbBox.Text, (Button)sender);
    private void CopyHsl_Click(object sender, RoutedEventArgs e) => Ui.Copy(HslBox.Text, (Button)sender);

    private void CopyHistoryItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: HistoryEntry entry } button)
            Ui.Copy(entry.Hex, button);
    }

    private sealed record HistoryEntry(byte R, byte G, byte B, string Hex, string Rgb, string Hsl)
    {
        public Brush SwatchBrush { get; } = new SolidColorBrush(Color.FromRgb(R, G, B));
    }
}
