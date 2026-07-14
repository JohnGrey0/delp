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
    private bool _settingJson;
    private bool _settingYaml;
    private Action? _pendingConvert;
    private int _requestId;

    public JsonYamlView()
    {
        InitializeComponent();

        _json = CodeEditors.Create("Json");
        JsonHost.Child = _json;
        _json.TextChanged += (_, _) => { if (!_settingJson) Debounce(ConvertJsonToYaml); };

        _yaml = CodeEditors.Create();
        YamlHost.Child = _yaml;
        _yaml.TextChanged += (_, _) => { if (!_settingYaml) Debounce(ConvertYamlToJson); };

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            _pendingConvert?.Invoke();
        };
    }

    // Only genuine edits reach here (see the _settingJson/_settingYaml guards above) — this is
    // never invoked by our own programmatic write of a conversion result to the other editor,
    // so there is no feedback loop. Unlike a plain "if busy, drop the request" guard, this always
    // (re)schedules, so an edit typed while a previous (slow, large-document) conversion is still
    // in flight is not silently lost — RunAsync's request-id check below discards only outdated
    // *results*, never queued *edits*.
    private void Debounce(Action convert)
    {
        _pendingConvert = convert;
        _debounce.Stop();
        _debounce.Start();
    }

    private async void ConvertJsonToYaml()
    {
        var text = _json.Text;
        var requestId = ++_requestId;
        if (string.IsNullOrWhiteSpace(text))
        {
            SetYaml("");
            HideError();
            return;
        }
        await RunAsync(() => JsonYamlTool.JsonToYaml(text), requestId, SetYaml, "JSON");
    }

    private async void ConvertYamlToJson()
    {
        var text = _yaml.Text;
        var requestId = ++_requestId;
        if (string.IsNullOrWhiteSpace(text))
        {
            SetJson("");
            HideError();
            return;
        }
        await RunAsync(() => JsonYamlTool.YamlToJson(text), requestId, SetJson, "YAML");
    }

    /// <summary>Runs a conversion off the UI thread and applies its result, unless a newer edit
    /// has superseded it (<paramref name="requestId"/> vs. <see cref="_requestId"/>) — this keeps
    /// out-of-order completions of overlapping conversions from clobbering a fresher result.</summary>
    private async Task RunAsync(Func<string> convert, int requestId, Action<string> apply, string sourceLabel)
    {
        try
        {
            var result = await Task.Run(convert);
            if (requestId != _requestId)
                return;
            apply(result);
            HideError();
        }
        catch (Exception ex)
        {
            if (requestId != _requestId)
                return;
            ErrorText.Text = $"{sourceLabel}: {ex.Message}";
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void SetJson(string text)
    {
        _settingJson = true;
        _json.Text = text;
        _settingJson = false;
    }

    private void SetYaml(string text)
    {
        _settingYaml = true;
        _yaml.Text = text;
        _settingYaml = false;
    }

    private void HideError() => ErrorText.Visibility = Visibility.Collapsed;

    private void CopyJson_Click(object sender, RoutedEventArgs e) => Ui.Copy(_json.Text, CopyJsonBtn);

    private void CopyYaml_Click(object sender, RoutedEventArgs e) => Ui.Copy(_yaml.Text, CopyYamlBtn);
}
