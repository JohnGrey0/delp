using System.Windows;
using System.Windows.Controls;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.WebDev;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.WebDev;

[Tool("js-minify", "JavaScript Minifier", ToolCategory.WebDev,
    "Minify JavaScript, with before/after size savings and inline parse errors.",
    Keywords = "javascript,js,minify,uglify", Order = 30)]
public partial class JsMinifyView : UserControl
{
    private readonly TextEditor _inputEditor;
    private readonly TextEditor _outputEditor;

    public JsMinifyView()
    {
        InitializeComponent();

        _inputEditor = CodeEditors.Create();
        InputEditorHost.Child = _inputEditor;

        _outputEditor = CodeEditors.Create(readOnly: true);
        OutputEditorHost.Child = _outputEditor;

        _inputEditor.Text = "function greet(name) {\n  return \"Hello, \" + name + \"!\";\n}\n";
    }

    private void Minify_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = JsTool.Minify(_inputEditor.Text);
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
