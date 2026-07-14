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

    public CsvJsonView()
    {
        InitializeComponent();

        _csv = CodeEditors.Create();
        _json = CodeEditors.Create("Json");
        CsvHost.Child = _csv;
        JsonHost.Child = _json;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) => { _debounce.Stop(); Convert(); };

        _csv.TextChanged += (_, _) => Schedule(fromCsv: true);
        _json.TextChanged += (_, _) => Schedule(fromCsv: false);
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

    private void Convert()
    {
        if (_fromCsv)
            Run(() => _json.Text = CsvJsonTool.CsvToJson(_csv.Text, Options));
        else
            Run(() => _csv.Text = CsvJsonTool.JsonToCsv(_json.Text, SelectedDelimiter ?? ','));
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        try
        {
            if (JsonNode.Parse(_json.Text) is not JsonArray array || array.Count == 0)
            {
                StatusText.Text = "";
                return;
            }

            var columns = new HashSet<string>();
            foreach (var item in array)
                if (item is JsonObject obj)
                    foreach (var kv in obj)
                        columns.Add(kv.Key);

            StatusText.Text = $"{array.Count} rows × {columns.Count} columns";
        }
        catch
        {
            StatusText.Text = "";
        }
    }

    private void CopyCsv_Click(object sender, RoutedEventArgs e) => Ui.Copy(_csv.Text, CopyCsvBtn);

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
