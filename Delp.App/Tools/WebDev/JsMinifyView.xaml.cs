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

    private async void Minify_Click(object sender, RoutedEventArgs e)
    {
        var js = _inputEditor.Text;
        MinifyBtn.IsEnabled = false;
        try
        {
            var result = await Task.Run(() => JsTool.Minify(js));
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
