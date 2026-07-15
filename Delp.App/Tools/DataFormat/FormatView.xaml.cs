using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DataFormat;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.DataFormat;

/// <summary>
/// Merges json-format, yaml-format, xml-format, sql-format and graphql-format into one
/// language-selectable formatter/validator. Core dispatch, options and validation semantics
/// are unchanged per language — only the view is shared.
/// </summary>
[Tool("format", "Formatter & Validator", ToolCategory.DataFormat,
    "Format, minify and validate JSON, YAML, XML, SQL, and GraphQL in one editor.",
    Keywords = "json,yaml,xml,sql,graphql,format,pretty,minify,validate,lint,yml,query,schema,gql," +
        "json-format,yaml-format,xml-format,sql-format,graphql-format", Order = 10)]
public partial class FormatView : UserControl
{
    private enum Lang { Json, Yaml, Xml, Sql, GraphQl }

    private readonly DispatcherTimer _debounce;
    private TextEditor _input = null!;
    private TextEditor _output = null!;
    private Lang _language = Lang.Json;
    private bool _switching;
    private bool _busy;
    private int _validateToken;
    private int _formatToken;

    public FormatView()
    {
        InitializeComponent();

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Validate();
        };

        // Stop pending work when navigated away so a cached-but-hidden view doesn't keep
        // computing/writing to itself; also invalidate any validation/format already in flight.
        Unloaded += (_, _) =>
        {
            _debounce.Stop();
            _validateToken++;
            _formatToken++;
        };

        SetLanguage(Lang.Json);
    }

    // ---- Lang switching ------------------------------------------------------------

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;
        var content = (LanguageCombo.SelectedItem as ComboBoxItem)?.Content as string ?? "JSON";
        SetLanguage(ParseLanguage(content));
    }

    private static Lang ParseLanguage(string content) => content switch
    {
        "YAML" => Lang.Yaml,
        "XML" => Lang.Xml,
        "SQL" => Lang.Sql,
        "GraphQL" => Lang.GraphQl,
        _ => Lang.Json,
    };

    /// <summary>
    /// Switches the active language: recreates the editors (JSON syntax highlighting only
    /// applies when JSON is selected — CodeEditors.Create has no way to re-highlight an
    /// existing instance, so a fresh editor pair is the clean way to avoid stale highlighting;
    /// the previous editors are simply dropped, so no handlers leak), adapts the options row via
    /// Visibility (never rebuilding the view), and re-runs the live conversion (validation) on
    /// the current input, preserved across the switch.
    /// </summary>
    private void SetLanguage(Lang language)
    {
        _switching = true;
        var previousText = _input?.Text ?? "";
        _language = language;

        var syntax = language == Lang.Json ? "Json" : null;

        _input = CodeEditors.Create(syntax);
        _input.Text = previousText;
        _input.TextChanged += (_, _) => DebounceValidate();
        InputHost.Child = _input;

        _output = CodeEditors.Create(syntax, readOnly: true);
        OutputHost.Child = _output;

        ConfigureOptionsFor(language);

        _switching = false;
        Validate();
    }

    private void ConfigureOptionsFor(Lang language)
    {
        MinifyBtn.Visibility = language == Lang.Yaml ? Visibility.Collapsed : Visibility.Visible;

        var showIndent = language != Lang.GraphQl;
        IndentLabel.Visibility = showIndent ? Visibility.Visible : Visibility.Collapsed;
        IndentCombo.Visibility = showIndent ? Visibility.Visible : Visibility.Collapsed;

        var tabSupported = language is Lang.Json or Lang.Xml;
        IndentTabItem.Visibility = tabSupported ? Visibility.Visible : Visibility.Collapsed;
        if (!tabSupported && ReferenceEquals(IndentCombo.SelectedItem, IndentTabItem))
            IndentCombo.SelectedItem = Indent2Item;

        SortKeysBox.Visibility = language == Lang.Json ? Visibility.Visible : Visibility.Collapsed;
        EscapeNonAsciiBox.Visibility = language == Lang.Json ? Visibility.Visible : Visibility.Collapsed;
        OmitDeclarationBox.Visibility = language == Lang.Xml ? Visibility.Visible : Visibility.Collapsed;
        UppercaseBox.Visibility = language == Lang.Sql ? Visibility.Visible : Visibility.Collapsed;
        YamlNoteText.Visibility = language == Lang.Yaml ? Visibility.Visible : Visibility.Collapsed;
    }

    // ---- Options --------------------------------------------------------------------------

    private string IndentTag => (IndentCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "2";

    private JsonFormatTool.JsonFormatOptions JsonOptions()
    {
        var tag = IndentTag;
        var useTabs = tag == "Tab";
        var indentSize = useTabs ? 1 : int.Parse(tag, CultureInfo.InvariantCulture);
        return new JsonFormatTool.JsonFormatOptions(indentSize, useTabs, SortKeysBox.IsChecked == true, EscapeNonAsciiBox.IsChecked == true);
    }

    private int YamlIndent() => int.Parse(IndentTag, CultureInfo.InvariantCulture);

    private XmlFormatOptions XmlOptions()
    {
        var tag = IndentTag;
        var useTabs = tag == "Tab";
        var indentSize = useTabs ? 2 : int.Parse(tag, CultureInfo.InvariantCulture);
        return new XmlFormatOptions(indentSize, useTabs, OmitDeclarationBox.IsChecked == true);
    }

    private SqlFormatOptions SqlOptions() => new(
        UppercaseKeywords: UppercaseBox.IsChecked == true,
        IndentSize: int.Parse(IndentTag, CultureInfo.InvariantCulture));

    // ---- Actions --------------------------------------------------------------------------

    private void Format_Click(object sender, RoutedEventArgs e)
    {
        switch (_language)
        {
            case Lang.Json:
            {
                var options = JsonOptions();
                _ = RunAsync(t => JsonFormatTool.Format(t, options));
                break;
            }
            case Lang.Yaml:
            {
                var indent = YamlIndent();
                _ = RunAsync(t => YamlFormatTool.Format(t, indent));
                break;
            }
            case Lang.Xml:
            {
                var options = XmlOptions();
                _ = RunAsync(t => XmlFormatTool.Format(t, options));
                break;
            }
            case Lang.Sql:
            {
                var options = SqlOptions();
                _ = RunAsync(t => SqlFormatTool.Format(t, options));
                break;
            }
            case Lang.GraphQl:
                _ = RunAsync(GraphQlTool.Format);
                break;
        }
    }

    private void Minify_Click(object sender, RoutedEventArgs e)
    {
        switch (_language)
        {
            case Lang.Json:
                _ = RunAsync(JsonFormatTool.Minify);
                break;
            case Lang.Xml:
                _ = RunAsync(XmlFormatTool.Minify);
                break;
            case Lang.Sql:
                _ = RunAsync(SqlFormatTool.Minify);
                break;
            case Lang.GraphQl:
                _ = RunAsync(GraphQlTool.Minify);
                break;
            case Lang.Yaml:
                // Minify is hidden for YAML (Core has no YamlFormatTool.Minify); nothing to do.
                break;
        }
    }

    /// <summary>
    /// Indent/omit-declaration changes reformat XML live (matching the original XmlFormatView);
    /// JSON/YAML option changes only refresh the validity status (matching their originals,
    /// since Validate doesn't depend on format options); SQL and GraphQL have no live behavior
    /// on option change (SQL options were always read at Format-click time; GraphQL has none).
    /// </summary>
    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || _switching)
            return;

        switch (_language)
        {
            case Lang.Xml:
            {
                var options = XmlOptions();
                _ = RunAsync(t => XmlFormatTool.Format(t, options));
                break;
            }
            case Lang.Json:
            case Lang.Yaml:
                DebounceValidate();
                break;
        }
    }

    private void CopyOutput_Click(object sender, RoutedEventArgs e) => Ui.Copy(_output.Text, CopyOutputBtn);

    /// <summary>Runs a conversion on the thread pool (safe for 1MB+ input), disabled-while-running.</summary>
    private async Task RunAsync(Func<string, string> convert)
    {
        if (_busy)
            return;
        _busy = true;
        FormatBtn.IsEnabled = false;
        MinifyBtn.IsEnabled = false;
        var text = _input.Text;
        var token = ++_formatToken;
        try
        {
            var result = await Task.Run(() => convert(text));
            if (token != _formatToken)
                return; // superseded by a newer edit, language switch, or navigating away
            _output.Text = result;
            ShowValid(text);
        }
        catch (Exception ex)
        {
            if (token != _formatToken)
                return;
            ShowError(ex.Message);
        }
        finally
        {
            _busy = false;
            FormatBtn.IsEnabled = true;
            MinifyBtn.IsEnabled = true;
        }
    }

    // ---- Live validation --------------------------------------------------------------------

    private void DebounceValidate()
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private static (int Line, int Col, string Message)? DoValidate(string text, Lang lang) => lang switch
    {
        Lang.Json => Convert(JsonFormatTool.Validate(text)),
        Lang.Yaml => Convert(YamlFormatTool.Validate(text)),
        Lang.Xml => Convert(XmlFormatTool.Validate(text)),
        Lang.GraphQl => Convert(GraphQlTool.Validate(text)),
        Lang.Sql => null, // SqlFormatTool exposes no validator; only Format/Minify report errors
        _ => null,
    };

    private static (int, int, string)? Convert(JsonFormatTool.JsonError? e) => e is null ? null : (e.Line, e.Col, e.Message);
    private static (int, int, string)? Convert(YamlFormatTool.YamlError? e) => e is null ? null : (e.Line, e.Col, e.Message);
    private static (int, int, string)? Convert(XmlError? e) => e is null ? null : (e.Line, e.Col, e.Message);
    private static (int, int, string)? Convert(GraphQlError? e) => e is null ? null : (e.Line, e.Column, e.Message);

    private async void Validate()
    {
        var text = _input.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            StatusText.Text = "Empty input";
            StatusText.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Fg2");
            return;
        }

        if (_language == Lang.Sql)
        {
            ShowNeutral(text);
            return;
        }

        var lang = _language;
        var token = ++_validateToken;
        var error = await Task.Run(() => DoValidate(text, lang));
        if (token != _validateToken)
            return; // superseded by a newer edit or language switch while this validation was running

        if (error is null)
            ShowValid(text);
        else
            ShowError($"Line {error.Value.Line}, Col {error.Value.Col}: {error.Value.Message}");
    }

    private void ShowValid(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetByteCount(text);
        StatusText.Text = $"Valid {LanguageLabel(_language)} — {FormatSize(bytes)}";
        StatusText.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Success");
    }

    private void ShowNeutral(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetByteCount(text);
        StatusText.Text = $"{LanguageLabel(_language)} — {FormatSize(bytes)}";
        StatusText.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Fg2");
    }

    private void ShowError(string message)
    {
        StatusText.Text = message;
        StatusText.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Danger");
    }

    private static string LanguageLabel(Lang lang) => lang switch
    {
        Lang.Json => "JSON",
        Lang.Yaml => "YAML",
        Lang.Xml => "XML",
        Lang.Sql => "SQL",
        Lang.GraphQl => "GraphQL",
        _ => "",
    };

    private static string FormatSize(int bytes) =>
        bytes < 1024
            ? bytes.ToString(CultureInfo.InvariantCulture) + " B"
            : (bytes / 1024.0).ToString("0.#", CultureInfo.InvariantCulture) + " KB";
}
