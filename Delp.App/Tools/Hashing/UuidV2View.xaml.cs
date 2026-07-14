using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;

namespace Delp.App.Tools.Hashing;

[Tool("uuid-v2", "UUID v2 (DCE Security)", ToolCategory.Hashing,
    "Generate RFC 9562 version 2 DCE Security UUIDs with an embedded local ID and domain.",
    Keywords = "uuid,guid,v2,dce,posix,rfc9562", Order = 120)]
public partial class UuidV2View : UserControl
{
    private readonly List<Guid> _guids = [];

    public UuidV2View()
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
            if (!uint.TryParse(LocalIdBox.Text.Trim(), out var localId))
                throw new FormatException("Local ID must be a whole number from 0 to 4294967295.");
            var domain = (DceDomain)Math.Max(0, DomainCombo.SelectedIndex);
            var node = UuidNode.RandomNode();
            var clockSeq = UuidNode.RandomClockSequence();

            _guids.Clear();
            Guid Generate() => UuidV2.Generate(localId, domain, node, clockSeq);
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
