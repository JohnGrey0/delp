using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DataFormat;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;

namespace Delp.App.Tools.DataFormat;

/// <summary>FromCombo's item order matches this enum's declaration order 1:1 (index cast) — keep
/// them in sync. <see cref="Auto"/> resolves to a concrete <see cref="TableFormat"/> via
/// <see cref="TableTool.Detect"/> at conversion time.</summary>
public enum TableFromFormat
{
    Auto,
    Csv,
    Tsv,
    Markdown,
    Json,
}

[Tool("table-convert", "Table Converter", ToolCategory.DataFormat,
    "Convert pasted tables between CSV, TSV, Markdown, JSON, HTML, SQL, ASCII, and LaTeX.",
    Keywords = "table,markdown,csv,tsv,excel,ascii,sql,insert,html,latex,convert", Order = 40)]
public partial class TableConverterView : UserControl
{
    private readonly TextEditor _input;
    private readonly TextEditor _output;
    private readonly IHighlightingDefinition? _jsonHighlighting;
    private readonly DispatcherTimer _debounce;

    private TableFormat _lastDetected = TableFormat.Csv;
    private int _requestId;

    public TableConverterView()
    {
        InitializeComponent();

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Convert();
        };

        _input = CodeEditors.Create("Json");
        _jsonHighlighting = _input.SyntaxHighlighting;
        InputHost.Child = _input;
        _input.TextChanged += (_, _) => Debounce();

        _output = CodeEditors.Create(null, readOnly: true);
        OutputHost.Child = _output;

        UpdateAdaptiveVisibility();
        UpdateHighlighting();

        // Stop pending work when navigated away so a cached-but-hidden view doesn't keep computing,
        // and invalidate any conversion already in flight.
        Unloaded += (_, _) =>
        {
            _debounce.Stop();
            _requestId++;
        };

        Convert(); // sets the initial "Empty input" state
    }

    private TableFromFormat FromSelection => (TableFromFormat)FromCombo.SelectedIndex;

    private TableWriteTarget ToTarget => (TableWriteTarget)ToCombo.SelectedIndex;

    private TableFormat EffectiveFromFormat => FromSelection == TableFromFormat.Auto ? _lastDetected : ToTableFormat(FromSelection);

    private char EffectiveDelimiter => DelimiterBox.Text.Length > 0 ? DelimiterBox.Text[0] : ',';

    private void Debounce()
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private void Combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            OnOptionsChanged();
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            OnOptionsChanged();
    }

    private void TextOption_Changed(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded)
            OnOptionsChanged();
    }

    private void OnOptionsChanged()
    {
        UpdateAdaptiveVisibility();
        UpdateHighlighting();
        Debounce();
    }

    private void UpdateAdaptiveVisibility()
    {
        var from = EffectiveFromFormat;
        DelimiterRow.Visibility = from == TableFormat.Csv ? Visibility.Visible : Visibility.Collapsed;
        HeaderBox.Visibility = from == TableFormat.Markdown ? Visibility.Collapsed : Visibility.Visible;

        var to = ToTarget;
        AlignmentRow.Visibility = to == TableWriteTarget.Markdown ? Visibility.Visible : Visibility.Collapsed;
        BordersRow.Visibility = to == TableWriteTarget.AsciiBox ? Visibility.Visible : Visibility.Collapsed;
        TableNameRow.Visibility = to == TableWriteTarget.SqlInsert ? Visibility.Visible : Visibility.Collapsed;
        ShapeRow.Visibility = to == TableWriteTarget.Json ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>The JSON side always uses the dark JSON editor; the other side is plain.</summary>
    private void UpdateHighlighting()
    {
        _input.SyntaxHighlighting = EffectiveFromFormat == TableFormat.Json ? _jsonHighlighting : null;
        _output.SyntaxHighlighting = ToTarget == TableWriteTarget.Json ? _jsonHighlighting : null;
    }

    private void UpdateDetectedText() =>
        DetectedText.Text = FromSelection == TableFromFormat.Auto && !string.IsNullOrWhiteSpace(_input.Text)
            ? $"Detected: {FormatName(_lastDetected)}"
            : "";

    private async void Convert()
    {
        var text = _input.Text;
        var requestId = ++_requestId;

        if (string.IsNullOrWhiteSpace(text))
        {
            SetOutput("");
            HideError();
            UpdateDetectedText();
            return;
        }

        var fromSelection = FromSelection;
        var toTarget = ToTarget;
        var hasHeader = HeaderBox.IsChecked == true;
        var delimiter = EffectiveDelimiter;
        var options = BuildWriteOptions();

        ConvertResult? result = null;
        Exception? failure = null;
        try
        {
            result = await Task.Run(() => ConvertCore(text, fromSelection, toTarget, hasHeader, delimiter, options));
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        if (requestId != _requestId)
            return; // superseded by a newer edit

        if (result is { } r)
        {
            _lastDetected = r.Detected;
            SetOutput(r.Output);
            HideError();
        }
        else if (failure is not null)
        {
            ErrorText.Text = failure.Message;
            ErrorText.Visibility = Visibility.Visible;
        }

        UpdateAdaptiveVisibility();
        UpdateHighlighting();
        UpdateDetectedText();
    }

    private static ConvertResult ConvertCore(string text, TableFromFormat from, TableWriteTarget to, bool hasHeader, char delimiter, TableWriteOptions options)
    {
        var detected = from == TableFromFormat.Auto ? TableTool.Detect(text) : ToTableFormat(from);
        var data = TableTool.Parse(text, detected, hasHeader, delimiter);
        var output = TableTool.Write(data, to, options);
        return new ConvertResult(output, detected);
    }

    private TableWriteOptions BuildWriteOptions() => new(
        Alignment: (MarkdownAlign)Math.Max(0, AlignmentCombo.SelectedIndex),
        Borders: BordersCombo.SelectedIndex == 1 ? AsciiBorderStyle.Unicode : AsciiBorderStyle.Ascii,
        SqlTableName: string.IsNullOrWhiteSpace(TableNameBox.Text) ? "table_name" : TableNameBox.Text.Trim(),
        Shape: ShapeCombo.SelectedIndex == 1 ? JsonTableShape.Arrays : JsonTableShape.Objects);

    private static TableFormat ToTableFormat(TableFromFormat from) => from switch
    {
        TableFromFormat.Csv => TableFormat.Csv,
        TableFromFormat.Tsv => TableFormat.Tsv,
        TableFromFormat.Markdown => TableFormat.Markdown,
        TableFromFormat.Json => TableFormat.Json,
        _ => TableFormat.Csv, // Auto is resolved by the caller via TableTool.Detect before reaching here
    };

    private static string FormatName(TableFormat format) => format switch
    {
        TableFormat.Csv => "CSV",
        TableFormat.Tsv => "TSV",
        TableFormat.Markdown => "Markdown",
        TableFormat.Json => "JSON",
        _ => format.ToString(),
    };

    private void SetOutput(string text) => _output.Text = text;

    private void HideError() => ErrorText.Visibility = Visibility.Collapsed;

    private void CopyInput_Click(object sender, RoutedEventArgs e) => Ui.Copy(_input.Text, CopyInputBtn);

    private void CopyOutput_Click(object sender, RoutedEventArgs e) => Ui.Copy(_output.Text, CopyOutputBtn);

    private readonly record struct ConvertResult(string Output, TableFormat Detected);
}
