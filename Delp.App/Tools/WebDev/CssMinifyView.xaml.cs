using System.Windows;
using System.Windows.Controls;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.WebDev;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.WebDev;

[Tool("css-minify", "CSS Minifier / Beautifier", ToolCategory.WebDev,
    "Minify or beautify CSS, with before/after size savings.",
    Keywords = "css,minify,beautify,format", Order = 20)]
public partial class CssMinifyView : UserControl
{
    private readonly TextEditor _inputEditor;
    private readonly TextEditor _outputEditor;

    public CssMinifyView()
    {
        InitializeComponent();

        _inputEditor = CodeEditors.Create();
        InputEditorHost.Child = _inputEditor;

        _outputEditor = CodeEditors.Create(readOnly: true);
        OutputEditorHost.Child = _outputEditor;

        _inputEditor.Text = ".card {\n  display: flex;\n  padding: 12px 16px;\n}\n";
    }

    private void Minify_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = CssTool.Minify(_inputEditor.Text);
            _outputEditor.Text = result.Code ?? "";
            ShowStatus(result.BeforeBytes, result.AfterBytes);
            ShowErrors(result.Errors);
        }
        catch (Exception ex)
        {
            ShowErrors([ex.Message]);
        }
    }

    private void Beautify_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var before = System.Text.Encoding.UTF8.GetByteCount(_inputEditor.Text);
            var beautified = CssTool.Beautify(_inputEditor.Text, 2);
            _outputEditor.Text = beautified;
            ShowStatus(before, System.Text.Encoding.UTF8.GetByteCount(beautified));
            ShowErrors([]);
        }
        catch (Exception ex)
        {
            ShowErrors([ex.Message]);
        }
    }

    private void CopyOutput_Click(object sender, RoutedEventArgs e) => Ui.Copy(_outputEditor.Text, CopyOutputBtn);

    private void ShowStatus(int before, int after)
    {
        var pct = before == 0 ? 0 : Math.Round((1 - (double)after / before) * 100, 1);
        var sign = pct >= 0 ? "-" : "+";
        StatusText.Text = $"{FormatBytes(before)} → {FormatBytes(after)} ({sign}{Math.Abs(pct):0.0}%)";
    }

    private void ShowErrors(IReadOnlyList<string> errors)
    {
        if (errors.Count == 0)
        {
            ErrorsText.Visibility = Visibility.Collapsed;
            return;
        }

        ErrorsText.Text = string.Join("\n", errors);
        ErrorsText.Visibility = Visibility.Visible;
    }

    private static string FormatBytes(int bytes) => bytes >= 1024 ? $"{bytes / 1024.0:0.0} KB" : $"{bytes} B";
}
