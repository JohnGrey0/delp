using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DataFormat;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.DataFormat;

[Tool("yaml-format", "YAML Formatter", ToolCategory.DataFormat,
    "Reformat YAML into canonical block style and validate it with line/column errors.",
    Keywords = "yaml,format,lint,yml,validate", Order = 40)]
public partial class YamlFormatView : UserControl
{
    private readonly TextEditor _input;
    private readonly TextEditor _output;
    private readonly DispatcherTimer _debounce;
    private bool _busy;

    public YamlFormatView()
    {
        InitializeComponent();

        _input = CodeEditors.Create();
        InputHost.Child = _input;
        _input.TextChanged += (_, _) => DebounceValidate();

        _output = CodeEditors.Create(readOnly: true);
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

    private int Indent =>
        int.Parse((IndentCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "2", CultureInfo.InvariantCulture);

    private async void Format_Click(object sender, RoutedEventArgs e)
    {
        if (_busy)
            return;
        _busy = true;
        FormatBtn.IsEnabled = false;
        var text = _input.Text;
        var indent = Indent;
        try
        {
            var result = await Task.Run(() => YamlFormatTool.Format(text, indent));
            _output.Text = result;
            ShowValid();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            _busy = false;
            FormatBtn.IsEnabled = true;
        }
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            DebounceValidate();
    }

    private void CopyOutput_Click(object sender, RoutedEventArgs e) => Ui.Copy(_output.Text, CopyOutputBtn);

    private async void Validate()
    {
        var text = _input.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            StatusText.Text = "Empty input";
            StatusText.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Fg2");
            return;
        }

        var error = await Task.Run(() => YamlFormatTool.Validate(text));
        if (error is null)
            ShowValid();
        else
            ShowError($"Line {error.Line}, Col {error.Col}: {error.Message}");
    }

    private void ShowValid()
    {
        StatusText.Text = "Valid YAML";
        StatusText.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Success");
    }

    private void ShowError(string message)
    {
        StatusText.Text = message;
        StatusText.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Danger");
    }
}
