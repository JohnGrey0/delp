using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("text-to-list", "Text → List / CSV", ToolCategory.TextProcessing,
    "Split text into words, lines, or delimited parts and format the result as a list, array, or CSV in a dozen styles.",
    Keywords = "list,array,python,csv,split,delimiter,words,sql in", Order = 150)]
public partial class TextToListView : UserControl
{
    private static readonly (ListFormat Value, string Label)[] Formats =
    [
        (ListFormat.PythonList, "Python list"),
        (ListFormat.JsonArray, "JSON array"),
        (ListFormat.JsArray, "JavaScript array"),
        (ListFormat.CSharpArray, "C# array"),
        (ListFormat.CsvLine, "CSV line"),
        (ListFormat.CsvColumn, "CSV column"),
        (ListFormat.SqlIn, "SQL IN (...)"),
        (ListFormat.PlainLines, "Plain lines"),
        (ListFormat.SpaceJoined, "Space joined"),
    ];

    private static readonly (QuoteChar Value, string Label)[] Quotes =
    [
        (QuoteChar.Double, "Double \""),
        (QuoteChar.Single, "Single '"),
        (QuoteChar.None, "None"),
    ];

    public TextToListView()
    {
        InitializeComponent();
        FormatBox.ItemsSource = Formats.Select(f => f.Label).ToList();
        FormatBox.SelectedIndex = 0;
        QuoteBox.ItemsSource = Quotes.Select(q => q.Label).ToList();
        QuoteBox.SelectedIndex = 0;
    }

    private SplitMode Mode =>
        LinesMode.IsChecked == true ? SplitMode.Lines :
        DelimiterMode.IsChecked == true ? SplitMode.Delimiter :
        SplitMode.Words;

    private ListFormat Format => Formats[Math.Max(FormatBox.SelectedIndex, 0)].Value;

    private QuoteChar Quote => Quotes[Math.Max(QuoteBox.SelectedIndex, 0)].Value;

    private void Mode_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;
        DelimiterBox.Visibility = Mode == SplitMode.Delimiter ? Visibility.Visible : Visibility.Collapsed;
        Refresh();
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Refresh();
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh();

    private void Refresh()
    {
        try
        {
            var options = new TextListOptions(
                Trim: TrimBox.IsChecked == true,
                RemoveEmpty: RemoveEmptyBox.IsChecked == true,
                Dedupe: DedupeBox.IsChecked == true,
                Lowercase: LowercaseBox.IsChecked == true,
                StripPunctuation: StripPunctuationBox.IsChecked == true);

            var items = TextListTool.Split(InputBox.Text, Mode, DelimiterBox.Text, options);
            OutputBox.Text = TextListTool.Format(items, Format, Quote);
            StatusText.Text = $"{items.Count} item{(items.Count == 1 ? "" : "s")}";
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(OutputBox.Text, CopyBtn);
}
