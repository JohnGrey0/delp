using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("text-stats", "Text Statistics", ToolCategory.TextProcessing,
    "Count characters, words, sentences, and other statistics for a block of text.",
    Keywords = "count,words,characters,lines,statistics", Order = 60)]
public partial class TextStatsView : UserControl
{
    private readonly DispatcherTimer _debounce;
    private bool _updating;

    public TextStatsView()
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

    private void Input_Changed(object sender, TextChangedEventArgs e)
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private void Render()
    {
        var stats = TextStatsTool.Analyze(InputBox.Text);

        StatsList.ItemsSource = new[]
        {
            new StatRow("Characters", stats.Chars.ToString(CultureInfo.InvariantCulture)),
            new StatRow("Characters (no spaces)", stats.CharsNoSpaces.ToString(CultureInfo.InvariantCulture)),
            new StatRow("Words", stats.Words.ToString(CultureInfo.InvariantCulture)),
            new StatRow("Unique words", stats.UniqueWords.ToString(CultureInfo.InvariantCulture)),
            new StatRow("Lines", stats.Lines.ToString(CultureInfo.InvariantCulture)),
            new StatRow("Non-empty lines", stats.NonEmptyLines.ToString(CultureInfo.InvariantCulture)),
            new StatRow("Sentences", stats.Sentences.ToString(CultureInfo.InvariantCulture)),
            new StatRow("Paragraphs", stats.Paragraphs.ToString(CultureInfo.InvariantCulture)),
            new StatRow("UTF-8 bytes", stats.Utf8Bytes.ToString(CultureInfo.InvariantCulture)),
            new StatRow("Avg. word length", stats.AvgWordLength.ToString("0.00", CultureInfo.InvariantCulture)),
            new StatRow("Reading time", FormatSeconds(stats.ReadingTimeSeconds)),
        };

        TopWordsList.ItemsSource = stats.TopWords.Count == 0
            ? new List<string> { "(no words)" }
            : stats.TopWords.Select(w => $"{w.Word} × {w.Count}").ToList();
    }

    private static string FormatSeconds(double seconds)
    {
        if (seconds < 60)
            return $"{seconds:0.#}s";
        var minutes = (int)(seconds / 60);
        var remaining = (int)(seconds % 60);
        return $"{minutes}m {remaining}s";
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

    private sealed record StatRow(string Label, string Value);
}
