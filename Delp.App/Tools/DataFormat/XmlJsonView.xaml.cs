using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DataFormat;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.DataFormat;

[Tool("xml-json", "XML ↔ JSON Converter", ToolCategory.DataFormat,
    "Convert between XML and JSON using an attribute, text and repeated-element mapping.",
    Keywords = "xml,json,convert", Order = 70)]
public partial class XmlJsonView : UserControl
{
    private readonly TextEditor _xml;
    private readonly TextEditor _json;
    private readonly DispatcherTimer _debounce;
    private bool _updating;
    private bool _fromXml = true;
    private int _token;

    public XmlJsonView()
    {
        InitializeComponent();

        _xml = CodeEditors.Create();
        _json = CodeEditors.Create("Json");
        XmlHost.Child = _xml;
        JsonHost.Child = _json;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += async (_, _) => { _debounce.Stop(); await ConvertAsync(); };

        _xml.TextChanged += (_, _) => Schedule(fromXml: true);
        _json.TextChanged += (_, _) => Schedule(fromXml: false);

        // Stop pending work when navigated away so a cached-but-hidden view doesn't keep
        // computing/writing to itself; also invalidate any conversion already in flight.
        Unloaded += (_, _) =>
        {
            _debounce.Stop();
            _token++;
        };
    }

    private void Schedule(bool fromXml)
    {
        if (_updating) return;
        _fromXml = fromXml;
        _debounce.Stop();
        _debounce.Start();
    }

    private void RootName_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Root name only matters for JSON -> XML; re-run that direction so the change is reflected.
        if (IsLoaded) Schedule(fromXml: false);
    }

    /// <summary>Parsing/re-serializing megabyte-scale documents runs off the UI thread so typing stays responsive.</summary>
    private async Task ConvertAsync()
    {
        var fromXml = _fromXml;
        var xmlText = _xml.Text;
        var jsonText = _json.Text;
        var rootName = string.IsNullOrWhiteSpace(RootNameBox.Text) ? "root" : RootNameBox.Text;
        var token = ++_token;

        string result;
        try
        {
            result = await Task.Run(() => fromXml
                ? XmlJsonTool.XmlToJson(xmlText)
                : XmlJsonTool.JsonToXml(jsonText, rootName));
        }
        catch (Exception ex)
        {
            if (token != _token) return;
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        if (token != _token) return; // superseded by a newer edit while this conversion was running

        _updating = true; // suppress the TextChanged this write triggers on the box we're about to fill
        try
        {
            if (fromXml) _json.Text = result; else _xml.Text = result;
            ErrorText.Visibility = Visibility.Collapsed;
        }
        finally
        {
            _updating = false;
        }
    }

    private void CopyXml_Click(object sender, RoutedEventArgs e) => Ui.Copy(_xml.Text, CopyXmlBtn);

    private void CopyJson_Click(object sender, RoutedEventArgs e) => Ui.Copy(_json.Text, CopyJsonBtn);
}
