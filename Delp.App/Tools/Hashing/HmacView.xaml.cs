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
    private byte[] _hash = [];

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

    private void KeyBox_TextChanged(object sender, TextChangedEventArgs e) => OnInputChanged();

    private void MessageBox_TextChanged(object sender, TextChangedEventArgs e) => OnInputChanged();

    private void Combo_SelectionChanged(object sender, SelectionChangedEventArgs e) => OnInputChanged();

    // Hex/Base64 is purely a display format for the already-computed digest — reformat the
    // cached hash instead of re-running the HMAC over the key/message again. If the key/message
    // are currently invalid (error banner showing), the cached hash is stale, so fall back to a
    // full recompute rather than silently redisplaying an old digest.
    private void OutputFormat_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;
        Run(ErrorText.Visibility == Visibility.Visible ? Compute : RenderOutput);
    }

    private void OnInputChanged()
    {
        if (IsLoaded)
            Run(Compute);
    }

    private void Compute()
    {
        EmptyKeyNote.Visibility = string.IsNullOrEmpty(KeyBox.Text) ? Visibility.Visible : Visibility.Collapsed;

        var key = HmacTool.ParseInput(KeyBox.Text, KeyFormat);
        var message = System.Text.Encoding.UTF8.GetBytes(MessageBox.Text);
        _hash = HmacTool.Compute(Algorithm, key, message);
        RenderOutput();
    }

    private void RenderOutput()
    {
        OutputBox.Text = OutputAsBase64
            ? Convert.ToBase64String(_hash)
            : Convert.ToHexString(_hash).ToLowerInvariant();
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
