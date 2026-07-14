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

    private async void Minify_Click(object sender, RoutedEventArgs e)
    {
        var css = _inputEditor.Text;
        SetBusy(true);
        try
        {
            var result = await Task.Run(() => CssTool.Minify(css));
            _outputEditor.Text = result.Code ?? "";
            MinifierUi.ShowResult(StatusText, ErrorsText, result);
        }
        catch (Exception ex)
        {
            MinifierUi.ShowError(ErrorsText, ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void Beautify_Click(object sender, RoutedEventArgs e)
    {
        var css = _inputEditor.Text;
        SetBusy(true);
        try
        {
            var before = System.Text.Encoding.UTF8.GetByteCount(css);
            var beautified = await Task.Run(() => CssTool.Beautify(css, 2));
            var after = System.Text.Encoding.UTF8.GetByteCount(beautified);
            _outputEditor.Text = beautified;

            var pct = before == 0 ? 0 : Math.Round((1 - (double)after / before) * 100, 1);
            StatusText.Text = MinifierUi.FormatSavings(before, after, pct);
            MinifierUi.ShowErrors(ErrorsText, []);
        }
        catch (Exception ex)
        {
            MinifierUi.ShowError(ErrorsText, ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void CopyOutput_Click(object sender, RoutedEventArgs e) => Ui.Copy(_outputEditor.Text, CopyOutputBtn);

    private void SetBusy(bool busy)
    {
        MinifyBtn.IsEnabled = !busy;
        BeautifyBtn.IsEnabled = !busy;
    }
}
