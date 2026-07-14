using System.Windows;
using System.Windows.Controls;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.WebDev;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.WebDev;

[Tool("html-minify", "HTML Minifier", ToolCategory.WebDev,
    "Minify HTML, with options for stripping comments and collapsing whitespace.",
    Keywords = "html,minify,compress", Order = 40)]
public partial class HtmlMinifyView : UserControl
{
    private readonly TextEditor _inputEditor;
    private readonly TextEditor _outputEditor;

    public HtmlMinifyView()
    {
        InitializeComponent();

        _inputEditor = CodeEditors.Create();
        InputEditorHost.Child = _inputEditor;

        _outputEditor = CodeEditors.Create(readOnly: true);
        OutputEditorHost.Child = _outputEditor;

        _inputEditor.Text = "<!-- greeting -->\n<div>\n  <p>Hello   World</p>\n</div>\n";
    }

    private async void Minify_Click(object sender, RoutedEventArgs e)
    {
        var html = _inputEditor.Text;
        var options = new HtmlMinifyOptions(
            RemoveComments: RemoveCommentsBox.IsChecked == true,
            CollapseWhitespace: CollapseWhitespaceBox.IsChecked == true);

        MinifyBtn.IsEnabled = false;
        try
        {
            var result = await Task.Run(() => HtmlTool.Minify(html, options));
            _outputEditor.Text = result.Code ?? "";
            MinifierUi.ShowResult(StatusText, ErrorsText, result);
        }
        catch (Exception ex)
        {
            MinifierUi.ShowError(ErrorsText, ex.Message);
        }
        finally
        {
            MinifyBtn.IsEnabled = true;
        }
    }

    private void CopyOutput_Click(object sender, RoutedEventArgs e) => Ui.Copy(_outputEditor.Text, CopyOutputBtn);
}
