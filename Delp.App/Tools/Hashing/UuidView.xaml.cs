using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;

namespace Delp.App.Tools.Hashing;

[Tool("uuid", "UUID Generator", ToolCategory.Hashing,
    "Generate and decode RFC 9562 UUIDs across every version — time-based, random, name-based, sortable and custom.",
    Keywords = "uuid,guid,v1,v2,v3,v4,v5,v6,v7,v8,time-based,random,namespace,sortable,epoch,custom,rfc9562," +
               "uuid-v1,uuid-v2,uuid-v3,uuid-v4,uuid-v5,uuid-v6,uuid-v7,uuid-v8,mac,dce,posix,md5,sha1,vendor,batch,decode",
    Order = 110)]
public partial class UuidView : UserControl
{
    private enum UuidVersion { V1, V2, V3, V4, V5, V6, V7, V8 }

    private readonly UuidBatchController _batch;
    private readonly ErrorBox _error;

    public UuidView()
    {
        InitializeComponent();
        _batch = new UuidBatchController(CountBox, OutputBox);
        _error = new ErrorBox(ErrorText);
    }

    private UuidVersion SelectedVersion => (UuidVersion)VersionCombo.SelectedIndex;

    private static bool IsLive(UuidVersion v) => v is UuidVersion.V3 or UuidVersion.V5;

    private static bool UsesMac(UuidVersion v) => v is UuidVersion.V1 or UuidVersion.V6;

    private static Visibility Show(bool visible) => visible ? Visibility.Visible : Visibility.Collapsed;

    private UuidStyle FormatStyle => new(
        Uppercase: UppercaseBox.IsChecked == true,
        Braces: BracesBox.IsChecked == true,
        NoHyphens: NoHyphensBox.IsChecked == true);

    private void View_Loaded(object sender, RoutedEventArgs e)
    {
        UpdatePanelVisibility();
        if (IsLive(SelectedVersion))
            Recompute();
    }

    private void Version_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        UpdatePanelVisibility();
        _error.HideError();

        if (IsLive(SelectedVersion))
        {
            Recompute();
        }
        else
        {
            // The previous selection's batch (if any) belongs to a different version — drop it
            // rather than leave a stale, mislabeled result sitting in the output box.
            _batch.Clear();
            TimestampNote.Text = "";
        }
    }

    private void UpdatePanelVisibility()
    {
        var v = SelectedVersion;
        var live = IsLive(v);
        var mac = UsesMac(v);

        CountPanel.Visibility = Show(!live);
        GenerateBtn.Visibility = Show(!live);

        DomainPanel.Visibility = Show(v == UuidVersion.V2);
        LocalIdPanel.Visibility = Show(v == UuidVersion.V2);
        V2Note.Visibility = Show(v == UuidVersion.V2);

        NamespacePanel.Visibility = Show(live);
        CustomNsPanel.Visibility = Show(live && NamespaceCombo.SelectedIndex == 4);
        NameRow.Visibility = Show(live);

        CustomHexPanel.Visibility = Show(v == UuidVersion.V8);
        RandomFillBox.Visibility = Show(v == UuidVersion.V8);
        V8Note.Visibility = Show(v == UuidVersion.V8);

        UseMacBox.Visibility = Show(mac);
        TimestampNote.Visibility = Show(mac);

        DecodeCard.Visibility = Show(v == UuidVersion.V7);

        VersionDescription.Text = DescriptionFor(v);
    }

    private static string DescriptionFor(UuidVersion v) => v switch
    {
        UuidVersion.V1 => "Time-based: derived from a Gregorian timestamp, clock sequence and node (MAC or random).",
        UuidVersion.V2 => "DCE Security: embeds a local ID and domain over most of the timestamp.",
        UuidVersion.V3 => "Deterministic: hashed (MD5) from a namespace and name — same inputs always produce the same UUID.",
        UuidVersion.V4 => "Random: cryptographically random, with no embedded structure — the most common version.",
        UuidVersion.V5 => "Deterministic: hashed (SHA-1) from a namespace and name — same inputs always produce the same UUID.",
        UuidVersion.V6 => "Sortable time-based: a reordered v1 layout so lexical string order matches time order.",
        UuidVersion.V7 => "Sortable time-based: derived from the Unix epoch millisecond timestamp.",
        UuidVersion.V8 => "Custom/vendor: 122 free bits for your own payload; only the version and variant bits are fixed.",
        _ => "",
    };

    private void Generate_Click(object sender, RoutedEventArgs e) => _error.Run(() =>
    {
        var count = _batch.ParseCount();
        switch (SelectedVersion)
        {
            case UuidVersion.V1:
            {
                var node = UseMacBox.IsChecked == true ? UuidNode.RealMacNode() : UuidNode.RandomNode();
                var clockSeq = UuidNode.RandomClockSequence();
                _batch.GenerateAndRender(count, () => UuidV1.Generate(node, clockSeq), FormatStyle);
                UpdateTimestampNote(UuidV1.DecodeTimestamp);
                break;
            }
            case UuidVersion.V2:
            {
                if (!uint.TryParse(LocalIdBox.Text.Trim(), out var localId))
                    throw new FormatException("Local ID must be a whole number from 0 to 4294967295.");
                var domain = (DceDomain)Math.Max(0, DomainCombo.SelectedIndex);
                // v2's layout overwrites time_low with the local ID and clock_seq_low with the domain,
                // so the visible timestamp only ticks every ~7 minutes. A fixed node + clock sequence
                // would therefore render an entire batch identical — randomize both per UUID instead.
                _batch.GenerateAndRender(count,
                    () => UuidV2.Generate(localId, domain, UuidNode.RandomNode(), UuidNode.RandomClockSequence()),
                    FormatStyle);
                break;
            }
            case UuidVersion.V4:
                _batch.GenerateAndRender(count, UuidV4.Generate, FormatStyle);
                break;
            case UuidVersion.V6:
            {
                var node = UseMacBox.IsChecked == true ? UuidNode.RealMacNode() : UuidNode.RandomNode();
                var clockSeq = UuidNode.RandomClockSequence();
                _batch.GenerateAndRender(count, () => UuidV6.Generate(node, clockSeq), FormatStyle);
                UpdateTimestampNote(UuidV6.DecodeTimestamp);
                break;
            }
            case UuidVersion.V7:
                _batch.GenerateAndRender(count, UuidV7.Generate, FormatStyle);
                break;
            case UuidVersion.V8:
            {
                var customHex = CustomHexBox.Text;
                var randomFill = RandomFillBox.IsChecked == true;
                _batch.GenerateAndRender(count, () => UuidV8.Generate(customHex, randomFill), FormatStyle);
                break;
            }
        }
    });

    private void UpdateTimestampNote(Func<Guid, DateTimeOffset> decode)
    {
        if (_batch.Guids.Count == 0)
        {
            TimestampNote.Text = "";
            return;
        }

        var ts = decode(_batch.Guids[0]);
        TimestampNote.Text =
            $"First UUID timestamp: {ts.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff} local / {ts.UtcDateTime:yyyy-MM-dd HH:mm:ss.fff} UTC";
    }

    private void FormatOption_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        if (IsLive(SelectedVersion))
            Recompute();
        else
            _batch.Reformat(FormatStyle);
    }

    private void NameBased_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        CustomNsPanel.Visibility = Show(NamespaceCombo.SelectedIndex == 4);
        Recompute();
    }

    private void Recompute()
    {
        try
        {
            var ns = ResolveNamespace();
            var guid = SelectedVersion == UuidVersion.V3
                ? UuidNameBased.GenerateV3(ns, NameBox.Text)
                : UuidNameBased.GenerateV5(ns, NameBox.Text);
            OutputBox.Text = UuidFormat.Apply(guid, FormatStyle);
            _error.HideError();
        }
        catch (Exception ex)
        {
            OutputBox.Text = "";
            _error.ShowError(ex);
        }
    }

    private Guid ResolveNamespace() => NamespaceCombo.SelectedIndex switch
    {
        0 => UuidNamespaces.Dns,
        1 => UuidNamespaces.Url,
        2 => UuidNamespaces.Oid,
        3 => UuidNamespaces.X500,
        _ => UuidNameBased.ParseNamespace(CustomNsBox.Text),
    };

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
