using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;

namespace Delp.App.Tools.Hashing;

[Tool("uuid-v1", "UUID v1 (Time-based)", ToolCategory.Hashing,
    "Generate RFC 9562 version 1 time-based UUIDs from a Gregorian timestamp, clock sequence and node.",
    Keywords = "uuid,guid,v1,time,mac,rfc9562", Order = 110)]
public partial class UuidV1View : UserControl
{
    private readonly UuidBatchController _batch;
    private readonly ErrorBox _error;

    public UuidV1View()
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
        var node = UseMacBox.IsChecked == true ? UuidNode.RealMacNode() : UuidNode.RandomNode();
        var clockSeq = UuidNode.RandomClockSequence();
        _batch.GenerateAndRender(count, () => UuidV1.Generate(node, clockSeq), FormatStyle);
        UpdateTimestampNote();
    });

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            _batch.Reformat(FormatStyle);
    }

    private void UpdateTimestampNote()
    {
        if (_batch.Guids.Count == 0)
        {
            TimestampNote.Text = "";
            return;
        }

        var ts = UuidV1.DecodeTimestamp(_batch.Guids[0]);
        TimestampNote.Text =
            $"First UUID timestamp: {ts.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff} local / {ts.UtcDateTime:yyyy-MM-dd HH:mm:ss.fff} UTC";
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => UuidOutputCopy.Copy(OutputBox, CopyBtn);

    private void CopyJson_Click(object sender, RoutedEventArgs e) => UuidOutputCopy.CopyAsJson(OutputBox, CopyJsonBtn);
}
