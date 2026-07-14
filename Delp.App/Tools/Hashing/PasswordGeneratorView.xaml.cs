using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;

namespace Delp.App.Tools.Hashing;

[Tool("password-generator", "Password Generator", ToolCategory.Hashing,
    "Generate secure random passwords or memorable passphrases, with live entropy and strength feedback.",
    Keywords = "password,generate,secure,passphrase,strength", Order = 50)]
public partial class PasswordGeneratorView : UserControl
{
    public PasswordGeneratorView()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateEntropy();
    }

    private bool IsPassphraseTab => ModeTabs.SelectedIndex == 1;

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (IsPassphraseTab)
            {
                var options = ReadPassphraseOptions();
                OutputBox.Text = PasswordTool.GeneratePassphrase(options);
                ShowEntropy(PasswordTool.EntropyBits(options));
            }
            else
            {
                var options = ReadPasswordOptions();
                var count = ReadCount();
                var lines = new string[count];
                for (var i = 0; i < count; i++)
                    lines[i] = PasswordTool.Generate(options);
                OutputBox.Text = string.Join(Environment.NewLine, lines);
                ShowEntropy(PasswordTool.EntropyBits(options));
            }
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            UpdateEntropy();
    }

    private void UpdateEntropy()
    {
        try
        {
            var bits = IsPassphraseTab
                ? PasswordTool.EntropyBits(ReadPassphraseOptions())
                : PasswordTool.EntropyBits(ReadPasswordOptions());
            ShowEntropy(bits);
        }
        catch
        {
            EntropyText.Text = "";
            StrengthText.Text = "";
        }
    }

    private void ShowEntropy(double bits)
    {
        var strength = PasswordTool.StrengthLabel(bits);
        EntropyText.Text = $"≈ {bits.ToString("F1", CultureInfo.InvariantCulture)} bits of entropy · ";
        StrengthText.Text = strength.ToString().ToUpperInvariant();
        StrengthText.Foreground = strength switch
        {
            PasswordStrength.Weak => (Brush)FindResource("Brush.Danger"),
            PasswordStrength.Fair => (Brush)FindResource("Brush.Warning"),
            PasswordStrength.Strong => (Brush)FindResource("Brush.Success"),
            _ => (Brush)FindResource("Brush.Accent"),
        };
    }

    private PasswordOptions ReadPasswordOptions() => new(
        ParseInt(LengthBox.Text, "Length"),
        LowerBox.IsChecked == true,
        UpperBox.IsChecked == true,
        DigitsBox.IsChecked == true,
        SymbolsBox.IsChecked == true,
        ExcludeAmbiguousBox.IsChecked == true,
        RequireEachBox.IsChecked == true);

    private PassphraseOptions ReadPassphraseOptions()
    {
        var words = ParseInt(WordsBox.Text, "Word count");
        var tag = (SeparatorCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "-";
        var separator = tag == "space" ? ' ' : tag[0];
        return new PassphraseOptions(words, separator, CapitalizeBox.IsChecked == true, AppendNumberBox.IsChecked == true);
    }

    private int ReadCount()
    {
        var count = ParseInt(CountBox.Text, "Count");
        if (count > 1000)
            throw new FormatException("Count must be at most 1000.");
        return count;
    }

    private static int ParseInt(string text, string field)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
            throw new FormatException($"{field} must be a positive whole number.");
        return value;
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(OutputBox.Text, CopyBtn);
}
