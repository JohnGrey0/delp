using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;

namespace Delp.App.Tools.Hashing;

[Tool("uuid-v8", "UUID v8 (Custom)", ToolCategory.Hashing,
    "Generate RFC 9562 version 8 UUIDs with your own payload in the 122 free bits.",
    Keywords = "uuid,guid,v8,custom,vendor,rfc9562", Order = 180)]
public partial class UuidV8View : UserControl
{
    private readonly List<Guid> _guids = [];

    public UuidV8View()
    {
        InitializeComponent();
    }

    private UuidStyle FormatStyle => new(
        Uppercase: UppercaseBox.IsChecked == true,
        Braces: BracesBox.IsChecked == true,
        NoHyphens: NoHyphensBox.IsChecked == true);

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var count = ParseCount();
            var customHex = CustomHexBox.Text;
            var randomFill = RandomFillBox.IsChecked == true;

            _guids.Clear();
            Guid Generate() => UuidV8.Generate(customHex, randomFill);
            var formatted = UuidBatch.Generate(Capture(Generate), count, FormatStyle);
            OutputBox.Text = string.Join(Environment.NewLine, formatted);
            HideError();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded && _guids.Count > 0)
            OutputBox.Text = string.Join(Environment.NewLine, _guids.Select(g => UuidFormat.Apply(g, FormatStyle)));
    }

    private int ParseCount()
    {
        if (!int.TryParse(CountBox.Text.Trim(), out var count))
            throw new FormatException("Count must be a whole number.");
        return count;
    }

    private Func<Guid> Capture(Func<Guid> generator) => () =>
    {
        var g = generator();
        _guids.Add(g);
        return g;
    };

    private void HideError() => ErrorText.Visibility = Visibility.Collapsed;

    private void ShowError(Exception ex)
    {
        ErrorText.Text = ex.Message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(OutputBox.Text, CopyBtn);

    private void CopyJson_Click(object sender, RoutedEventArgs e)
    {
        var lines = OutputBox.Text.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
        Ui.Copy(System.Text.Json.JsonSerializer.Serialize(lines), CopyJsonBtn);
    }
}
