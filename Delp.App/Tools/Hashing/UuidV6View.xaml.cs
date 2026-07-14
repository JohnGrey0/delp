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
    private readonly List<Guid> _guids = [];

    public UuidV6View()
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
            var node = UseMacBox.IsChecked == true ? UuidNode.RealMacNode() : UuidNode.RandomNode();
            var clockSeq = UuidNode.RandomClockSequence();

            _guids.Clear();
            Guid Generate() => UuidV6.Generate(node, clockSeq);
            var formatted = UuidBatch.Generate(Capture(Generate), count, FormatStyle);
            OutputBox.Text = string.Join(Environment.NewLine, formatted);
            UpdateTimestampNote();
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

    private void UpdateTimestampNote()
    {
        if (_guids.Count == 0)
        {
            TimestampNote.Text = "";
            return;
        }

        var ts = UuidV6.DecodeTimestamp(_guids[0]);
        TimestampNote.Text =
            $"First UUID timestamp: {ts.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff} local / {ts.UtcDateTime:yyyy-MM-dd HH:mm:ss.fff} UTC";
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
