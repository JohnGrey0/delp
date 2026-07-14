using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DataFormat;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.DataFormat;

[Tool("jsonpath", "JSONPath Query", ToolCategory.DataFormat,
    "Query a JSON document with JSONPath expressions and preview every match.",
    Keywords = "jsonpath,query,json,filter,$..", Order = 20)]
public partial class JsonPathView : UserControl
{
    private readonly TextEditor _input;
    private readonly TextEditor _results;
    private readonly DispatcherTimer _debounce;
    private int _requestId;

    public JsonPathView()
    {
        InitializeComponent();

        _input = CodeEditors.Create("Json");
        InputHost.Child = _input;
        _input.TextChanged += (_, _) => DebounceRun();

        _results = CodeEditors.Create("Json", readOnly: true);
        ResultsHost.Child = _results;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Run();
        };

        Run();
    }

    // Guarded like Base64View.Option_Changed: PathBox's XAML default Text="$" fires this
    // TextChanged handler during InitializeComponent, before _debounce (and _input/_results)
    // are assigned in the constructor — without the IsLoaded check this throws a
    // NullReferenceException on every construction. The constructor's explicit Run() call
    // after field initialization covers the initial state instead.
    private void PathBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded)
            DebounceRun();
    }

    private void CopyResults_Click(object sender, RoutedEventArgs e) => Ui.Copy(_results.Text, CopyResultsBtn);

    private void DebounceRun()
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private async void Run()
    {
        var json = _input.Text;
        var path = PathBox.Text;
        var requestId = ++_requestId;

        if (string.IsNullOrWhiteSpace(json))
        {
            _results.Text = "";
            ShowStatus("Empty input", "Brush.Fg2");
            return;
        }
        if (string.IsNullOrWhiteSpace(path))
        {
            ShowStatus("Enter a JSONPath expression", "Brush.Fg2");
            return;
        }

        try
        {
            var result = await Task.Run(() => JsonPathTool.Query(json, path));
            if (requestId != _requestId)
                return; // superseded by a newer edit
            _results.Text = result.ResultJson;
            ShowStatus(result.Count == 1 ? "1 match" : $"{result.Count} matches", "Brush.Success");
        }
        catch (Exception ex)
        {
            if (requestId != _requestId)
                return;
            ShowStatus(ex.Message, "Brush.Danger");
        }
    }

    private void ShowStatus(string text, string brushKey)
    {
        StatusText.Text = text;
        StatusText.SetResourceReference(TextBlock.ForegroundProperty, brushKey);
    }
}
