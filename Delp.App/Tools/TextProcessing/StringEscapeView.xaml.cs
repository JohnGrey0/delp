using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("string-escape", "String Escape / Unescape", ToolCategory.TextProcessing,
    "Escape and unescape text for JSON, XML/HTML, CSV, C#, JavaScript, SQL, regex, and URL contexts.",
    Keywords = "escape,unescape,json,csv,sql,quotes", Order = 70)]
public partial class StringEscapeView : UserControl
{
    private static readonly (EscapeTarget Target, string Label)[] Targets =
    [
        (EscapeTarget.Json, "JSON"),
        (EscapeTarget.XmlHtml, "XML / HTML"),
        (EscapeTarget.Csv, "CSV"),
        (EscapeTarget.CSharp, "C#"),
        (EscapeTarget.JavaScript, "JavaScript"),
        (EscapeTarget.Sql, "SQL"),
        (EscapeTarget.Regex, "Regex"),
        (EscapeTarget.Url, "URL"),
    ];

    private bool _updating;

    public StringEscapeView()
    {
        InitializeComponent();
        TargetBox.ItemsSource = Targets.Select(t => t.Label).ToList();
        TargetBox.SelectedIndex = 0;
    }

    private EscapeTarget SelectedTarget => Targets[Math.Max(TargetBox.SelectedIndex, 0)].Target;

    private void TargetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            Run(() => EscapedBox.Text = EscapeTool.Escape(SelectedTarget, PlainBox.Text));
    }

    private void PlainBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => EscapedBox.Text = EscapeTool.Escape(SelectedTarget, PlainBox.Text));

    private void EscapedBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => PlainBox.Text = EscapeTool.Unescape(SelectedTarget, EscapedBox.Text));

    private void CopyPlain_Click(object sender, RoutedEventArgs e) => Ui.Copy(PlainBox.Text, CopyPlainBtn);
    private void CopyEscaped_Click(object sender, RoutedEventArgs e) => Ui.Copy(EscapedBox.Text, CopyEscapedBtn);

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
