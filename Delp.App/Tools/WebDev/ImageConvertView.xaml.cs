using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Delp.App.Infrastructure;
using Microsoft.Win32;

namespace Delp.App.Tools.WebDev;

[Tool("image-convert", "Image Converter", ToolCategory.WebDev,
    "Convert, resize, rotate, and inspect images, and export multi-size favicons.",
    Keywords = "image,convert,resize,png,jpeg,webp,ico,favicon,exif,compress", Order = 95)]
public partial class ImageConvertView : UserControl
{
    private bool _updatingResizeBoxes;
    private bool _busy;
    private string? _fileName;
    private long _originalFileSize;
    private BitmapSource? _currentBitmap;
    private BitmapMetadata? _metadata;
    private ImageInfo? _info;

    public ImageConvertView()
    {
        InitializeComponent();
    }

    private bool HasImage => _currentBitmap is not null;

    // ---- Load ----

    private void DropCard_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = !_busy && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void DropCard_Drop(object sender, DragEventArgs e)
    {
        // _busy already serializes load/transform/save/favicon against each other so a second
        // drop can never land on top of state a still-running operation is about to overwrite —
        // but silently swallowing the drop (the previous behavior) left the user with no idea
        // why nothing happened. Say so instead.
        if (_busy)
        {
            ShowError("Still processing the previous image — try again once it finishes.");
            return;
        }
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
            await LoadAsync(files[0]);
    }

    private async void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose an image",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.ico;*.webp;*.heic;*.heif|All files|*.*",
        };
        if (dialog.ShowDialog() == true)
            await LoadAsync(dialog.FileName);
    }

    private async Task LoadAsync(string path)
    {
        if (_busy)
            return;
        _busy = true;
        SetBusyState(true);
        try
        {
            var loaded = await Task.Run(() => ImageConvertSupport.Load(path));
            _fileName = Path.GetFileName(path);
            _originalFileSize = loaded.Info.FileSizeBytes;
            _currentBitmap = loaded.Bitmap;
            _metadata = loaded.Metadata;
            _info = loaded.Info;

            PreviewImage.Source = _currentBitmap;
            PreviewHintText.Visibility = Visibility.Collapsed;
            SetResizeBoxes(_currentBitmap.PixelWidth, _currentBitmap.PixelHeight);
            RefreshInfoList();
            SizeChangeText.Text = "";
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            _busy = false;
            SetBusyState(false);
        }
    }

    private void RefreshInfoList()
    {
        if (_info is null || _currentBitmap is null)
        {
            InfoList.ItemsSource = null;
            return;
        }

        var rows = new List<InfoRow>
        {
            new("File", _fileName ?? ""),
            new("Format", _info.Format),
            new("Dimensions", $"{_currentBitmap.PixelWidth} × {_currentBitmap.PixelHeight}"),
            new("DPI", $"{_info.DpiX:0.#} × {_info.DpiY:0.#}"),
            new("Frames", _info.FrameCount.ToString(CultureInfo.InvariantCulture)),
            new("File size", FormatSize(_originalFileSize)),
        };

        if (_info.CameraModel is { } camera)
            rows.Add(new InfoRow("Camera", camera));
        if (_info.DateTaken is { } taken)
            rows.Add(new InfoRow("Taken", taken.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)));
        if (_info.Orientation is { } orientation)
            rows.Add(new InfoRow("Orientation", orientation.ToString(CultureInfo.InvariantCulture)));
        rows.Add(new InfoRow("GPS data", _info.HasGps ? "Present" : "None"));

        InfoList.ItemsSource = rows;
    }

    // ---- Resize ----

    private void SetResizeBoxes(int width, int height)
    {
        _updatingResizeBoxes = true;
        ResizeWidthBox.Text = width.ToString(CultureInfo.InvariantCulture);
        ResizeHeightBox.Text = height.ToString(CultureInfo.InvariantCulture);
        _updatingResizeBoxes = false;
    }

    private void ResizeWidthBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingResizeBoxes || !HasImage || LockAspectBox.IsChecked != true)
            return;
        if (!int.TryParse(ResizeWidthBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var width) || width <= 0)
            return;

        var height = (int)Math.Round(width * (_currentBitmap!.PixelHeight / (double)_currentBitmap.PixelWidth));
        _updatingResizeBoxes = true;
        ResizeHeightBox.Text = Math.Max(1, height).ToString(CultureInfo.InvariantCulture);
        _updatingResizeBoxes = false;
    }

    private void ResizeHeightBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingResizeBoxes || !HasImage || LockAspectBox.IsChecked != true)
            return;
        if (!int.TryParse(ResizeHeightBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var height) || height <= 0)
            return;

        var width = (int)Math.Round(height * (_currentBitmap!.PixelWidth / (double)_currentBitmap.PixelHeight));
        _updatingResizeBoxes = true;
        ResizeWidthBox.Text = Math.Max(1, width).ToString(CultureInfo.InvariantCulture);
        _updatingResizeBoxes = false;
    }

    private void PercentPreset_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || !HasImage)
            return;
        if (PercentPresetBox.SelectedItem is not ComboBoxItem { Content: string text } || !text.EndsWith('%'))
            return;
        if (!int.TryParse(text.TrimEnd('%'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var percent))
            return;

        var width = Math.Max(1, (int)Math.Round(_currentBitmap!.PixelWidth * percent / 100.0));
        var height = Math.Max(1, (int)Math.Round(_currentBitmap.PixelHeight * percent / 100.0));
        SetResizeBoxes(width, height);
        PercentPresetBox.SelectedIndex = 0;
    }

    private async void ApplyResize_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || !HasImage)
            return;
        if (!int.TryParse(ResizeWidthBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var width) || width <= 0)
        {
            ShowError("Width must be a positive whole number.");
            return;
        }
        if (!int.TryParse(ResizeHeightBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var height) || height <= 0)
        {
            ShowError("Height must be a positive whole number.");
            return;
        }

        await TransformAsync(bitmap => ImageConvertSupport.Resize(bitmap, width, height));
    }

    // ---- Rotate / flip ----

    private async void RotateLeft_Click(object sender, RoutedEventArgs e) =>
        await TransformAsync(bitmap => ImageConvertSupport.Rotate(bitmap, 270));

    private async void RotateRight_Click(object sender, RoutedEventArgs e) =>
        await TransformAsync(bitmap => ImageConvertSupport.Rotate(bitmap, 90));

    private async void Rotate180_Click(object sender, RoutedEventArgs e) =>
        await TransformAsync(bitmap => ImageConvertSupport.Rotate(bitmap, 180));

    private async void FlipH_Click(object sender, RoutedEventArgs e) =>
        await TransformAsync(bitmap => ImageConvertSupport.Flip(bitmap, FlipDirection.Horizontal));

    private async void FlipV_Click(object sender, RoutedEventArgs e) =>
        await TransformAsync(bitmap => ImageConvertSupport.Flip(bitmap, FlipDirection.Vertical));

    private async Task TransformAsync(Func<BitmapSource, BitmapSource> transform)
    {
        if (_busy || _currentBitmap is null)
            return;
        _busy = true;
        SetBusyState(true);
        try
        {
            var source = _currentBitmap;
            var result = await Task.Run(() => transform(source));
            _currentBitmap = result;
            PreviewImage.Source = result;
            SetResizeBoxes(result.PixelWidth, result.PixelHeight);
            if (_info is not null)
            {
                // Resize re-renders at 96 DPI regardless of the source's DPI (see
                // ImageConvertSupport.Resize), so the INFO panel's DPI row must track the
                // transformed bitmap too — otherwise it keeps showing the pre-transform DPI.
                _info = _info with { Width = result.PixelWidth, Height = result.PixelHeight, DpiX = result.DpiX, DpiY = result.DpiY };
                RefreshInfoList();
            }
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            _busy = false;
            SetBusyState(false);
        }
    }

    // ---- Format / save ----

    private ImageOutputFormat SelectedFormat =>
        ((FormatBox.SelectedItem as ComboBoxItem)?.Content as string) switch
        {
            "JPEG" => ImageOutputFormat.Jpeg,
            "BMP" => ImageOutputFormat.Bmp,
            "GIF" => ImageOutputFormat.Gif,
            "TIFF" => ImageOutputFormat.Tiff,
            _ => ImageOutputFormat.Png,
        };

    private void Format_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (QualityPanel is null)
            return;
        QualityPanel.Visibility = SelectedFormat == ImageOutputFormat.Jpeg ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || _currentBitmap is null)
            return;

        var format = SelectedFormat;
        var quality = ParseQuality();
        var (ext, filter) = format switch
        {
            ImageOutputFormat.Jpeg => ("jpg", "JPEG image|*.jpg"),
            ImageOutputFormat.Bmp => ("bmp", "BMP image|*.bmp"),
            ImageOutputFormat.Gif => ("gif", "GIF image|*.gif"),
            ImageOutputFormat.Tiff => ("tif", "TIFF image|*.tif"),
            _ => ("png", "PNG image|*.png"),
        };

        var dialog = new SaveFileDialog { FileName = $"converted.{ext}", Filter = filter };
        if (dialog.ShowDialog() != true)
            return;

        _busy = true;
        SetBusyState(true);
        try
        {
            var bitmap = _currentBitmap;
            var metadata = _metadata;
            var strip = StripMetadataBox.IsChecked == true;
            var bytes = await Task.Run(() => ImageConvertSupport.Encode(bitmap, format, quality, metadata, strip));
            await Task.Run(() => File.WriteAllBytes(dialog.FileName, bytes));

            SizeChangeText.Text = $"{FormatSize(_originalFileSize)} → {FormatSize(bytes.LongLength)}";
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            _busy = false;
            SetBusyState(false);
        }
    }

    private async void ExportFavicon_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || _currentBitmap is null)
            return;

        var dialog = new SaveFileDialog { FileName = "favicon.ico", Filter = "Icon|*.ico" };
        if (dialog.ShowDialog() != true)
            return;

        _busy = true;
        SetBusyState(true);
        try
        {
            var bitmap = _currentBitmap;
            var bytes = await Task.Run(() => ImageConvertSupport.ExportFavicon(bitmap));
            await Task.Run(() => File.WriteAllBytes(dialog.FileName, bytes));

            SizeChangeText.Text = $"Favicon saved — {FormatSize(bytes.LongLength)} ({ImageConvertSupport.FaviconSizes.Length} sizes)";
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            _busy = false;
            SetBusyState(false);
        }
    }

    private int ParseQuality()
    {
        if (!int.TryParse(QualityBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quality))
            return 90;
        return Math.Clamp(quality, 1, 100);
    }

    private void SetBusyState(bool busy)
    {
        BrowseBtn.IsEnabled = !busy;
        ApplyResizeBtn.IsEnabled = !busy;
        SaveAsBtn.IsEnabled = !busy;
        ExportFaviconBtn.IsEnabled = !busy;
        RotateLeftBtn.IsEnabled = !busy;
        RotateRightBtn.IsEnabled = !busy;
        Rotate180Btn.IsEnabled = !busy;
        FlipHBtn.IsEnabled = !busy;
        FlipVBtn.IsEnabled = !busy;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB",
    };

    private sealed record InfoRow(string Label, string Value);
}
