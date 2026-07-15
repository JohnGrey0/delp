using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("line-sort", "Line Sorter & Deduplicator", ToolCategory.TextProcessing,
    "Sort, deduplicate, and clean up lines of text.",
    Keywords = "sort,dedupe,lines,unique,shuffle,filter,grep,keep,remove,number lines,sequence", Order = 50)]
public partial class LineSortView : UserControl
{
    private readonly DispatcherTimer _debounce;
    private bool _updating;

    public LineSortView()
    {
        InitializeComponent();

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Run(Render);
        };

        Run(Render);
    }

    // Guarded like JsonPathView.PathBox_TextChanged: NumberStartBox/NumberStepBox/NumberPadBox
    // carry XAML default Text values ("1"/"1"/"0"), which fire TextChanged synchronously during
    // InitializeComponent — before _debounce is assigned — so this shared handler needs the same
    // IsLoaded check the other controls it's wired to (which have no XAML defaults) don't strictly
    // need. The constructor's explicit Run(Render) call after field initialization covers the
    // initial state instead.
    private void Input_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;
        _debounce.Stop();
        _debounce.Start();
    }

    // Toggles the start/step/pad row's visibility in addition to re-running conversion — a
    // dedicated handler rather than reusing Input_Changed directly.
    private void NumberLines_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;
        NumberOptionsPanel.Visibility = Show(NumberLinesBox.IsChecked == true);
        Input_Changed(sender, e);
    }

    private static Visibility Show(bool visible) => visible ? Visibility.Visible : Visibility.Collapsed;

    private SortMode ReadSortMode() => SortModeBox.SelectedIndex switch
    {
        1 => SortMode.Asc,
        2 => SortMode.Desc,
        3 => SortMode.Natural,
        4 => SortMode.Length,
        5 => SortMode.Numeric,
        _ => SortMode.None,
    };

    private LineFilterMode ReadFilterMode() => FilterModeBox.SelectedIndex switch
    {
        1 => LineFilterMode.Keep,
        2 => LineFilterMode.Remove,
        _ => LineFilterMode.Off,
    };

    private static int ParseIntOrDefault(string text, int fallback) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;

    private void Render()
    {
        var options = new LineToolOptions(
            Mode: ReadSortMode(),
            CaseInsensitive: CaseInsensitiveBox.IsChecked == true,
            Dedupe: DedupeBox.IsChecked == true,
            TrimLines: TrimBox.IsChecked == true,
            RemoveEmpty: RemoveEmptyBox.IsChecked == true,
            Reverse: ReverseBox.IsChecked == true,
            Shuffle: ShuffleBox.IsChecked == true,
            Filter: ReadFilterMode(),
            FilterPattern: FilterPatternBox.Text,
            FilterRegex: FilterRegexBox.IsChecked == true,
            NumberLines: NumberLinesBox.IsChecked == true,
            NumberStart: ParseIntOrDefault(NumberStartBox.Text, 1),
            NumberStep: ParseIntOrDefault(NumberStepBox.Text, 1),
            NumberPad: ParseIntOrDefault(NumberPadBox.Text, 0));

        var result = LineTool.Process(InputBox.Text, options);
        OutputBox.Text = result.Text;
        StatusText.Text = result.FilteredTotal is int total
            ? $"{result.Before} → {result.After} lines · kept {result.FilteredKept} of {total}"
            : $"{result.Before} → {result.After} lines";
    }

    private void CopyOutput_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(OutputBox.Text, CopyOutputBtn);

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
}
