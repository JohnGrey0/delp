using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Encoding;

namespace Delp.App.Tools.Encoding;

[Tool("binary-hex", "Binary ↔ Hex ↔ Text", ToolCategory.Encoding,
    "Convert freely between UTF-8 text, hex, binary, and decimal byte representations.",
    Keywords = "binary,hex,bytes,bits,dump,decimal", Order = 60)]
public partial class BinaryHexView : UserControl
{
    private bool _updating;
    private byte[] _lastBytes = Array.Empty<byte>();

    public BinaryHexView()
    {
        InitializeComponent();
    }

    private bool Spaced => SpacedBox.IsChecked == true;
    private bool Uppercase => UppercaseBox.IsChecked == true;

    private void TextPaneBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => Sync(BytesTool.FromText(TextPaneBox.Text), except: TextPaneBox));

    private void HexBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => Sync(BytesTool.FromHex(HexBox.Text), except: HexBox));

    private void BinaryBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => Sync(BytesTool.FromBinary(BinaryBox.Text), except: BinaryBox));

    private void DecimalBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => Sync(BytesTool.FromDecimalBytes(DecimalBox.Text), except: DecimalBox));

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;
        Run(() =>
        {
            HexBox.Text = BytesTool.ToHex(_lastBytes, Spaced, Uppercase);
            BinaryBox.Text = BytesTool.ToBinary(_lastBytes, Spaced);
        });
    }

    private void Sync(byte[] bytes, TextBox except)
    {
        _lastBytes = bytes;
        if (!ReferenceEquals(except, TextPaneBox)) TextPaneBox.Text = BytesTool.ToText(bytes);
        if (!ReferenceEquals(except, HexBox)) HexBox.Text = BytesTool.ToHex(bytes, Spaced, Uppercase);
        if (!ReferenceEquals(except, BinaryBox)) BinaryBox.Text = BytesTool.ToBinary(bytes, Spaced);
        if (!ReferenceEquals(except, DecimalBox)) DecimalBox.Text = BytesTool.ToDecimalBytes(bytes);
    }

    private void CopyText_Click(object sender, RoutedEventArgs e) => Ui.Copy(TextPaneBox.Text, CopyTextBtn);
    private void CopyHex_Click(object sender, RoutedEventArgs e) => Ui.Copy(HexBox.Text, CopyHexBtn);
    private void CopyBinary_Click(object sender, RoutedEventArgs e) => Ui.Copy(BinaryBox.Text, CopyBinaryBtn);
    private void CopyDecimal_Click(object sender, RoutedEventArgs e) => Ui.Copy(DecimalBox.Text, CopyDecimalBtn);

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
        }
        finally
        {
            _updating = false;
        }
    }
}
