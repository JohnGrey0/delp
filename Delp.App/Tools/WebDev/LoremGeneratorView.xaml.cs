using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.WebDev;

namespace Delp.App.Tools.WebDev;

[Tool("lorem-ipsum", "Lorem Ipsum Generator", ToolCategory.WebDev,
    "Generate placeholder Lorem Ipsum text by words, sentences, or paragraphs.",
    Keywords = "lorem,ipsum,placeholder,text,dummy", Order = 60)]
public partial class LoremGeneratorView : UserControl
{
    public LoremGeneratorView()
    {
        InitializeComponent();
        Generate();
    }

    private void Generate_Click(object sender, RoutedEventArgs e) => Generate();

    private void Generate()
    {
        try
        {
            if (!int.TryParse(CountBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
                throw new FormatException("Count must be a whole number.");

            var unit = WordsRadio.IsChecked == true
                ? LoremUnit.Words
                : SentencesRadio.IsChecked == true
                    ? LoremUnit.Sentences
                    : LoremUnit.Paragraphs;

            var options = new LoremOptions(
                unit,
                count,
                StartClassic: StartClassicBox.IsChecked == true,
                HtmlParagraphs: HtmlParagraphsBox.IsChecked == true);

            var text = LoremTool.Generate(options);
            OutputBox.Text = text;

            var wordCount = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            StatusText.Text = $"{wordCount} words · {text.Length} characters";
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(OutputBox.Text, CopyBtn);
}
