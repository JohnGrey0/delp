using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("whitespace", "Whitespace Visualizer & Cleaner", ToolCategory.TextProcessing,
    "Reveal invisible whitespace and clean up trailing spaces, tabs, and line endings.",
    Keywords = "whitespace,tabs,spaces,trailing,invisible", Order = 80)]
public partial class WhitespaceView : UserControl
{
    private static readonly (LineEnding Value, string Label)[] LineEndings =
    [
        (LineEnding.None, "Unchanged"),
        (LineEnding.Lf, "LF"),
        (LineEnding.CrLf, "CRLF"),
    ];

    private bool _updating;

    public WhitespaceView()
    {
        InitializeComponent();
        LineEndingBox.ItemsSource = LineEndings.Select(l => l.Label).ToList();
        LineEndingBox.SelectedIndex = 0;
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh();

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Refresh();
    }

    private void Refresh()
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            var text = InputBox.Text;
            VisualizedBox.Text = WhitespaceTool.Visualize(text);

            var result = WhitespaceTool.Clean(text, BuildOptions());
            CleanedBox.Text = result.Text;
            StatusText.Text = $"{result.Changes} change{(result.Changes == 1 ? "" : "s")}";
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

    private WhitespaceCleanOptions BuildOptions()
    {
        var tabWidth = 4;
        if (int.TryParse(TabWidthBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
            tabWidth = parsed;

        var lineEndingIndex = Math.Max(LineEndingBox.SelectedIndex, 0);

        return new WhitespaceCleanOptions(
            TrimTrailing: TrimTrailingBox.IsChecked == true,
            TrimLeading: TrimLeadingBox.IsChecked == true,
            CollapseSpaces: CollapseSpacesBox.IsChecked == true,
            TabsToSpaces: TabsToSpacesBox.IsChecked == true,
            TabWidth: tabWidth,
            SpacesToTabs: SpacesToTabsBox.IsChecked == true,
            RemoveEmptyLines: RemoveEmptyBox.IsChecked == true,
            CollapseEmptyLines: CollapseEmptyBox.IsChecked == true,
            Normalize: LineEndings[lineEndingIndex].Value,
            StripZeroWidth: StripZeroWidthBox.IsChecked == true);
    }

    private void CopyCleaned_Click(object sender, RoutedEventArgs e) => Ui.Copy(CleanedBox.Text, CopyCleanedBtn);
}
