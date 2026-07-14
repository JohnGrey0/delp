using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Encoding;

namespace Delp.App.Tools.Encoding;

[Tool("rot13", "ROT13 / Caesar Cipher", ToolCategory.Encoding,
    "Shift letters by a configurable amount, ROT13 by default; every other character is left untouched.",
    Keywords = "rot13,caesar,cipher,shift", Order = 80)]
public partial class Rot13View : UserControl
{
    private bool _updating;

    public Rot13View()
    {
        InitializeComponent();
    }

    private int Shift
    {
        get
        {
            if (!int.TryParse(ShiftBox.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n))
                throw new FormatException($"'{ShiftBox.Text}' is not a valid integer shift amount.");
            return n;
        }
    }

    private void PlainBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => ShiftedBox.Text = Rot13Tool.Shift(PlainBox.Text, Shift));

    private void ShiftedBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => PlainBox.Text = Rot13Tool.Shift(ShiftedBox.Text, 26 - Shift));

    private void Option_Changed(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded)
            Run(() => ShiftedBox.Text = Rot13Tool.Shift(PlainBox.Text, Shift));
    }

    private void CopyPlain_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(PlainBox.Text, CopyPlainBtn);

    private void CopyShifted_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(ShiftedBox.Text, CopyShiftedBtn);

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
