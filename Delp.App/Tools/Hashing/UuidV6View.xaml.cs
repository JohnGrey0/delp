using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;

namespace Delp.App.Tools.Hashing;

[Tool("uuid-v6", "UUID v6 (Sortable time-based)", ToolCategory.Hashing,
    "Generate RFC 9562 version 6 UUIDs, a reordered v1 layout that sorts lexically by time.",
    Keywords = "uuid,guid,v6,sortable,rfc9562", Order = 160)]
public partial class UuidV6View : UserControl
{
    private readonly UuidBatchController _batch;
    private readonly ErrorBox _error;

    public UuidV6View()
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
        _batch.GenerateAndRender(count, () => UuidV6.Generate(node, clockSeq), FormatStyle);
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

        var ts = UuidV6.DecodeTimestamp(_batch.Guids[0]);
        TimestampNote.Text =
            $"First UUID timestamp: {ts.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff} local / {ts.UtcDateTime:yyyy-MM-dd HH:mm:ss.fff} UTC";
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => UuidOutputCopy.Copy(OutputBox, CopyBtn);

    private void CopyJson_Click(object sender, RoutedEventArgs e) => UuidOutputCopy.CopyAsJson(OutputBox, CopyJsonBtn);
}
