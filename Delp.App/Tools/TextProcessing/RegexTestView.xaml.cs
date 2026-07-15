using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("regex-test", "Regex Tester", ToolCategory.TextProcessing,
    "Test .NET regular expressions against sample text with live match highlighting, replace, and a searchable pattern library.",
    Keywords = "regex,regexp,pattern,match,test,replace,patterns,library,cheatsheet,common,regex-library", Order = 20)]
public partial class RegexTestView : UserControl
{
    private const int MaxInputLength = 1_000_000; // ~1 MB of chars, per the perf note in the spec
    private const int MaxRenderedMatches = 1000; // cap so a pathological match count can't hang the UI thread

    private readonly DispatcherTimer _debounce;
    private bool _updating;

    public RegexTestView()
    {
        InitializeComponent();

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Run(Render);
        };

        Run(Render);

        LibraryList.ItemsSource = RegexLibrary.All;
        if (LibraryList.Items.Count > 0)
            LibraryList.SelectedIndex = 0;
        else
            LibraryDetailPanel.Visibility = Visibility.Collapsed;
    }

    private void Input_Changed(object sender, RoutedEventArgs e)
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private RegexToolOptions ReadOptions() => new(
        IgnoreCase: IgnoreCaseBox.IsChecked == true,
        Multiline: MultilineBox.IsChecked == true,
        Singleline: SinglelineBox.IsChecked == true,
        IgnoreWhitespace: IgnoreWhitespaceBox.IsChecked == true);

    private void Render()
    {
        var pattern = PatternBox.Text;
        var rawText = TestTextBox.Text ?? "";
        var truncated = rawText.Length > MaxInputLength;
        var text = truncated ? rawText[..MaxInputLength] : rawText;
        var truncationNote = truncated ? " (first 1 MB processed)" : "";

        if (string.IsNullOrEmpty(pattern))
        {
            SetPlainMatchesView(text);
            MatchesList.Items.Clear();
            ReplaceResultBox.Text = text;
            StatusText.Text = "Enter a pattern to begin." + truncationNote;
            return;
        }

        var options = ReadOptions();
        var sw = Stopwatch.StartNew();
        var result = RegexTool.Run(pattern, text, options);
        sw.Stop();

        if (result.Error != null)
        {
            SetPlainMatchesView(text);
            MatchesList.Items.Clear();
            StatusText.Text = "";
            ReplaceResultBox.Text = "";
            // Adapt RegexTool's non-throwing error channel to the app-wide Run()/ErrorText idiom.
            throw new FormatException(result.Error);
        }

        RenderMatchesView(text, result.Matches);
        RenderMatchesList(result.Matches);
        var matchNote = result.Matches.Count > MaxRenderedMatches
            ? $" (showing first {MaxRenderedMatches} of {result.Matches.Count})"
            : "";
        StatusText.Text =
            $"{result.Matches.Count} match{(result.Matches.Count == 1 ? "" : "es")} in {sw.ElapsedMilliseconds} ms{truncationNote}{matchNote}";

        var replaceResult = RegexTool.Replace(pattern, text, ReplacementBox.Text ?? "", options);
        ReplaceResultBox.Text = replaceResult.Error ?? replaceResult.Result ?? "";
    }

    private void SetPlainMatchesView(string text) =>
        MatchesView.Document = new FlowDocument(new Paragraph(new Run(text)));

    private void RenderMatchesView(string text, IReadOnlyList<MatchInfo> matches)
    {
        var paragraph = new Paragraph { Margin = new Thickness(0) };

        if (matches.Count == 0)
        {
            paragraph.Inlines.Add(new Run(text));
        }
        else
        {
            var primary = (Brush)FindResource("Brush.AccentSoft");
            var accent = (Brush)FindResource("Brush.Accent");
            var alternate = accent.Clone();
            alternate.Opacity = 0.25;

            var cursor = 0;
            var useAlternate = false;
            int? previousEnd = null;

            // Cap highlighted runs so a pathological match count (e.g. an empty-width
            // pattern over a 1 MB input) can't force thousands of Run/Inline objects
            // into the FlowDocument and hang the UI thread.
            foreach (var m in matches.OrderBy(m => m.Index).Take(MaxRenderedMatches))
            {
                if (m.Index > cursor)
                    paragraph.Inlines.Add(new Run(text.Substring(cursor, m.Index - cursor)));

                // Alternate the tint only for back-to-back matches (no gap between them) so
                // adjacent highlighted runs stay visually distinguishable from one another.
                useAlternate = previousEnd.HasValue && previousEnd.Value == m.Index && !useAlternate;

                paragraph.Inlines.Add(new Run(text.Substring(m.Index, m.Length))
                {
                    Background = useAlternate ? alternate : primary,
                });

                cursor = m.Index + m.Length;
                previousEnd = cursor;
            }

            if (cursor < text.Length)
                paragraph.Inlines.Add(new Run(text.Substring(cursor)));
        }

        MatchesView.Document = new FlowDocument(paragraph);
    }

    private void RenderMatchesList(IReadOnlyList<MatchInfo> matches)
    {
        MatchesList.Items.Clear();

        // Cap the number of match panels actually built: with 100k+ matches, even
        // WPF's virtualized ListBox would have to allocate every item up front here
        // since each entry is a pre-built panel, not a lazily-templated data row.
        var shown = Math.Min(matches.Count, MaxRenderedMatches);
        for (var i = 0; i < shown; i++)
        {
            var m = matches[i];
            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            panel.Children.Add(new TextBlock
            {
                Text = $"Match {i + 1}  [{m.Index}..{m.Index + m.Length}]  \"{Truncate(m.Value)}\"",
                Style = (Style)FindResource("Text.Mono"),
                TextWrapping = TextWrapping.Wrap,
            });

            foreach (var g in m.Groups)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = g.Success ? $"    {g.Name}: \"{Truncate(g.Value)}\"" : $"    {g.Name}: (no match)",
                    Style = (Style)FindResource("Text.Mono"),
                    Foreground = (Brush)FindResource("Brush.Fg2"),
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = g.Success ? 1.0 : 0.6,
                });
            }

            MatchesList.Items.Add(panel);
        }

        if (matches.Count > MaxRenderedMatches)
        {
            MatchesList.Items.Add(new TextBlock
            {
                Text = $"…showing {MaxRenderedMatches} of {matches.Count} matches",
                Style = (Style)FindResource("Text.Sub"),
                Margin = new Thickness(0, 4, 0, 0),
            });
        }
    }

    private static string Truncate(string s) => s.Length > 60 ? s[..60] + "…" : s;

    private void CopyReplace_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(ReplaceResultBox.Text, CopyReplaceBtn);

    private void LibrarySearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var selected = LibraryList.SelectedItem as RegexEntry;
        LibraryList.ItemsSource = RegexLibrary.Search(LibrarySearchBox.Text);

        if (selected != null && ((IEnumerable<RegexEntry>)LibraryList.ItemsSource).Contains(selected))
            LibraryList.SelectedItem = selected;
        else if (LibraryList.Items.Count > 0)
            LibraryList.SelectedIndex = 0;
        else
            ShowLibraryEntry(null);
    }

    private void LibraryList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ShowLibraryEntry(LibraryList.SelectedItem as RegexEntry);

    private void ShowLibraryEntry(RegexEntry? entry)
    {
        if (entry is null)
        {
            LibraryDetailPanel.Visibility = Visibility.Collapsed;
            return;
        }

        LibraryDetailPanel.Visibility = Visibility.Visible;
        LibraryDetailName.Text = entry.Name;
        LibraryDetailDescription.Text = entry.Description;
        LibraryPatternBox.Text = entry.Pattern;
        LibraryExampleBox.Text = entry.Example;

        var result = RegexTool.Run(entry.Pattern, entry.Example, new RegexToolOptions());
        if (result.Error != null)
        {
            LibraryMatchStatusText.Text = "✗ " + result.Error;
            LibraryMatchStatusText.Foreground = (Brush)FindResource("Brush.Danger");
        }
        else if (result.Matches.Count > 0)
        {
            LibraryMatchStatusText.Text = "✓ matches its example";
            LibraryMatchStatusText.Foreground = (Brush)FindResource("Brush.Success");
        }
        else
        {
            LibraryMatchStatusText.Text = "✗ does not match its example";
            LibraryMatchStatusText.Foreground = (Brush)FindResource("Brush.Danger");
        }
    }

    private void CopyLibraryPattern_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(LibraryPatternBox.Text, CopyLibraryPatternBtn);

    private void UsePattern_Click(object sender, RoutedEventArgs e)
    {
        if (LibraryList.SelectedItem is not RegexEntry entry)
            return;

        PatternBox.Text = entry.Pattern;
        MainTabs.SelectedIndex = 0;
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
}
