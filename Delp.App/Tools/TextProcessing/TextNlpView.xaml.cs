using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("text-nlp", "NLP Text Processor", ToolCategory.TextProcessing,
    "Tokenize text with a hand-rolled Porter stemmer, stopword removal and n-gram frequencies.",
    Keywords = "nlp,stopwords,stemming,tokens,ngrams,frequency", Order = 140)]
public partial class TextNlpView : UserControl
{
    private readonly DispatcherTimer _debounce;
    private int _runToken;

    public TextNlpView()
    {
        InitializeComponent();

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            _ = RunAsync();
        };

        Loaded += (_, _) => _ = RunAsync();
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e) => Debounce();
    private void ExtraStopwords_TextChanged(object sender, TextChangedEventArgs e) => Debounce();

    private void Options_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            _ = RunAsync();
    }

    private void Debounce()
    {
        if (!IsLoaded)
            return;
        _debounce.Stop();
        _debounce.Start();
    }

    /// <summary>
    /// Runs the full tokenize/stem/frequency/n-gram pipeline off the UI
    /// thread (a pasted megabyte-scale document must not freeze the app),
    /// then applies the result back on the UI thread — unless a newer run
    /// has since been kicked off, in which case this stale result is
    /// dropped rather than clobbering what's on screen.
    /// </summary>
    private async Task RunAsync()
    {
        var token = ++_runToken;

        var options = new NlpTool.NlpOptions(
            Lowercase: LowercaseBox.IsChecked == true,
            RemoveStopwords: StopwordsBox.IsChecked == true,
            RemovePunctuation: PunctuationBox.IsChecked == true,
            RemoveNumbers: NumbersBox.IsChecked == true,
            Stem: StemBox.IsChecked == true,
            ExtraStopwords: ExtraStopwordsBox.Text);
        var text = InputBox.Text;

        try
        {
            var (result, bigrams, trigrams) = await Task.Run(() =>
            {
                var r = NlpTool.Process(text, options);
                var bg = NlpTool.Ngrams(r.Tokens, 2);
                var tg = NlpTool.Ngrams(r.Tokens, 3);
                return (r, bg, tg);
            });

            if (token != _runToken)
                return;

            ProcessedBox.Text = result.ProcessedText;
            TokensBox.Text = string.Join(Environment.NewLine, result.Tokens);

            FrequencyList.ItemsSource = result.Frequencies
                .Take(100)
                .Select(f => $"{f.Term} × {f.Count}")
                .ToList();

            BigramList.ItemsSource = bigrams
                .Take(50)
                .Select(g => $"{g.Gram} × {g.Count}")
                .ToList();

            TrigramList.ItemsSource = trigrams
                .Take(50)
                .Select(g => $"{g.Gram} × {g.Count}")
                .ToList();

            StatusText.Text =
                $"{result.Tokens.Count} tokens · {result.Frequencies.Count} unique · {result.SentenceCount} sentences";
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            if (token != _runToken)
                return;
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void CopyProcessed_Click(object sender, RoutedEventArgs e) => Ui.Copy(ProcessedBox.Text, CopyProcessedBtn);

    private void CopyTokens_Click(object sender, RoutedEventArgs e) => Ui.Copy(TokensBox.Text, CopyTokensBtn);
}
