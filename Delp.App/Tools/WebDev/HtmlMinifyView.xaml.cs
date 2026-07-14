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

    private void Minify_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new HtmlMinifyOptions(
                RemoveComments: RemoveCommentsBox.IsChecked == true,
                CollapseWhitespace: CollapseWhitespaceBox.IsChecked == true);

            var result = HtmlTool.Minify(_inputEditor.Text, options);
            _outputEditor.Text = result.Code ?? "";

            var pct = result.BeforeBytes == 0 ? 0 : Math.Round((1 - (double)result.AfterBytes / result.BeforeBytes) * 100, 1);
            var sign = pct >= 0 ? "-" : "+";
            StatusText.Text = $"{FormatBytes(result.BeforeBytes)} → {FormatBytes(result.AfterBytes)} ({sign}{Math.Abs(pct):0.0}%)";

            if (result.Errors.Count == 0)
            {
                ErrorsText.Visibility = Visibility.Collapsed;
            }
            else
            {
                ErrorsText.Text = string.Join("\n", result.Errors);
                ErrorsText.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            ErrorsText.Text = ex.Message;
            ErrorsText.Visibility = Visibility.Visible;
        }
    }

    private void CopyOutput_Click(object sender, RoutedEventArgs e) => Ui.Copy(_outputEditor.Text, CopyOutputBtn);

    private static string FormatBytes(int bytes) => bytes >= 1024 ? $"{bytes / 1024.0:0.0} KB" : $"{bytes} B";
}
