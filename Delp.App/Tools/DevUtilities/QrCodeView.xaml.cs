using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("qr-code", "QR Code Generator", ToolCategory.DevUtilities,
    "Generate a QR code from text, a URL, or a Wi-Fi network, with adjustable size and error correction.",
    Keywords = "qr,qrcode,barcode,wifi,link", Order = 10)]
public partial class QrCodeView : UserControl
{
    // QR encoding (QRCodeGenerator + PNG rasterization) is too expensive to run on every keystroke; debounce it.
    private readonly DispatcherTimer _debounce = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private byte[]? _pngBytes;

    public QrCodeView()
    {
        InitializeComponent();
        AuthCombo.SelectedIndex = 0;
        EccCombo.SelectedIndex = 1; // M
        SizeCombo.SelectedIndex = 1; // Medium
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Render();
        };
        Loaded += (_, _) => Render();
    }

    private bool IsWifiTab => SourceTabs.SelectedIndex == 1;

    private void Input_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Debounce();
    }

    private void Source_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
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
            var content = IsWifiTab ? BuildWifiPayload() : ContentBox.Text;

            var level = ((EccCombo.SelectedItem as ComboBoxItem)?.Tag as string) switch
            {
                "L" => QrEccLevel.L,
                "Q" => QrEccLevel.Q,
                "H" => QrEccLevel.H,
                _ => QrEccLevel.M,
            };

            var pixelsPerModule = int.TryParse((SizeCombo.SelectedItem as ComboBoxItem)?.Tag as string, out var px)
                ? px
                : 10;

            _pngBytes = QrTool.CreatePng(content, pixelsPerModule, level);
            QrImage.Source = ToBitmapImage(_pngBytes);

            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            _pngBytes = null;
            QrImage.Source = null;
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private string BuildWifiPayload()
    {
        var auth = ((AuthCombo.SelectedItem as ComboBoxItem)?.Tag as string) switch
        {
            "Wep" => WifiAuth.Wep,
            "None" => WifiAuth.None,
            _ => WifiAuth.Wpa,
        };
        return QrTool.WifiPayload(SsidBox.Text, PasswordBox.Text, auth, HiddenBox.IsChecked == true);
    }

    private static BitmapImage ToBitmapImage(byte[] bytes)
    {
        var image = new BitmapImage();
        using var stream = new MemoryStream(bytes);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_pngBytes is null)
            return;

        var dialog = new SaveFileDialog { Filter = "PNG image (*.png)|*.png", FileName = "qrcode.png" };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            File.WriteAllBytes(dialog.FileName, _pngBytes);
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void CopyImage_Click(object sender, RoutedEventArgs e)
    {
        if (QrImage.Source is BitmapSource source)
            Clipboard.SetImage(source);
    }
}
