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

    public TextNlpView()
    {
        InitializeComponent();

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Run();
        };

        Loaded += (_, _) => Run();
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e) => Debounce();
    private void ExtraStopwords_TextChanged(object sender, TextChangedEventArgs e) => Debounce();

    private void Options_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Run();
    }

    private void Debounce()
    {
        if (!IsLoaded)
            return;
        _debounce.Stop();
        _debounce.Start();
    }

    private void Run()
    {
        try
        {
            var options = new NlpTool.NlpOptions(
                Lowercase: LowercaseBox.IsChecked == true,
                RemoveStopwords: StopwordsBox.IsChecked == true,
                RemovePunctuation: PunctuationBox.IsChecked == true,
                RemoveNumbers: NumbersBox.IsChecked == true,
                Stem: StemBox.IsChecked == true,
                ExtraStopwords: ExtraStopwordsBox.Text);

            var result = NlpTool.Process(InputBox.Text, options);

            ProcessedBox.Text = result.ProcessedText;
            TokensBox.Text = string.Join(Environment.NewLine, result.Tokens);

            FrequencyList.ItemsSource = result.Frequencies
                .Take(100)
                .Select(f => $"{f.Term} × {f.Count}")
                .ToList();

            var bigrams = NlpTool.Ngrams(result.Tokens, 2);
            BigramList.ItemsSource = bigrams
                .Take(50)
                .Select(g => $"{g.Gram} × {g.Count}")
                .ToList();

            var trigrams = NlpTool.Ngrams(result.Tokens, 3);
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
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void CopyProcessed_Click(object sender, RoutedEventArgs e) => Ui.Copy(ProcessedBox.Text, CopyProcessedBtn);

    private void CopyTokens_Click(object sender, RoutedEventArgs e) => Ui.Copy(TokensBox.Text, CopyTokensBtn);
}
