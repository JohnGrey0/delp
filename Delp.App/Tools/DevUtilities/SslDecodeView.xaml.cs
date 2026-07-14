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
    // Bumped by every user action that changes what should be on screen (paste-PEM decode or a
    // host fetch). A fetch's async result is only applied if this still matches the id it captured
    // when the fetch started — otherwise a slow fetch could land after (and clobber) whatever the
    // user has since typed into the PASTE PEM tab, or a newer fetch's result.
    private int _requestId;

    public SslDecodeView()
    {
        InitializeComponent();
    }

    private void PemBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _requestId++;
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

        var requestId = ++_requestId;
        FetchBtn.IsEnabled = false;
        var originalContent = FetchBtn.Content;
        FetchBtn.Content = "Fetching…";
        try
        {
            var certs = await CertTool.FetchFromHostAsync(host, port);
            if (requestId == _requestId)
            {
                CertsList.ItemsSource = certs;
                HideError();
            }
        }
        catch (Exception ex)
        {
            if (requestId == _requestId)
            {
                CertsList.ItemsSource = null;
                ShowError(ex.Message);
            }
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
