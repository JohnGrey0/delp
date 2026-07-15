using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Encoding;

namespace Delp.App.Tools.Encoding;

[Tool("base64", "Base64 Encode / Decode", ToolCategory.Encoding,
    "Convert text to and from Base64, Base32, Base58, and Ascii85.",
    Keywords = "base64,b64,encode,decode,binary,ascii,base32,base58,base85,ascii85,crockford,base-n", Order = 10)]
public partial class Base64View : UserControl
{
    private static readonly string[] EncodedLabels =
    [
        "BASE64", "BASE64 URL-SAFE", "BASE32", "BASE32 CROCKFORD", "BASE58", "ASCII85",
    ];

    private bool _updating;

    public Base64View()
    {
        InitializeComponent();
        AlphabetBox.SelectedIndex = 0;
    }

    private int Alphabet => Math.Max(AlphabetBox.SelectedIndex, 0);

    private string Encode(string text) => Alphabet switch
    {
        0 => Base64Tool.Encode(text),
        1 => Base64Tool.Encode(text, urlSafe: true),
        2 => BaseNTool.Encode(text, BaseNAlphabet.Base32),
        3 => BaseNTool.Encode(text, BaseNAlphabet.Base32Crockford),
        4 => BaseNTool.Encode(text, BaseNAlphabet.Base58),
        _ => BaseNTool.Encode(text, BaseNAlphabet.Ascii85),
    };

    private string Decode(string text) => Alphabet switch
    {
        0 => Base64Tool.Decode(text),
        1 => Base64Tool.Decode(text, urlSafe: true),
        2 => BaseNTool.Decode(text, BaseNAlphabet.Base32),
        3 => BaseNTool.Decode(text, BaseNAlphabet.Base32Crockford),
        4 => BaseNTool.Decode(text, BaseNAlphabet.Base58),
        _ => BaseNTool.Decode(text, BaseNAlphabet.Ascii85),
    };

    private void PlainBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => EncodedBox.Text = Encode(PlainBox.Text));

    private void EncodedBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => PlainBox.Text = Decode(EncodedBox.Text));

    private void Alphabet_Changed(object sender, SelectionChangedEventArgs e)
    {
        EncodedLabelText.Text = EncodedLabels[Alphabet];
        if (IsLoaded)
            Run(() => EncodedBox.Text = Encode(PlainBox.Text));
    }

    private void CopyPlain_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(PlainBox.Text, CopyPlainBtn);

    private void CopyEncoded_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(EncodedBox.Text, CopyEncodedBtn);

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
