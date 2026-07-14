using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("line-sort", "Line Sorter & Deduplicator", ToolCategory.TextProcessing,
    "Sort, deduplicate, and clean up lines of text.",
    Keywords = "sort,dedupe,lines,unique,shuffle", Order = 50)]
public partial class LineSortView : UserControl
{
    private bool _updating;

    public LineSortView()
    {
        InitializeComponent();
        Run(Render);
    }

    private void Input_Changed(object sender, RoutedEventArgs e) => Run(Render);

    private SortMode ReadSortMode() => SortModeBox.SelectedIndex switch
    {
        1 => SortMode.Asc,
        2 => SortMode.Desc,
        3 => SortMode.Natural,
        4 => SortMode.Length,
        5 => SortMode.Numeric,
        _ => SortMode.None,
    };

    private void Render()
    {
        var options = new LineToolOptions(
            Mode: ReadSortMode(),
            CaseInsensitive: CaseInsensitiveBox.IsChecked == true,
            Dedupe: DedupeBox.IsChecked == true,
            TrimLines: TrimBox.IsChecked == true,
            RemoveEmpty: RemoveEmptyBox.IsChecked == true,
            Reverse: ReverseBox.IsChecked == true,
            Shuffle: ShuffleBox.IsChecked == true);

        var result = LineTool.Process(InputBox.Text, options);
        OutputBox.Text = result.Text;
        StatusText.Text = $"{result.Before} → {result.After} lines";
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
