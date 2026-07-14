using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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

    private readonly DispatcherTimer _debounce;
    private bool _updating;

    public TextToListView()
    {
        InitializeComponent();
        FormatBox.ItemsSource = Formats.Select(f => f.Label).ToList();
        FormatBox.SelectedIndex = 0;
        QuoteBox.ItemsSource = Quotes.Select(q => q.Label).ToList();
        QuoteBox.SelectedIndex = 0;

        // Words-mode splitting is a regex scan over the whole input, so re-running it on every
        // keystroke of a large paste would stall the UI thread; debounce like every other
        // continuous-typing tool in the app (LineSortView, TextStatsView, ...).
        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Run(Refresh);
        };

        Unloaded += (_, _) => _debounce.Stop();
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
        Debounce();
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Debounce();
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e) => Debounce();

    private void Debounce()
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private void Refresh()
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
    }

    /// <summary>Runs a render pass with reentrancy protection and inline error reporting.</summary>
    private void Run(Action render)
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            render();
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

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(OutputBox.Text, CopyBtn);
}
