using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Encoding;

namespace Delp.App.Tools.Encoding;

[Tool("unicode-escape", "Unicode Escape / Unescape", ToolCategory.Encoding,
    "Convert text to and from \\uXXXX, \\UXXXXXXXX, and \\xNN escape sequences.",
    Keywords = "unicode,escape,codepoint,\\u", Order = 50)]
public partial class UnicodeEscapeView : UserControl
{
    private bool _updating;

    public UnicodeEscapeView()
    {
        InitializeComponent();
    }

    private bool NonAsciiOnly => NonAsciiOnlyBox.IsChecked == true;

    private void PlainBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => EscapedBox.Text = UnicodeEscapeTool.Escape(PlainBox.Text, NonAsciiOnly));

    private void EscapedBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => PlainBox.Text = UnicodeEscapeTool.Unescape(EscapedBox.Text));

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Run(() => EscapedBox.Text = UnicodeEscapeTool.Escape(PlainBox.Text, NonAsciiOnly));
    }

    private void CopyPlain_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(PlainBox.Text, CopyPlainBtn);

    private void CopyEscaped_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(EscapedBox.Text, CopyEscapedBtn);

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
