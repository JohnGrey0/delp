using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Encoding;

namespace Delp.App.Tools.Encoding;

[Tool("html-entities", "HTML Entity Encode / Decode", ToolCategory.Encoding,
    "Escape and unescape HTML entities, with an option to render non-ASCII characters as numeric entities.",
    Keywords = "html,entities,escape,amp,nbsp", Order = 30)]
public partial class HtmlEntitiesView : UserControl
{
    private bool _updating;

    public HtmlEntitiesView()
    {
        InitializeComponent();
    }

    private bool NonAsciiToNumeric => NumericBox.IsChecked == true;

    private void PlainBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => EncodedBox.Text = HtmlEntityTool.Encode(PlainBox.Text, NonAsciiToNumeric));

    private void EncodedBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => PlainBox.Text = HtmlEntityTool.Decode(EncodedBox.Text));

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Run(() => EncodedBox.Text = HtmlEntityTool.Encode(PlainBox.Text, NonAsciiToNumeric));
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
