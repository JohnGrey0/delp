using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DataFormat;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.DataFormat;

[Tool("sql-format", "SQL Formatter", ToolCategory.DataFormat,
    "Format or minify SQL with a dependency-free, dialect-agnostic layout engine.",
    Keywords = "sql,format,pretty,query,minify", Order = 90)]
public partial class SqlFormatView : UserControl
{
    private readonly TextEditor _input;
    private readonly TextEditor _output;
    private int _token;

    public SqlFormatView()
    {
        InitializeComponent();

        _input = CodeEditors.Create();
        _output = CodeEditors.Create(readOnly: true);
        InputHost.Child = _input;
        OutputHost.Child = _output;

        // Invalidate any format/minify already in flight when navigated away, so a cached-but-hidden
        // view doesn't write a stale result to itself later.
        Unloaded += (_, _) => _token++;
    }

    private SqlFormatOptions Options => new(
        UppercaseKeywords: UppercaseBox.IsChecked == true,
        IndentSize: int.Parse((string)((ComboBoxItem)IndentCombo.SelectedItem).Tag, CultureInfo.InvariantCulture));

    private void Format_Click(object sender, RoutedEventArgs e)
    {
        var options = Options; // read UI state before hopping off the UI thread
        _ = RunAsync(text => SqlFormatTool.Format(text, options));
    }

    private void Minify_Click(object sender, RoutedEventArgs e) => _ = RunAsync(SqlFormatTool.Minify);

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(_output.Text, CopyBtn);

    /// <summary>Runs a conversion on the thread pool (safe for 1MB+ input) with inline error reporting.</summary>
    private async Task RunAsync(Func<string, string> convert)
    {
        var text = _input.Text;
        var token = ++_token;
        try
        {
            var result = await Task.Run(() => convert(text));
            if (token != _token) return;
            _output.Text = result;
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            if (token != _token) return;
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
