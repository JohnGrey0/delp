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
    private readonly UuidBatchController _batch;
    private readonly ErrorBox _error;

    public UuidV7View()
    {
        InitializeComponent();
        _batch = new UuidBatchController(CountBox, OutputBox);
        _error = new ErrorBox(ErrorText);
    }

    private UuidStyle FormatStyle => new(
        Uppercase: UppercaseBox.IsChecked == true,
        Braces: BracesBox.IsChecked == true,
        NoHyphens: NoHyphensBox.IsChecked == true);

    private void Generate_Click(object sender, RoutedEventArgs e) => _error.Run(() =>
    {
        var count = _batch.ParseCount();
        _batch.GenerateAndRender(count, UuidV7.Generate, FormatStyle);
    });

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            _batch.Reformat(FormatStyle);
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

    private void Copy_Click(object sender, RoutedEventArgs e) => UuidOutputCopy.Copy(OutputBox, CopyBtn);

    private void CopyJson_Click(object sender, RoutedEventArgs e) => UuidOutputCopy.CopyAsJson(OutputBox, CopyJsonBtn);
}
