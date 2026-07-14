using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Encoding;

namespace Delp.App.Tools.Encoding;

[Tool("morse", "Morse Code Translator", ToolCategory.Encoding,
    "Translate text to and from ITU International Morse Code.",
    Keywords = "morse,dot,dash,telegraph,itu", Order = 70)]
public partial class MorseView : UserControl
{
    private bool _updating;

    public MorseView()
    {
        InitializeComponent();
    }

    private bool SkipUnknown => SkipUnknownBox.IsChecked == true;

    private void PlainBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => MorseBox.Text = MorseTool.Encode(PlainBox.Text, SkipUnknown));

    private void MorseBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => PlainBox.Text = MorseTool.Decode(MorseBox.Text));

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Run(() => MorseBox.Text = MorseTool.Encode(PlainBox.Text, SkipUnknown));
    }

    private void CopyText_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(PlainBox.Text, CopyTextBtn);

    private void CopyMorse_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(MorseBox.Text, CopyMorseBtn);

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
