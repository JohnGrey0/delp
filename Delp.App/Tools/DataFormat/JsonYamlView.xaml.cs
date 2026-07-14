using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DataFormat;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.DataFormat;

[Tool("json-yaml", "JSON ↔ YAML Converter", ToolCategory.DataFormat,
    "Convert documents between JSON and YAML, preserving scalar types both ways.",
    Keywords = "json,yaml,convert,yml", Order = 30)]
public partial class JsonYamlView : UserControl
{
    private readonly TextEditor _json;
    private readonly TextEditor _yaml;
    private readonly DispatcherTimer _debounce;
    private bool _updating;
    private Action? _pendingConvert;

    public JsonYamlView()
    {
        InitializeComponent();

        _json = CodeEditors.Create("Json");
        JsonHost.Child = _json;
        _json.TextChanged += (_, _) => Debounce(ConvertJsonToYaml);

        _yaml = CodeEditors.Create();
        YamlHost.Child = _yaml;
        _yaml.TextChanged += (_, _) => Debounce(ConvertYamlToJson);

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

    private async void ConvertJsonToYaml()
    {
        var text = _json.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            _updating = true;
            _yaml.Text = "";
            _updating = false;
            HideError();
            return;
        }
        await RunAsync(() => JsonYamlTool.JsonToYaml(text), result => _yaml.Text = result, "JSON");
    }

    private async void ConvertYamlToJson()
    {
        var text = _yaml.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            _updating = true;
            _json.Text = "";
            _updating = false;
            HideError();
            return;
        }
        await RunAsync(() => JsonYamlTool.YamlToJson(text), result => _json.Text = result, "YAML");
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

    private void CopyJson_Click(object sender, RoutedEventArgs e) => Ui.Copy(_json.Text, CopyJsonBtn);

    private void CopyYaml_Click(object sender, RoutedEventArgs e) => Ui.Copy(_yaml.Text, CopyYamlBtn);
}
