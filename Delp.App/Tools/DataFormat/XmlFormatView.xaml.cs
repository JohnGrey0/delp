using System.Globalization;
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
    private int _validateToken;
    private int _formatToken;

    public XmlFormatView()
    {
        InitializeComponent();

        _input = CodeEditors.Create();
        _output = CodeEditors.Create(readOnly: true);
        InputHost.Child = _input;
        OutputHost.Child = _output;

        _validateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _validateTimer.Tick += async (_, _) => { _validateTimer.Stop(); await ValidateAsync(); };

        _input.TextChanged += (_, _) =>
        {
            _validateTimer.Stop();
            _validateTimer.Start();
        };

        // Stop pending work when navigated away so a cached-but-hidden view doesn't keep
        // computing/writing to itself; also invalidate any validation/format already in flight.
        Unloaded += (_, _) =>
        {
            _validateTimer.Stop();
            _validateToken++;
            _formatToken++;
        };
    }

    private XmlFormatOptions Options
    {
        get
        {
            var tag = (IndentCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "2";
            var useTabs = tag == "tab";
            var indentSize = useTabs ? 2 : int.Parse(tag, CultureInfo.InvariantCulture);
            return new XmlFormatOptions(indentSize, useTabs, OmitDeclarationBox.IsChecked == true);
        }
    }

    /// <summary>Parsing megabyte-scale XML is noticeable on the UI thread, so validation runs on the thread pool.</summary>
    private async Task ValidateAsync()
    {
        var text = _input.Text;
        if (text.Trim().Length == 0)
        {
            ValidityText.Text = "";
            return;
        }

        var token = ++_validateToken;
        var error = await Task.Run(() => XmlFormatTool.Validate(text));
        if (token != _validateToken)
            return; // superseded by a newer edit while this validation was running

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

    private void Format_Click(object sender, RoutedEventArgs e) => _ = RunFormatAsync();

    private void Minify_Click(object sender, RoutedEventArgs e) => _ = RunAsync(XmlFormatTool.Minify);

    private void IndentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded) _ = RunFormatAsync();
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded) _ = RunFormatAsync();
    }

    private Task RunFormatAsync()
    {
        var options = Options; // read UI state before hopping off the UI thread
        return RunAsync(text => XmlFormatTool.Format(text, options));
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(_output.Text, CopyBtn);

    /// <summary>Runs a conversion on the thread pool (safe for 1MB+ input) with inline error reporting.</summary>
    private async Task RunAsync(Func<string, string> convert)
    {
        var text = _input.Text;
        var token = ++_formatToken;
        try
        {
            var result = await Task.Run(() => convert(text));
            if (token != _formatToken) return;
            _output.Text = result;
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            if (token != _formatToken) return;
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
