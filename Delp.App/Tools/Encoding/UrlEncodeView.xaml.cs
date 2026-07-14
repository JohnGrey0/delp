using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Encoding;

namespace Delp.App.Tools.Encoding;

[Tool("url-encode", "URL Percent-Encoding", ToolCategory.Encoding,
    "Percent-encode and decode text for URLs, with component, form-data, and reserved-char-preserving modes.",
    Keywords = "url,percent,escape,uri,querystring", Order = 20)]
public partial class UrlEncodeView : UserControl
{
    private bool _updating;

    public UrlEncodeView()
    {
        InitializeComponent();
    }

    private UrlEncodeMode Mode =>
        FormDataMode.IsChecked == true ? UrlEncodeMode.FormData :
        PreserveMode.IsChecked == true ? UrlEncodeMode.PreserveUriChars :
        UrlEncodeMode.Component;

    private void DecodedBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => EncodedBox.Text = UrlEncodeTool.Encode(DecodedBox.Text, Mode));

    private void EncodedBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => DecodedBox.Text = UrlEncodeTool.Decode(EncodedBox.Text, Mode));

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Run(() => EncodedBox.Text = UrlEncodeTool.Encode(DecodedBox.Text, Mode));
    }

    private void CopyDecoded_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(DecodedBox.Text, CopyDecodedBtn);

    private void CopyEncoded_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(EncodedBox.Text, CopyEncodedBtn);

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
