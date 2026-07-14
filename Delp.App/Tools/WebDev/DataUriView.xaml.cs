using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Delp.App.Infrastructure;
using Delp.Core.Tools.WebDev;
using Microsoft.Win32;

namespace Delp.App.Tools.WebDev;

[Tool("data-uri", "Data URI Converter", ToolCategory.WebDev,
    "Encode files or text into data: URIs, and decode data URIs back to bytes.",
    Keywords = "datauri,base64,inline,image,mime", Order = 70)]
public partial class DataUriView : UserControl
{
    /// <summary>Cap on decoded preview image width — a preview pane never needs full resolution.</summary>
    private const int PreviewDecodePixelWidth = 480;

    private bool _updating;
    private byte[]? _fileBytes;
    private string? _fileBase64;
    private string? _fileName;
    private byte[]? _decodedBytes;

    public DataUriView()
    {
        InitializeComponent();
        MimeBox.Text = "text/plain";
    }

    private bool IsFileMode => FileModeRadio.IsChecked == true;

    private void Mode_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;
        FilePanel.Visibility = IsFileMode ? Visibility.Visible : Visibility.Collapsed;
        TextInputBox.Visibility = IsFileMode ? Visibility.Collapsed : Visibility.Visible;
        TextBase64Box.Visibility = IsFileMode ? Visibility.Collapsed : Visibility.Visible;
        UpdateEncodedOutput();
    }

    private async void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Choose a file to encode" };
        if (dialog.ShowDialog() == true)
            await LoadFileAsync(dialog.FileName);
    }

    private void FilePanel_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void FilePanel_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
            await LoadFileAsync(files[0]);
    }

    /// <summary>
    /// Reads the file and Base64-encodes it off the UI thread — files dropped here can be
    /// multi-MB, and Base64-encoding is cached so later MIME/option changes don't redo it.
    /// </summary>
    private async Task LoadFileAsync(string path)
    {
        try
        {
            var (bytes, base64) = await Task.Run(() =>
            {
                var b = File.ReadAllBytes(path);
                return (Bytes: b, Base64: Convert.ToBase64String(b));
            });
            _fileBytes = bytes;
            _fileBase64 = base64;
            _fileName = Path.GetFileName(path);
            MimeBox.Text = DataUriTool.GuessMime(Path.GetExtension(path));
            FileNameText.Text = _fileBytes.LongLength > 2 * 1024 * 1024
                ? $"{_fileName} ({FormatSize(_fileBytes.LongLength)}) — large file, the data URI will be huge."
                : $"{_fileName} ({FormatSize(_fileBytes.LongLength)})";
            UpdateOutputCore();
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void TextInput_Changed(object sender, TextChangedEventArgs e) => UpdateEncodedOutput();

    private void Mime_Changed(object sender, TextChangedEventArgs e) => UpdateEncodedOutput();

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            UpdateEncodedOutput();
    }

    private void UpdateEncodedOutput() => Run(UpdateOutputCore);

    private void UpdateOutputCore()
    {
        string uri;
        long originalSize;

        if (IsFileMode)
        {
            if (_fileBytes is null || _fileBase64 is null)
            {
                EncodedBox.Text = "";
                EncodedSizeText.Text = "";
                return;
            }
            // Reuse the cached Base64 payload — the file's bytes haven't changed, only the
            // (cheap) header, so there's no need to re-encode a potentially huge byte array.
            uri = DataUriTool.EncodeFromBase64(_fileBase64, MimeBox.Text);
            originalSize = _fileBytes.LongLength;
        }
        else
        {
            var text = TextInputBox.Text ?? "";
            uri = DataUriTool.EncodeText(text, MimeBox.Text, TextBase64Box.IsChecked == true);
            originalSize = System.Text.Encoding.UTF8.GetByteCount(text);
        }

        EncodedBox.Text = uri;
        if (originalSize == 0)
        {
            EncodedSizeText.Text = $"{uri.Length:N0} characters";
        }
        else
        {
            var pct = (uri.Length - originalSize) * 100.0 / originalSize;
            var sign = pct >= 0 ? "+" : "";
            EncodedSizeText.Text = $"{FormatSize(originalSize)} → {FormatSize(uri.Length)}  ({sign}{pct:F0}% vs original)";
        }
    }

    private void CopyEncoded_Click(object sender, RoutedEventArgs e) => Ui.Copy(EncodedBox.Text, CopyEncodedBtn);

    private void DecodeInput_Changed(object sender, TextChangedEventArgs e) => Run(() =>
    {
        DecodePreviewImage.Source = null;
        SaveAsBtn.IsEnabled = false;
        _decodedBytes = null;

        if (string.IsNullOrWhiteSpace(DecodeInputBox.Text))
        {
            DecodeMimeText.Text = "";
            DecodeSizeText.Text = "";
            return;
        }

        var parts = DataUriTool.Decode(DecodeInputBox.Text);
        _decodedBytes = parts.Data;
        DecodeMimeText.Text = parts.MimeType + (parts.IsBase64 ? "  ·  base64" : "  ·  percent-encoded");
        DecodeSizeText.Text = FormatSize(parts.Data.LongLength);
        SaveAsBtn.IsEnabled = true;

        if (parts.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var stream = new MemoryStream(parts.Data);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                // Cap decode resolution — this is a thumbnail-scale preview, so there's no need
                // to decode a large source image (e.g. a 50 MP photo) at full resolution.
                bmp.DecodePixelWidth = PreviewDecodePixelWidth;
                bmp.StreamSource = stream;
                bmp.EndInit();
                bmp.Freeze();
                DecodePreviewImage.Source = bmp;
            }
            catch
            {
                // Not a renderable raster image (e.g. SVG) — leave the preview blank, never crash.
            }
        }
    });

    private async void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        if (_decodedBytes is null)
            return;

        var dialog = new SaveFileDialog { FileName = "decoded.bin" };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var bytes = _decodedBytes;
            await Task.Run(() => File.WriteAllBytes(dialog.FileName, bytes));
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB",
    };

    /// <summary>Runs a conversion with reentrancy protection and inline error reporting.</summary>
    private void Run(Action action)
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            action();
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
