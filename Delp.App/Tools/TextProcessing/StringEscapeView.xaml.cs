using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Encoding;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("string-escape", "Escape / Encode", ToolCategory.TextProcessing,
    "Escape and unescape text for JSON, XML/HTML, HTML entities, CSV, C#, JavaScript, SQL, regex, URL, and Unicode escape contexts.",
    Keywords = "escape,unescape,json,csv,sql,quotes,url,percent,entities,unicode,\\u,url-encode,html-entities,unicode-escape",
    Order = 70)]
public partial class StringEscapeView : UserControl
{
    /// <summary>Escape targets offered by this view. The first seven map 1:1 onto
    /// <see cref="EscapeTarget"/>; the rest dispatch to the dedicated encoders that used to be
    /// their own tools (<see cref="HtmlEntityTool"/>, <see cref="UrlEncodeTool"/>,
    /// <see cref="UnicodeEscapeTool"/>).</summary>
    private enum Target
    {
        Json,
        XmlHtml,
        HtmlEntities,
        Csv,
        CSharp,
        JavaScript,
        Sql,
        Regex,
        UrlComponent,
        UrlFormData,
        UrlPreserve,
        UnicodeEscape,
    }

    private static readonly (Target Target, string Label)[] Targets =
    [
        (Target.Json, "JSON"),
        (Target.XmlHtml, "XML / HTML"),
        (Target.HtmlEntities, "HTML Entities"),
        (Target.Csv, "CSV"),
        (Target.CSharp, "C#"),
        (Target.JavaScript, "JavaScript"),
        (Target.Sql, "SQL"),
        (Target.Regex, "Regex"),
        (Target.UrlComponent, "URL — Component"),
        (Target.UrlFormData, "URL — Form data"),
        (Target.UrlPreserve, "URL — Preserve URI chars"),
        (Target.UnicodeEscape, "Unicode escapes"),
    ];

    private bool _updating;

    public StringEscapeView()
    {
        InitializeComponent();
        TargetBox.ItemsSource = Targets.Select(t => t.Label).ToList();
        TargetBox.SelectedIndex = 0;
    }

    private Target SelectedTarget => Targets[Math.Max(TargetBox.SelectedIndex, 0)].Target;

    private bool NonAsciiToNumeric => NumericEntitiesBox.IsChecked == true;
    private bool NonAsciiOnly => NonAsciiOnlyBox.IsChecked == true;

    private void TargetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateOptionVisibility();
        if (IsLoaded)
            Run(() => EscapedBox.Text = Escape(SelectedTarget, PlainBox.Text));
    }

    private void UpdateOptionVisibility()
    {
        var target = SelectedTarget;
        HtmlEntitiesOptions.Visibility = target == Target.HtmlEntities ? Visibility.Visible : Visibility.Collapsed;
        UnicodeOptions.Visibility = target == Target.UnicodeEscape ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Run(() => EscapedBox.Text = Escape(SelectedTarget, PlainBox.Text));
    }

    private void PlainBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => EscapedBox.Text = Escape(SelectedTarget, PlainBox.Text));

    private void EscapedBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => PlainBox.Text = Unescape(SelectedTarget, EscapedBox.Text));

    private void CopyPlain_Click(object sender, RoutedEventArgs e) => Ui.Copy(PlainBox.Text, CopyPlainBtn);
    private void CopyEscaped_Click(object sender, RoutedEventArgs e) => Ui.Copy(EscapedBox.Text, CopyEscapedBtn);

    private string Escape(Target target, string input) => target switch
    {
        Target.HtmlEntities => HtmlEntityTool.Encode(input, NonAsciiToNumeric),
        Target.UrlComponent => UrlEncodeTool.Encode(input, UrlEncodeMode.Component),
        Target.UrlFormData => UrlEncodeTool.Encode(input, UrlEncodeMode.FormData),
        Target.UrlPreserve => UrlEncodeTool.Encode(input, UrlEncodeMode.PreserveUriChars),
        Target.UnicodeEscape => UnicodeEscapeTool.Escape(input, NonAsciiOnly),
        _ => EscapeTool.Escape(ToEscapeTarget(target), input),
    };

    private string Unescape(Target target, string input) => target switch
    {
        Target.HtmlEntities => HtmlEntityTool.Decode(input),
        Target.UrlComponent => UrlEncodeTool.Decode(input, UrlEncodeMode.Component),
        Target.UrlFormData => UrlEncodeTool.Decode(input, UrlEncodeMode.FormData),
        Target.UrlPreserve => UrlEncodeTool.Decode(input, UrlEncodeMode.PreserveUriChars),
        Target.UnicodeEscape => UnicodeEscapeTool.Unescape(input),
        _ => EscapeTool.Unescape(ToEscapeTarget(target), input),
    };

    private static EscapeTarget ToEscapeTarget(Target target) => target switch
    {
        Target.Json => EscapeTarget.Json,
        Target.XmlHtml => EscapeTarget.XmlHtml,
        Target.Csv => EscapeTarget.Csv,
        Target.CSharp => EscapeTarget.CSharp,
        Target.JavaScript => EscapeTarget.JavaScript,
        Target.Sql => EscapeTarget.Sql,
        Target.Regex => EscapeTarget.Regex,
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "No EscapeTarget mapping for this target."),
    };

    /// <summary>Runs a conversion with reentrancy protection and inline error reporting.</summary>
    private void Run(Action convert)
    {
        if (_updating)
            return;
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
