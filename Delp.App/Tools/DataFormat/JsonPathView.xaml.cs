using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DataFormat;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.DataFormat;

[Tool("jsonpath", "JSONPath & XPath Query", ToolCategory.DataFormat,
    "Query a JSON document with JSONPath or an XML document with XPath, and preview every match.",
    Keywords = "jsonpath,query,json,filter,$..,xpath,xml,xml query,nodes", Order = 20)]
public partial class JsonPathView : UserControl
{
    private readonly TextEditor _input;
    private readonly TextEditor _results;
    private readonly DispatcherTimer _debounce;
    private int _requestId;

    private readonly TextEditor _xpathInput;
    private readonly DispatcherTimer _xpathDebounce;
    private int _xpathRequestId;
    private IReadOnlyList<XPathTool.XPathMatch> _xpathMatches = [];

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

        // XML has no dark-tuned syntax highlighting in this app (only "Json" does — see
        // CONVENTIONS.md), so this editor stays plain rather than rendering unreadable
        // light-theme colors.
        _xpathInput = CodeEditors.Create(null);
        XPathInputHost.Child = _xpathInput;
        _xpathInput.TextChanged += (_, _) => DebounceXPathRun();

        _xpathDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _xpathDebounce.Tick += (_, _) =>
        {
            _xpathDebounce.Stop();
            RunXPath();
        };

        Run();
        RunXPath();
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
            ShowStatus(StatusText, "Empty input", "Brush.Fg2");
            return;
        }
        if (string.IsNullOrWhiteSpace(path))
        {
            ShowStatus(StatusText, "Enter a JSONPath expression", "Brush.Fg2");
            return;
        }

        try
        {
            var result = await Task.Run(() => JsonPathTool.Query(json, path));
            if (requestId != _requestId)
                return; // superseded by a newer edit
            _results.Text = result.ResultJson;
            ShowStatus(StatusText, result.Count == 1 ? "1 match" : $"{result.Count} matches", "Brush.Success");
        }
        catch (Exception ex)
        {
            if (requestId != _requestId)
                return;
            ShowStatus(StatusText, ex.Message, "Brush.Danger");
        }
    }

    // Same crash-guard reasoning as PathBox_TextChanged above: this tab's controls are part of
    // the same InitializeComponent() call, so any XAML default here would fire before _xpathInput
    // / _xpathDebounce are assigned. XPathExprBox has no default Text, so nothing fires today —
    // the guard stays anyway so a future default value can't reintroduce the crash.
    private void XPathExprBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded)
            DebounceXPathRun();
    }

    private void DebounceXPathRun()
    {
        _xpathDebounce.Stop();
        _xpathDebounce.Start();
    }

    private async void RunXPath()
    {
        var xml = _xpathInput.Text;
        var expr = XPathExprBox.Text;
        var requestId = ++_xpathRequestId;

        if (string.IsNullOrWhiteSpace(xml))
        {
            _xpathMatches = [];
            XPathResultsList.ItemsSource = null;
            ShowStatus(XPathStatusText, "Empty input", "Brush.Fg2");
            return;
        }
        if (string.IsNullOrWhiteSpace(expr))
        {
            ShowStatus(XPathStatusText, "Enter an XPath expression", "Brush.Fg2");
            return;
        }

        try
        {
            var result = await Task.Run(() => XPathTool.Evaluate(xml, expr));
            if (requestId != _xpathRequestId)
                return; // superseded by a newer edit

            _xpathMatches = result.Matches;
            XPathResultsList.ItemsSource = result.Matches;

            var label = result.Count == 1 ? "1 match" : $"{result.Count} matches";
            if (result.Truncated)
                label += $" (capped at {result.Count})";
            ShowStatus(XPathStatusText, label, "Brush.Success");
        }
        catch (Exception ex)
        {
            if (requestId != _xpathRequestId)
                return;
            // Same convention as Base64View.Run / the JSONPath tab's Run() above: leave the last
            // successful results in place on error (don't clear _xpathMatches/ItemsSource) — only
            // the error line updates. Otherwise every transient invalid state while live-typing an
            // expression (e.g. an unclosed "[") would blank the results list.
            ShowStatus(XPathStatusText, ex.Message, "Brush.Danger");
        }
    }

    private void CopyXPathResults_Click(object sender, RoutedEventArgs e)
    {
        var text = string.Join(Environment.NewLine, _xpathMatches.Select(m => $"{m.Path}\t{m.Snippet}"));
        Ui.Copy(text, CopyXPathResultsBtn);
    }

    private static void ShowStatus(TextBlock target, string text, string brushKey)
    {
        target.Text = text;
        target.SetResourceReference(TextBlock.ForegroundProperty, brushKey);
    }
}
