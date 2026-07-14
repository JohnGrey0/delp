using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DataFormat;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.DataFormat;

[Tool("toml-parse", "TOML Parser", ToolCategory.DataFormat,
    "Convert between TOML and JSON, with validation and diagnostic line/column info.",
    Keywords = "toml,config,parse,validate,cargo", Order = 50)]
public partial class TomlParseView : UserControl
{
    private readonly TextEditor _toml;
    private readonly TextEditor _json;
    private readonly DispatcherTimer _debounce;
    private bool _updating;
    private Action? _pendingConvert;

    public TomlParseView()
    {
        InitializeComponent();

        _toml = CodeEditors.Create();
        TomlHost.Child = _toml;
        _toml.TextChanged += (_, _) => Debounce(ConvertTomlToJson);

        _json = CodeEditors.Create("Json");
        JsonHost.Child = _json;
        _json.TextChanged += (_, _) => Debounce(ConvertJsonToToml);

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            _pendingConvert?.Invoke();
        };
    }

    private void Debounce(Action convert)
    {
        if (_updating)
            return;
        _pendingConvert = convert;
        _debounce.Stop();
        _debounce.Start();
    }

    private async void ConvertTomlToJson()
    {
        var text = _toml.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            _updating = true;
            _json.Text = "";
            _updating = false;
            HideError();
            return;
        }
        await RunAsync(() => TomlTool.TomlToJson(text), result => _json.Text = result, "TOML");
    }

    private async void ConvertJsonToToml()
    {
        var text = _json.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            _updating = true;
            _toml.Text = "";
            _updating = false;
            HideError();
            return;
        }
        await RunAsync(() => TomlTool.JsonToToml(text), result => _toml.Text = result, "JSON");
    }

    private async Task RunAsync(Func<string> convert, Action<string> apply, string sourceLabel)
    {
        _updating = true;
        try
        {
            var result = await Task.Run(convert);
            apply(result);
            HideError();
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"{sourceLabel}: {ex.Message}";
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            _updating = false;
        }
    }

    private void HideError() => ErrorText.Visibility = Visibility.Collapsed;

    private void CopyToml_Click(object sender, RoutedEventArgs e) => Ui.Copy(_toml.Text, CopyTomlBtn);

    private void CopyJson_Click(object sender, RoutedEventArgs e) => Ui.Copy(_json.Text, CopyJsonBtn);
}
