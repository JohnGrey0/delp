using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Delp.App.Infrastructure;
using Delp.Core.Tools.WebDev;
using Microsoft.Win32;

namespace Delp.App.Tools.WebDev;

[Tool("placeholder-image", "Placeholder Image Generator", ToolCategory.WebDev,
    "Generate a solid-color placeholder image with a centered size label.",
    Keywords = "placeholder,image,dummy,png,mock", Order = 90)]
public partial class PlaceholderImageView : UserControl
{
    private readonly DispatcherTimer _debounce;
    private byte[]? _currentBytes;

    public PlaceholderImageView()
    {
        InitializeComponent();

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Render();
        };

        Unloaded += (_, _) => _debounce.Stop();

        Render();
    }

    private ImageKind SelectedKind => JpegRadio.IsChecked == true ? ImageKind.Jpeg : ImageKind.Png;

    private void Option_TextChanged(object sender, TextChangedEventArgs e)
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Render();
    }

    private void Render()
    {
        try
        {
            var width = ParseInt(WidthBox.Text, "Width");
            var height = ParseInt(HeightBox.Text, "Height");
            var options = new PlaceholderOptions(
                width, height, BgBox.Text, FgBox.Text,
                string.IsNullOrWhiteSpace(LabelBox.Text) ? null : LabelBox.Text,
                SelectedKind);

            _currentBytes = PlaceholderRenderer.Render(options);

            var bmp = new BitmapImage();
            using (var stream = new MemoryStream(_currentBytes))
            {
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = stream;
                bmp.EndInit();
            }
            bmp.Freeze();
            PreviewImage.Source = bmp;

            BgSwatch.Background = SafeBrush(BgBox.Text);
            FgSwatch.Background = SafeBrush(FgBox.Text);

            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            _currentBytes = null;
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private static int ParseInt(string text, string fieldName)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            throw new FormatException($"{fieldName} must be a whole number.");
        return value;
    }

    private static Brush SafeBrush(string hex)
    {
        try
        {
            PlaceholderTool.ValidateHexColor(hex, "color");
            var s = hex.StartsWith('#') ? hex[1..] : hex;
            if (s.Length == 3)
                s = $"{s[0]}{s[0]}{s[1]}{s[1]}{s[2]}{s[2]}";
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#" + s)!);
        }
        catch
        {
            return Brushes.Transparent;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_currentBytes is null)
            return;

        var ext = SelectedKind == ImageKind.Jpeg ? "jpg" : "png";
        var dialog = new SaveFileDialog
        {
            FileName = $"placeholder.{ext}",
            Filter = SelectedKind == ImageKind.Jpeg ? "JPEG image (*.jpg)|*.jpg" : "PNG image (*.png)|*.png",
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            File.WriteAllBytes(dialog.FileName, _currentBytes);
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
        if (PreviewImage.Source is BitmapSource bitmap)
        {
            try { Clipboard.SetImage(bitmap); }
            catch
            {
                // Clipboard can be transiently locked by another process; ignore.
            }
        }
    }
}
