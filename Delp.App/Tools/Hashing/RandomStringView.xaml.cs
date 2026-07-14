using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;

namespace Delp.App.Tools.Hashing;

[Tool("random-string", "Random String Generator", ToolCategory.Hashing,
    "Generate cryptographically secure random strings with a configurable character set.",
    Keywords = "random,string,secure,token,secret", Order = 190)]
public partial class RandomStringView : UserControl
{
    public RandomStringView()
    {
        InitializeComponent();
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(LengthBox.Text.Trim(), out var length))
                throw new FormatException("Length must be a whole number.");
            var count = ParseCount();

            var options = new RandomStringOptions(
                length,
                LowerBox.IsChecked == true,
                UpperBox.IsChecked == true,
                DigitsBox.IsChecked == true,
                SymbolsBox.IsChecked == true,
                string.IsNullOrEmpty(CustomBox.Text) ? null : CustomBox.Text,
                ExcludeAmbiguousBox.IsChecked == true);

            var lines = Enumerable.Range(0, count).Select(_ => RandomStringTool.Generate(options));
            OutputBox.Text = string.Join(Environment.NewLine, lines);

            var bits = RandomStringTool.EntropyBits(options);
            EntropyNote.Text = $"≈ {bits:0.#} bits of entropy per string";
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private int ParseCount()
    {
        if (!int.TryParse(CountBox.Text.Trim(), out var count) || count < 1 || count > 1000)
            throw new FormatException("Count must be a whole number between 1 and 1000.");
        return count;
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(OutputBox.Text, CopyBtn);
}
