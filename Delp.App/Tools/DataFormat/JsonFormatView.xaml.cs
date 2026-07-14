using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DataFormat;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.DataFormat;

[Tool("json-format", "JSON Formatter & Validator", ToolCategory.DataFormat,
    "Format, minify, and validate JSON with precise line and column error reporting.",
    Keywords = "json,format,pretty,minify,validate", Order = 10)]
public partial class JsonFormatView : UserControl
{
    private readonly TextEditor _input;
    private readonly TextEditor _output;
    private readonly DispatcherTimer _debounce;
    private bool _busy;

    public JsonFormatView()
    {
        InitializeComponent();

        _input = CodeEditors.Create("Json");
        InputHost.Child = _input;
        _input.TextChanged += (_, _) => DebounceValidate();

        _output = CodeEditors.Create("Json", readOnly: true);
        OutputHost.Child = _output;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Validate();
        };

        Validate();
    }

    private void DebounceValidate()
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private JsonFormatTool.JsonFormatOptions CurrentOptions()
    {
        var tag = (IndentCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "2";
        var useTabs = tag == "Tab";
        var indentSize = useTabs ? 1 : int.Parse(tag, CultureInfo.InvariantCulture);
        return new JsonFormatTool.JsonFormatOptions(indentSize, useTabs, SortKeysBox.IsChecked == true, EscapeNonAsciiBox.IsChecked == true);
    }

    private async void Format_Click(object sender, RoutedEventArgs e)
    {
        var options = CurrentOptions();
        await RunAsync(() => JsonFormatTool.Format(_input.Text, options));
    }

    private async void Minify_Click(object sender, RoutedEventArgs e) =>
        await RunAsync(() => JsonFormatTool.Minify(_input.Text));

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            DebounceValidate();
    }

    private void CopyOutput_Click(object sender, RoutedEventArgs e) => Ui.Copy(_output.Text, CopyOutputBtn);

    private async Task RunAsync(Func<string> convert)
    {
        if (_busy)
            return;
        _busy = true;
        FormatBtn.IsEnabled = false;
        MinifyBtn.IsEnabled = false;
        try
        {
            var text = _input.Text;
            var result = await Task.Run(convert);
            _output.Text = result;
            ShowValid(text);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            _busy = false;
            FormatBtn.IsEnabled = true;
            MinifyBtn.IsEnabled = true;
        }
    }

    private async void Validate()
    {
        var text = _input.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            StatusText.Text = "Empty input";
            StatusText.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Fg2");
            return;
        }

        var error = await Task.Run(() => JsonFormatTool.Validate(text));
        if (error is null)
            ShowValid(text);
        else
            ShowError($"Line {error.Line}, Col {error.Col}: {error.Message}");
    }

    private void ShowValid(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetByteCount(text);
        StatusText.Text = $"Valid JSON — {FormatSize(bytes)}";
        StatusText.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Success");
    }

    private void ShowError(string message)
    {
        StatusText.Text = message;
        StatusText.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Danger");
    }

    private static string FormatSize(int bytes) =>
        bytes < 1024 ? $"{bytes} B" : $"{bytes / 1024.0:0.#} KB";
}
