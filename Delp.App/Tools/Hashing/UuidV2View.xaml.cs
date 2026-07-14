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
    private readonly UuidBatchController _batch;
    private readonly ErrorBox _error;

    public UuidV2View()
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
        if (!uint.TryParse(LocalIdBox.Text.Trim(), out var localId))
            throw new FormatException("Local ID must be a whole number from 0 to 4294967295.");
        var domain = (DceDomain)Math.Max(0, DomainCombo.SelectedIndex);
        // v2's layout overwrites time_low with the local ID and clock_seq_low with the domain,
        // so the visible timestamp only ticks every ~7 minutes. A fixed node + clock sequence
        // would therefore render an entire batch identical — randomize both per UUID instead.
        _batch.GenerateAndRender(count,
            () => UuidV2.Generate(localId, domain, UuidNode.RandomNode(), UuidNode.RandomClockSequence()),
            FormatStyle);
    });

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            _batch.Reformat(FormatStyle);
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => UuidOutputCopy.Copy(OutputBox, CopyBtn);

    private void CopyJson_Click(object sender, RoutedEventArgs e) => UuidOutputCopy.CopyAsJson(OutputBox, CopyJsonBtn);
}
