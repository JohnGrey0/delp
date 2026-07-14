using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;

namespace Delp.App.Tools.Hashing;

[Tool("hmac", "HMAC Generator", ToolCategory.Hashing,
    "Compute a keyed-hash message authentication code (HMAC) for a message.",
    Keywords = "hmac,signature,sha256,key,mac", Order = 20)]
public partial class HmacView : UserControl
{
    private bool _updating;

    public HmacView()
    {
        InitializeComponent();
        Run(Compute);
    }

    private InputInterpretation KeyFormat => SelectedContent(KeyFormatCombo) switch
    {
        "Hex" => InputInterpretation.Hex,
        "Base64" => InputInterpretation.Base64,
        _ => InputInterpretation.Utf8,
    };

    private string Algorithm => SelectedContent(AlgoCombo) ?? "SHA-256";

    private bool OutputAsBase64 => Base64Radio.IsChecked == true;

    private static string? SelectedContent(ComboBox combo) => (combo.SelectedItem as ComboBoxItem)?.Content as string;

    private void KeyBox_TextChanged(object sender, TextChangedEventArgs e) => OnChanged();

    private void MessageBox_TextChanged(object sender, TextChangedEventArgs e) => OnChanged();

    private void Combo_SelectionChanged(object sender, SelectionChangedEventArgs e) => OnChanged();

    private void OutputFormat_Changed(object sender, RoutedEventArgs e) => OnChanged();

    private void OnChanged()
    {
        if (IsLoaded)
            Run(Compute);
    }

    private void Compute()
    {
        EmptyKeyNote.Visibility = string.IsNullOrEmpty(KeyBox.Text) ? Visibility.Visible : Visibility.Collapsed;

        var key = HmacTool.ParseInput(KeyBox.Text, KeyFormat);
        var message = System.Text.Encoding.UTF8.GetBytes(MessageBox.Text);
        var hash = HmacTool.Compute(Algorithm, key, message);

        OutputBox.Text = OutputAsBase64
            ? Convert.ToBase64String(hash)
            : Convert.ToHexString(hash).ToLowerInvariant();
    }

    private void CopyOutput_Click(object sender, RoutedEventArgs e) => Ui.Copy(OutputBox.Text, CopyOutputBtn);

    /// <summary>Runs a conversion with reentrancy protection and inline error reporting.</summary>
    private void Run(Action convert)
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            convert();
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
            OutputBox.Text = "";
        }
        finally
        {
            _updating = false;
        }
    }
}
