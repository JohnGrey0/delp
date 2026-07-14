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

    public XmlJsonView()
    {
        InitializeComponent();

        _xml = CodeEditors.Create();
        _json = CodeEditors.Create("Json");
        XmlHost.Child = _xml;
        JsonHost.Child = _json;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) => { _debounce.Stop(); Convert(); };

        _xml.TextChanged += (_, _) => Schedule(fromXml: true);
        _json.TextChanged += (_, _) => Schedule(fromXml: false);
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

    private void Convert()
    {
        if (_fromXml)
            Run(() => _json.Text = XmlJsonTool.XmlToJson(_xml.Text));
        else
            Run(() => _xml.Text = XmlJsonTool.JsonToXml(_json.Text, string.IsNullOrWhiteSpace(RootNameBox.Text) ? "root" : RootNameBox.Text));
    }

    private void CopyXml_Click(object sender, RoutedEventArgs e) => Ui.Copy(_xml.Text, CopyXmlBtn);

    private void CopyJson_Click(object sender, RoutedEventArgs e) => Ui.Copy(_json.Text, CopyJsonBtn);

    /// <summary>Runs a conversion with reentrancy protection and inline error reporting.</summary>
    private void Run(Action convert)
    {
        if (_updating) return;
        _updating = true;
        try
        {
            convert();
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            _updating = false;
        }
    }
}
