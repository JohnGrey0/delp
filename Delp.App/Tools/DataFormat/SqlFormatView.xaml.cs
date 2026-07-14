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

    public SqlFormatView()
    {
        InitializeComponent();

        _input = CodeEditors.Create();
        _output = CodeEditors.Create(readOnly: true);
        InputHost.Child = _input;
        OutputHost.Child = _output;
    }

    private SqlFormatOptions Options => new(
        UppercaseKeywords: UppercaseBox.IsChecked == true,
        IndentSize: int.Parse((string)((ComboBoxItem)IndentCombo.SelectedItem).Tag));

    private void Format_Click(object sender, RoutedEventArgs e) =>
        Run(() => _output.Text = SqlFormatTool.Format(_input.Text, Options));

    private void Minify_Click(object sender, RoutedEventArgs e) =>
        Run(() => _output.Text = SqlFormatTool.Minify(_input.Text));

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(_output.Text, CopyBtn);

    /// <summary>Runs a conversion with inline error reporting.</summary>
    private void Run(Action convert)
    {
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
    }
}
