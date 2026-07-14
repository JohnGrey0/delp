using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DataFormat;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.DataFormat;

[Tool("csv-json", "CSV ↔ JSON Converter", ToolCategory.DataFormat,
    "Convert between CSV and JSON, with delimiter auto-detection and type inference.",
    Keywords = "csv,json,convert,delimiter,tsv", Order = 80)]
public partial class CsvJsonView : UserControl
{
    private readonly TextEditor _csv;
    private readonly TextEditor _json;
    private readonly DispatcherTimer _debounce;
    private bool _updating;
    private bool _fromCsv = true;
    private int _token;

    public CsvJsonView()
    {
        InitializeComponent();

        _csv = CodeEditors.Create();
        _json = CodeEditors.Create("Json");
        CsvHost.Child = _csv;
        JsonHost.Child = _json;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += async (_, _) => { _debounce.Stop(); await ConvertAsync(); };

        _csv.TextChanged += (_, _) => Schedule(fromCsv: true);
        _json.TextChanged += (_, _) => Schedule(fromCsv: false);

        // Stop pending work when navigated away so a cached-but-hidden view doesn't keep
        // computing/writing to itself; also invalidate any conversion already in flight.
        Unloaded += (_, _) =>
        {
            _debounce.Stop();
            _token++;
        };
    }

    private char? SelectedDelimiter =>
        (DelimiterCombo.SelectedItem as ComboBoxItem)?.Tag as string is { Length: 1 } s ? s[0] : null;

    private CsvOptions Options => new(SelectedDelimiter, HeaderBox.IsChecked == true, InferTypesBox.IsChecked == true);

    private void Schedule(bool fromCsv)
    {
        if (_updating) return;
        _fromCsv = fromCsv;
        _debounce.Stop();
        _debounce.Start();
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded) Schedule(_fromCsv);
    }

    /// <summary>Parsing/re-serializing megabyte-scale CSV or JSON runs off the UI thread so typing stays responsive.</summary>
    private async Task ConvertAsync()
    {
        var fromCsv = _fromCsv;
        var options = Options; // read UI state before hopping off the UI thread
        var delimiter = SelectedDelimiter ?? ',';
        var csvText = _csv.Text;
        var jsonText = _json.Text;
        var token = ++_token;

        string result;
        string status;
        try
        {
            (result, status) = await Task.Run(() =>
            {
                if (fromCsv)
                {
                    var json = CsvJsonTool.CsvToJson(csvText, options);
                    return (json, ComputeStatus(json));
                }
                var csv = CsvJsonTool.JsonToCsv(jsonText, delimiter);
                return (csv, ComputeStatus(jsonText));
            });
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
            if (fromCsv) _json.Text = result; else _csv.Text = result;
            ErrorText.Visibility = Visibility.Collapsed;
        }
        finally
        {
            _updating = false;
        }
        StatusText.Text = status;
    }

    /// <summary>Pure, off-UI-thread-safe status computation; "" when the JSON isn't a non-empty array.</summary>
    private static string ComputeStatus(string json)
    {
        try
        {
            if (JsonNode.Parse(json) is not JsonArray array || array.Count == 0)
                return "";

            var columns = new HashSet<string>();
            foreach (var item in array)
                if (item is JsonObject obj)
                    foreach (var kv in obj)
                        columns.Add(kv.Key);

            return $"{array.Count} rows × {columns.Count} columns";
        }
        catch
        {
            return "";
        }
    }

    private void CopyCsv_Click(object sender, RoutedEventArgs e) => Ui.Copy(_csv.Text, CopyCsvBtn);

    private void CopyJson_Click(object sender, RoutedEventArgs e) => Ui.Copy(_json.Text, CopyJsonBtn);
}
