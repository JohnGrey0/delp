using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;

namespace Delp.App.Tools.Hashing;

[Tool("uuid-v7", "UUID v7 (Unix epoch, sortable)", ToolCategory.Hashing,
    "Generate RFC 9562 version 7 UUIDs from the Unix epoch millisecond timestamp, and decode existing ones.",
    Keywords = "uuid,guid,v7,epoch,sortable,rfc9562", Order = 170)]
public partial class UuidV7View : UserControl
{
    private readonly List<Guid> _guids = [];

    public UuidV7View()
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
            _guids.Clear();
            var formatted = UuidBatch.Generate(Capture(UuidV7.Generate), count, FormatStyle);
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

    private void DecodeBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var text = DecodeBox.Text.Trim();
        if (text.Length == 0)
        {
            DecodeResult.Text = "";
            return;
        }

        if (!Guid.TryParse(text, out var guid))
        {
            DecodeResult.Text = "Not a valid UUID.";
            return;
        }

        try
        {
            var ts = UuidV7.DecodeTimestamp(guid);
            DecodeResult.Text =
                $"Timestamp: {ts.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff} local / {ts.UtcDateTime:yyyy-MM-dd HH:mm:ss.fff} UTC";
        }
        catch (Exception ex)
        {
            DecodeResult.Text = ex.Message;
        }
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
