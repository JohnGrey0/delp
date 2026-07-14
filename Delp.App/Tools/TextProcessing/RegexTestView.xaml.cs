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
    "Test .NET regular expressions against sample text with live match highlighting and replace.",
    Keywords = "regex,regexp,pattern,match,test,replace", Order = 20)]
public partial class RegexTestView : UserControl
{
    private const int MaxInputLength = 1_000_000; // ~1 MB of chars, per the perf note in the spec

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
        StatusText.Text =
            $"{result.Matches.Count} match{(result.Matches.Count == 1 ? "" : "es")} in {sw.ElapsedMilliseconds} ms{truncationNote}";

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

            foreach (var m in matches.OrderBy(m => m.Index))
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

        for (var i = 0; i < matches.Count; i++)
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
    }

    private static string Truncate(string s) => s.Length > 60 ? s[..60] + "…" : s;

    private void CopyReplace_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(ReplaceResultBox.Text, CopyReplaceBtn);

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
