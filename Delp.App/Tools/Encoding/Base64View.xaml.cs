using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Encoding;

namespace Delp.App.Tools.Encoding;

[Tool("base64", "Base64 Encode / Decode", ToolCategory.Encoding,
    "Convert text to and from Base64, with an optional URL-safe alphabet.",
    Keywords = "base64,b64,encode,decode,binary,ascii", Order = 10)]
public partial class Base64View : UserControl
{
    private bool _updating;

    public Base64View()
    {
        InitializeComponent();
    }

    private bool UrlSafe => UrlSafeBox.IsChecked == true;

    private void PlainBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => EncodedBox.Text = Base64Tool.Encode(PlainBox.Text, UrlSafe));

    private void EncodedBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => PlainBox.Text = Base64Tool.Decode(EncodedBox.Text, UrlSafe));

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Run(() => EncodedBox.Text = Base64Tool.Encode(PlainBox.Text, UrlSafe));
    }

    private void CopyPlain_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(PlainBox.Text, CopyPlainBtn);

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
