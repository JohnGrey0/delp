using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("ssl-decode", "SSL/TLS Certificate Decoder", ToolCategory.DevUtilities,
    "Decode X.509 certificates from pasted PEM/DER, or fetch and inspect a live host's certificate chain over TLS.",
    Keywords = "ssl,tls,certificate,x509,pem,expiry", Order = 100)]
public partial class SslDecodeView : UserControl
{
    public SslDecodeView()
    {
        InitializeComponent();
    }

    private void PemBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var text = PemBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            CertsList.ItemsSource = null;
            HideError();
            return;
        }

        try
        {
            CertsList.ItemsSource = CertTool.DecodePem(text);
            HideError();
        }
        catch (Exception ex)
        {
            CertsList.ItemsSource = null;
            ShowError(ex.Message);
        }
    }

    private async void Fetch_Click(object sender, RoutedEventArgs e)
    {
        var host = HostBox.Text.Trim();
        if (host.Length == 0)
        {
            ShowError("Enter a host name.");
            return;
        }

        if (!int.TryParse(PortBox.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var port) ||
            port is < 1 or > 65535)
        {
            ShowError("Port must be a number between 1 and 65535.");
            return;
        }

        FetchBtn.IsEnabled = false;
        var originalContent = FetchBtn.Content;
        FetchBtn.Content = "Fetching…";
        try
        {
            var certs = await CertTool.FetchFromHostAsync(host, port);
            CertsList.ItemsSource = certs;
            HideError();
        }
        catch (Exception ex)
        {
            CertsList.ItemsSource = null;
            ShowError(ex.Message);
        }
        finally
        {
            FetchBtn.IsEnabled = true;
            FetchBtn.Content = originalContent;
        }
    }

    private void CopyField_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string value } button)
            Ui.Copy(value, button);
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void HideError() => ErrorText.Visibility = Visibility.Collapsed;
}
