using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DataFormat;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.DataFormat;

[Tool("xml-format", "XML Formatter & Validator", ToolCategory.DataFormat,
    "Format, minify and validate XML documents, safely rejecting embedded DTDs.",
    Keywords = "xml,format,pretty,minify,validate", Order = 60)]
public partial class XmlFormatView : UserControl
{
    private readonly TextEditor _input;
    private readonly TextEditor _output;
    private readonly DispatcherTimer _validateTimer;

    public XmlFormatView()
    {
        InitializeComponent();

        _input = CodeEditors.Create();
        _output = CodeEditors.Create(readOnly: true);
        InputHost.Child = _input;
        OutputHost.Child = _output;

        _validateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _validateTimer.Tick += (_, _) => { _validateTimer.Stop(); Validate(); };

        _input.TextChanged += (_, _) =>
        {
            _validateTimer.Stop();
            _validateTimer.Start();
        };
    }

    private XmlFormatOptions Options
    {
        get
        {
            var tag = (IndentCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "2";
            var useTabs = tag == "tab";
            var indentSize = useTabs ? 2 : int.Parse(tag);
            return new XmlFormatOptions(indentSize, useTabs, OmitDeclarationBox.IsChecked == true);
        }
    }

    private void Validate()
    {
        if (_input.Text.Trim().Length == 0)
        {
            ValidityText.Text = "";
            return;
        }

        var error = XmlFormatTool.Validate(_input.Text);
        if (error is null)
        {
            ValidityText.Text = "Valid XML";
            ValidityText.Foreground = (Brush)FindResource("Brush.Success");
        }
        else
        {
            ValidityText.Text = $"Line {error.Line}, Col {error.Col}: {error.Message}";
            ValidityText.Foreground = (Brush)FindResource("Brush.Danger");
        }
    }

    private void Format_Click(object sender, RoutedEventArgs e) => RunFormat();

    private void Minify_Click(object sender, RoutedEventArgs e) =>
        Run(() => _output.Text = XmlFormatTool.Minify(_input.Text));

    private void IndentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded) RunFormat();
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded) RunFormat();
    }

    private void RunFormat() => Run(() => _output.Text = XmlFormatTool.Format(_input.Text, Options));

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
