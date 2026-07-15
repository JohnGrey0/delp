using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DataFormat;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;

namespace Delp.App.Tools.DataFormat;

/// <summary>
/// Supported formats. FromCombo/ToCombo item order matches this enum's declaration
/// order 1:1 (index cast), so keep them in sync.
/// </summary>
public enum DataFormatKind
{
    Json,
    Yaml,
    Xml,
    Csv,
    Toml,
}

[Tool("convert-data", "Data Converter", ToolCategory.DataFormat,
    "Convert documents between JSON, YAML, XML, CSV and TOML, pivoting through JSON.",
    Keywords = "json,yaml,xml,csv,toml,convert,yml,tsv,delimiter,config,parse,validate,cargo,json-yaml,xml-json,csv-json,toml-parse",
    Order = 20)]
public partial class DataConverterView : UserControl
{
    private readonly TextEditor _top;
    private readonly TextEditor _bottom;
    private readonly IHighlightingDefinition? _jsonHighlighting;
    private readonly DispatcherTimer _debounce;
    private bool _settingTop;
    private bool _settingBottom;
    private bool _syncingCombos;
    private bool _lastDirectionTopToBottom = true;
    private Action? _pendingConvert;
    private int _requestId;

    public DataConverterView()
    {
        InitializeComponent();

        _top = CodeEditors.Create("Json");
        _bottom = CodeEditors.Create("Json");
        _jsonHighlighting = _top.SyntaxHighlighting;
        TopHost.Child = _top;
        BottomHost.Child = _bottom;

        _top.TextChanged += (_, _) => { if (!_settingTop) Debounce(ConvertTopToBottom); };
        _bottom.TextChanged += (_, _) => { if (!_settingBottom) Debounce(ConvertBottomToTop); };

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            _pendingConvert?.Invoke();
        };

        // Defaults (JSON -> YAML) are already a supported pair, so just sync the
        // presentation — no need to run the invariant check or a conversion.
        UpdateSectionLabels();
        UpdateAdaptiveOptionsVisibility();
        UpdateHighlighting();

        // Stop pending work when navigated away so a cached-but-hidden view doesn't keep
        // computing/writing to itself; also invalidate any conversion already in flight.
        Unloaded += (_, _) =>
        {
            _debounce.Stop();
            _requestId++;
        };
    }

    private DataFormatKind FromFormat => (DataFormatKind)FromCombo.SelectedIndex;
    private DataFormatKind ToFormat => (DataFormatKind)ToCombo.SelectedIndex;

    private char? SelectedDelimiter =>
        (DelimiterCombo.SelectedItem as ComboBoxItem)?.Tag as string is { Length: 1 } s ? s[0] : null;

    private CsvOptions CsvOptionsValue => new(SelectedDelimiter, HeaderBox.IsChecked == true, InferTypesBox.IsChecked == true);

    private string EffectiveRootName => string.IsNullOrWhiteSpace(RootNameBox.Text) ? "root" : RootNameBox.Text;

    // Only genuine edits reach here (see the _settingTop/_settingBottom guards above) — this is
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

    private void FromCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingCombos || !IsLoaded) return;
        EnforceJsonInvariant(changedIsFrom: true);
        OnFormatsChanged();
    }

    private void ToCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingCombos || !IsLoaded) return;
        EnforceJsonInvariant(changedIsFrom: false);
        OnFormatsChanged();
    }

    /// <summary>Supported pairs are exactly JSON⇄YAML, JSON⇄XML, JSON⇄CSV, JSON⇄TOML — one side
    /// must always be JSON. If the just-changed combo left both sides non-JSON, snap the OTHER
    /// side back to JSON and explain why with a status note.</summary>
    private void EnforceJsonInvariant(bool changedIsFrom)
    {
        if (FromFormat == DataFormatKind.Json || ToFormat == DataFormatKind.Json)
        {
            SnapNoteText.Visibility = Visibility.Collapsed;
            return;
        }

        var attemptedFrom = FromFormat;
        var attemptedTo = ToFormat;

        _syncingCombos = true;
        if (changedIsFrom) ToCombo.SelectedIndex = (int)DataFormatKind.Json;
        else FromCombo.SelectedIndex = (int)DataFormatKind.Json;
        _syncingCombos = false;

        SnapNoteText.Text =
            $"{FormatName(attemptedFrom)} → {FormatName(attemptedTo)} isn't supported directly — " +
            $"{(changedIsFrom ? "TO" : "FROM")} reset to JSON.";
        SnapNoteText.Visibility = Visibility.Visible;
    }

    private void OnFormatsChanged()
    {
        UpdateSectionLabels();
        UpdateAdaptiveOptionsVisibility();
        UpdateHighlighting();
        Debounce(_lastDirectionTopToBottom ? ConvertTopToBottom : ConvertBottomToTop);
    }

    private void RootName_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Root name only matters for JSON -> XML; re-run the last live direction so it's reflected.
        if (IsLoaded) Debounce(_lastDirectionTopToBottom ? ConvertTopToBottom : ConvertBottomToTop);
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded) Debounce(_lastDirectionTopToBottom ? ConvertTopToBottom : ConvertBottomToTop);
    }

    private void Swap_Click(object sender, RoutedEventArgs e)
    {
        var newFromIndex = ToCombo.SelectedIndex;
        var newToIndex = FromCombo.SelectedIndex;
        var topText = _top.Text;
        var bottomText = _bottom.Text;

        _syncingCombos = true;
        FromCombo.SelectedIndex = newFromIndex;
        ToCombo.SelectedIndex = newToIndex;
        _syncingCombos = false;

        // A swap of an already-valid pair is always still valid, so no invariant check needed.
        SnapNoteText.Visibility = Visibility.Collapsed;

        SetTop(bottomText);
        SetBottom(topText);
        HideError();

        UpdateSectionLabels();
        UpdateAdaptiveOptionsVisibility();
        UpdateHighlighting();
        UpdateCsvStatus();
    }

    private void UpdateSectionLabels()
    {
        TopLabel.Text = FormatName(FromFormat);
        BottomLabel.Text = FormatName(ToFormat);
    }

    private void UpdateAdaptiveOptionsVisibility()
    {
        var xmlInvolved = FromFormat == DataFormatKind.Xml || ToFormat == DataFormatKind.Xml;
        var csvInvolved = FromFormat == DataFormatKind.Csv || ToFormat == DataFormatKind.Csv;
        var tomlInvolved = FromFormat == DataFormatKind.Toml || ToFormat == DataFormatKind.Toml;

        RootNameRow.Visibility = xmlInvolved ? Visibility.Visible : Visibility.Collapsed;
        CsvOptionsRow.Visibility = csvInvolved ? Visibility.Visible : Visibility.Collapsed;
        TomlNote.Visibility = tomlInvolved ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>The JSON side always uses the dark JSON editor; the other side is plain.</summary>
    private void UpdateHighlighting()
    {
        _top.SyntaxHighlighting = FromFormat == DataFormatKind.Json ? _jsonHighlighting : null;
        _bottom.SyntaxHighlighting = ToFormat == DataFormatKind.Json ? _jsonHighlighting : null;
    }

    private async void ConvertTopToBottom()
    {
        _lastDirectionTopToBottom = true;
        var text = _top.Text;
        var from = FromFormat;
        var to = ToFormat;
        var rootName = EffectiveRootName;
        var delimiter = SelectedDelimiter ?? ',';
        var csvOptions = CsvOptionsValue;
        var requestId = ++_requestId;
        if (string.IsNullOrWhiteSpace(text))
        {
            SetBottom("");
            HideError();
            UpdateCsvStatus();
            return;
        }
        await RunAsync(() => ConvertText(from, to, text, rootName, delimiter, csvOptions), requestId, ApplyBottom, FormatName(from));
    }

    private async void ConvertBottomToTop()
    {
        _lastDirectionTopToBottom = false;
        var text = _bottom.Text;
        var from = ToFormat;
        var to = FromFormat;
        var rootName = EffectiveRootName;
        var delimiter = SelectedDelimiter ?? ',';
        var csvOptions = CsvOptionsValue;
        var requestId = ++_requestId;
        if (string.IsNullOrWhiteSpace(text))
        {
            SetTop("");
            HideError();
            UpdateCsvStatus();
            return;
        }
        await RunAsync(() => ConvertText(from, to, text, rootName, delimiter, csvOptions), requestId, ApplyTop, FormatName(from));
    }

    /// <summary>Dispatches to exactly the Core calls the four absorbed tools used — one side of
    /// every pair is always JSON. Same-format pairs (e.g. a stray JSON -> JSON selection) pass
    /// the text through unchanged.</summary>
    private static string ConvertText(DataFormatKind from, DataFormatKind to, string text, string rootName, char delimiter, CsvOptions csvOptions)
    {
        if (from == to) return text;
        return (from, to) switch
        {
            (DataFormatKind.Json, DataFormatKind.Yaml) => JsonYamlTool.JsonToYaml(text),
            (DataFormatKind.Yaml, DataFormatKind.Json) => JsonYamlTool.YamlToJson(text),
            (DataFormatKind.Json, DataFormatKind.Xml) => XmlJsonTool.JsonToXml(text, rootName),
            (DataFormatKind.Xml, DataFormatKind.Json) => XmlJsonTool.XmlToJson(text),
            (DataFormatKind.Json, DataFormatKind.Csv) => CsvJsonTool.JsonToCsv(text, delimiter),
            (DataFormatKind.Csv, DataFormatKind.Json) => CsvJsonTool.CsvToJson(text, csvOptions),
            (DataFormatKind.Json, DataFormatKind.Toml) => TomlTool.JsonToToml(text),
            (DataFormatKind.Toml, DataFormatKind.Json) => TomlTool.TomlToJson(text),
            _ => throw new FormatException($"{FormatName(from)} → {FormatName(to)} isn't supported directly — pivot through JSON."),
        };
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

    private void ApplyTop(string text)
    {
        SetTop(text);
        UpdateCsvStatus();
    }

    private void ApplyBottom(string text)
    {
        SetBottom(text);
        UpdateCsvStatus();
    }

    private void SetTop(string text)
    {
        _settingTop = true;
        _top.Text = text;
        _settingTop = false;
    }

    private void SetBottom(string text)
    {
        _settingBottom = true;
        _bottom.Text = text;
        _settingBottom = false;
    }

    /// <summary>Row/column count status, shown only while CSV is one of the two selected formats.</summary>
    private void UpdateCsvStatus()
    {
        if (FromFormat != DataFormatKind.Csv && ToFormat != DataFormatKind.Csv)
        {
            StatusText.Text = "";
            return;
        }

        var jsonText = FromFormat == DataFormatKind.Json ? _top.Text
            : ToFormat == DataFormatKind.Json ? _bottom.Text
            : "";
        StatusText.Text = ComputeCsvStatus(jsonText);
    }

    /// <summary>Pure, off-UI-thread-safe status computation; "" when the JSON isn't a non-empty array.</summary>
    private static string ComputeCsvStatus(string json)
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

    private void HideError() => ErrorText.Visibility = Visibility.Collapsed;

    private static string FormatName(DataFormatKind format) => format switch
    {
        DataFormatKind.Json => "JSON",
        DataFormatKind.Yaml => "YAML",
        DataFormatKind.Xml => "XML",
        DataFormatKind.Csv => "CSV",
        DataFormatKind.Toml => "TOML",
        _ => format.ToString(),
    };

    private void CopyTop_Click(object sender, RoutedEventArgs e) => Ui.Copy(_top.Text, CopyTopBtn);

    private void CopyBottom_Click(object sender, RoutedEventArgs e) => Ui.Copy(_bottom.Text, CopyBottomBtn);
}
