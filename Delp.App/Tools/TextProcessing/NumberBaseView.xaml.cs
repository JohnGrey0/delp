using System.Globalization;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("number-base", "Number Base Converter", ToolCategory.TextProcessing,
    "Convert arbitrary-precision integers between binary, octal, decimal, hex, and any custom radix 2-36.",
    Keywords = "binary,octal,decimal,hex,radix,base", Order = 90)]
public partial class NumberBaseView : UserControl
{
    private bool _updating;
    private BigInteger _value = BigInteger.Zero;

    public NumberBaseView()
    {
        InitializeComponent();
        _updating = true;
        try
        {
            RefreshBoxesExcept(null);
            RefreshCustomBox();
            UpdateStatus();
        }
        finally
        {
            _updating = false;
        }
    }

    private void BinaryBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateFrom(BinaryBox.Text, 2, BinaryBox);
    private void OctalBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateFrom(OctalBox.Text, 8, OctalBox);
    private void DecimalBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateFrom(DecimalBox.Text, 10, DecimalBox);
    private void HexBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateFrom(HexBox.Text, 16, HexBox);

    private void UpdateFrom(string text, int radix, TextBox source)
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            _value = BaseTool.Parse(text, radix);
            RefreshBoxesExcept(source);
            RefreshCustomBox();
            UpdateStatus();
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

    private void CustomBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            if (!int.TryParse(CustomRadixBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var radix) || radix is < 2 or > 36)
                throw new FormatException("Custom radix must be an integer between 2 and 36.");

            _value = BaseTool.Parse(CustomValueBox.Text, radix);
            RefreshBoxesExcept(null);
            UpdateStatus();
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

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || _updating)
            return;
        _updating = true;
        try
        {
            RefreshBoxesExcept(null);
            RefreshCustomBox();
        }
        finally
        {
            _updating = false;
        }
    }

    /// <summary>Reformats the four linked boxes from <see cref="_value"/>, skipping
    /// <paramref name="source"/> (the box the user is actively editing).</summary>
    private void RefreshBoxesExcept(TextBox? source)
    {
        var uppercase = UppercaseBox.IsChecked == true;
        var grouped = GroupDigitsBox.IsChecked == true;
        if (source != BinaryBox) BinaryBox.Text = BaseTool.ToBase(_value, 2, uppercase, grouped ? 4 : 0);
        if (source != OctalBox) OctalBox.Text = BaseTool.ToBase(_value, 8, uppercase, grouped ? 3 : 0);
        if (source != DecimalBox) DecimalBox.Text = BaseTool.ToBase(_value, 10, uppercase, grouped ? 3 : 0);
        if (source != HexBox) HexBox.Text = BaseTool.ToBase(_value, 16, uppercase, grouped ? 4 : 0);
    }

    private void RefreshCustomBox()
    {
        if (int.TryParse(CustomRadixBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var radix) && radix is >= 2 and <= 36)
            CustomValueBox.Text = BaseTool.ToBase(_value, radix, UppercaseBox.IsChecked == true);
    }

    private void UpdateStatus()
    {
        var (bits, bytes) = BaseTool.Measure(_value);
        StatusText.Text = $"{bits} bit{(bits == 1 ? "" : "s")} · {bytes} byte{(bytes == 1 ? "" : "s")}";
    }
}
