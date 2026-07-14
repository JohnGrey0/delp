using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("regex-library", "Regex Pattern Library", ToolCategory.TextProcessing,
    "Browse a searchable library of ready-made regular expressions for common formats.",
    Keywords = "regex,patterns,library,cheatsheet,common", Order = 30)]
public partial class RegexLibraryView : UserControl
{
    public RegexLibraryView()
    {
        InitializeComponent();
        EntryList.ItemsSource = RegexLibrary.All;
        if (EntryList.Items.Count > 0)
            EntryList.SelectedIndex = 0;
        else
            DetailPanel.Visibility = Visibility.Collapsed;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var selected = EntryList.SelectedItem as RegexEntry;
        EntryList.ItemsSource = RegexLibrary.Search(SearchBox.Text);

        if (selected != null && ((IEnumerable<RegexEntry>)EntryList.ItemsSource).Contains(selected))
            EntryList.SelectedItem = selected;
        else if (EntryList.Items.Count > 0)
            EntryList.SelectedIndex = 0;
        else
            ShowEntry(null);
    }

    private void EntryList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ShowEntry(EntryList.SelectedItem as RegexEntry);

    private void ShowEntry(RegexEntry? entry)
    {
        if (entry is null)
        {
            DetailPanel.Visibility = Visibility.Collapsed;
            return;
        }

        DetailPanel.Visibility = Visibility.Visible;
        DetailName.Text = entry.Name;
        DetailDescription.Text = entry.Description;
        PatternBox.Text = entry.Pattern;
        ExampleBox.Text = entry.Example;

        var result = RegexTool.Run(entry.Pattern, entry.Example, new RegexToolOptions());
        if (result.Error != null)
        {
            MatchStatusText.Text = "✗ " + result.Error;
            MatchStatusText.Foreground = (Brush)FindResource("Brush.Danger");
        }
        else if (result.Matches.Count > 0)
        {
            MatchStatusText.Text = "✓ matches its example";
            MatchStatusText.Foreground = (Brush)FindResource("Brush.Success");
        }
        else
        {
            MatchStatusText.Text = "✗ does not match its example";
            MatchStatusText.Foreground = (Brush)FindResource("Brush.Danger");
        }
    }

    private void CopyPattern_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(PatternBox.Text, CopyPatternBtn);
}
