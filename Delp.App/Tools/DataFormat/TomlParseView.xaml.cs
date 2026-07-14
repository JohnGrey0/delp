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
    private bool _settingToml;
    private bool _settingJson;
    private Action? _pendingConvert;
    private int _requestId;

    public TomlParseView()
    {
        InitializeComponent();

        _toml = CodeEditors.Create();
        TomlHost.Child = _toml;
        _toml.TextChanged += (_, _) => { if (!_settingToml) Debounce(ConvertTomlToJson); };

        _json = CodeEditors.Create("Json");
        JsonHost.Child = _json;
        _json.TextChanged += (_, _) => { if (!_settingJson) Debounce(ConvertJsonToToml); };

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            _pendingConvert?.Invoke();
        };
    }

    // Only genuine edits reach here (see the _settingToml/_settingJson guards above) — this is
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

    private async void ConvertTomlToJson()
    {
        var text = _toml.Text;
        var requestId = ++_requestId;
        if (string.IsNullOrWhiteSpace(text))
        {
            SetJson("");
            HideError();
            return;
        }
        await RunAsync(() => TomlTool.TomlToJson(text), requestId, SetJson, "TOML");
    }

    private async void ConvertJsonToToml()
    {
        var text = _json.Text;
        var requestId = ++_requestId;
        if (string.IsNullOrWhiteSpace(text))
        {
            SetToml("");
            HideError();
            return;
        }
        await RunAsync(() => TomlTool.JsonToToml(text), requestId, SetToml, "JSON");
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

    private void SetToml(string text)
    {
        _settingToml = true;
        _toml.Text = text;
        _settingToml = false;
    }

    private void SetJson(string text)
    {
        _settingJson = true;
        _json.Text = text;
        _settingJson = false;
    }

    private void HideError() => ErrorText.Visibility = Visibility.Collapsed;

    private void CopyToml_Click(object sender, RoutedEventArgs e) => Ui.Copy(_toml.Text, CopyTomlBtn);

    private void CopyJson_Click(object sender, RoutedEventArgs e) => Ui.Copy(_json.Text, CopyJsonBtn);
}
