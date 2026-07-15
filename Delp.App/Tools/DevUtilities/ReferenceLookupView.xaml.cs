using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("reference", "Reference Lookup", ToolCategory.DevUtilities,
    "Look up HTTP status codes, MIME types, ports, user agent strings, and chmod/byte-size conversions.",
    Keywords = "http,status,mime,content-type,port,tcp,udp,reference,lookup,http-status,mime-lookup,port-lookup," +
               "user agent,ua,browser,chmod,permissions,octal,bytes,kib,size,bandwidth", Order = 60)]
public partial class ReferenceLookupView : UserControl
{
    // Shared reentrancy guard for the CHMOD tab's grid/octal/symbolic sync (same pattern as
    // Base64View) and for the live-parse tabs below — only one of these tabs is ever being
    // edited by the user at a time, so one flag is enough.
    private bool _updating;
    private int _uaSampleIndex = -1;

    public ReferenceLookupView()
    {
        InitializeComponent();

        // Each dataset (HttpStatusData/MimeData/PortData) is only touched here, in Loaded,
        // which fires once when this tool is first opened — not at app startup, and not
        // per-tab-selection (WPF only realizes the selected TabItem's visual tree anyway,
        // so all three still populate together the first time the tool is shown).
        Loaded += (_, _) =>
        {
            ApplyHttpFilter();
            RefreshMime();
            RefreshPort();
            ApplyChmod(ReadPermissionsFromGrid());
            RefreshBytes();
        };
    }

    // ---- HTTP STATUS ----

    private void HttpFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            ApplyHttpFilter();
    }

    private void ApplyHttpFilter()
    {
        var selectedGroup = (HttpClassFilter.SelectedItem as ComboBoxItem)?.Content as string ?? "All classes";
        var results = HttpStatusData.Search(HttpSearchBox.Text);

        if (selectedGroup != "All classes")
            results = results.Where(e => HttpStatusData.GroupLabel(e.Code) == selectedGroup).ToList();

        HttpResultsList.ItemsSource = results;
        HttpCountText.Text = $"{results.Count} of {HttpStatusData.All.Count} codes";

        if (results.Count > 0)
            HttpResultsList.SelectedIndex = 0;
        else
            ShowHttpDetail(null);
    }

    private void HttpResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ShowHttpDetail(HttpResultsList.SelectedItem as HttpStatusEntry);

    private void ShowHttpDetail(HttpStatusEntry? entry)
    {
        if (entry is null)
        {
            HttpEmptyText.Visibility = Visibility.Visible;
            HttpDetailContent.Visibility = Visibility.Collapsed;
            return;
        }

        HttpEmptyText.Visibility = Visibility.Collapsed;
        HttpDetailContent.Visibility = Visibility.Visible;

        HttpDetailCodeName.Text = $"{entry.Code} {entry.Name}";
        HttpDetailSummary.Text = entry.Summary;
        HttpDetailWhen.Text = entry.When;
        HttpDetailRfc.Text = string.IsNullOrEmpty(entry.Rfc) ? "" : entry.Rfc;

        HttpDetailClassBadge.Text = entry.Class;
        HttpDetailClassBadge.Foreground = BrushForHttpClass(entry.Class);
    }

    private Brush BrushForHttpClass(string className) => (Brush)(className switch
    {
        HttpStatusData.Success => FindResource("Brush.Success"),
        HttpStatusData.Redirection => FindResource("Brush.Accent"),
        HttpStatusData.ClientError => FindResource("Brush.Warning"),
        HttpStatusData.ServerError => FindResource("Brush.Danger"),
        _ => FindResource("Brush.Fg2"),
    });

    // ---- MIME TYPES ----

    private void MimeSearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshMime();

    private void RefreshMime()
    {
        var results = MimeData.Search(MimeSearchBox.Text);
        MimeResultsList.ItemsSource = results;
        MimeCountText.Text = $"{results.Count} result{(results.Count == 1 ? "" : "s")}";
    }

    private void CopyMime_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string mime } button)
            Ui.Copy(mime, button);
    }

    // ---- PORTS ----

    private void PortSearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshPort();

    private void RefreshPort()
    {
        var results = PortData.Search(PortSearchBox.Text);
        PortResultsList.ItemsSource = results;
        PortCountText.Text = $"{results.Count} result{(results.Count == 1 ? "" : "s")}";
    }

    // ---- USER AGENT ----

    private void UaSample_Click(object sender, RoutedEventArgs e)
    {
        _uaSampleIndex = (_uaSampleIndex + 1) % UserAgentData.SampleUserAgents.Count;
        UaInputBox.Text = UserAgentData.SampleUserAgents[_uaSampleIndex];
    }

    private void UaInput_Changed(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        Run(() =>
        {
            var text = UaInputBox.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                UaResultList.ItemsSource = null;
                UaBotBadge.Visibility = Visibility.Collapsed;
                return;
            }

            var result = UserAgentTool.Parse(text);

            UaResultList.ItemsSource = new List<LabeledValue>
            {
                new("Browser", Combine(result.Browser, result.BrowserVersion)),
                new("Engine", Combine(result.Engine, result.EngineVersion)),
                new("Operating System", Combine(result.Os, result.OsVersion)),
                new("Device Type", result.DeviceType),
            };

            if (result.IsBot)
            {
                UaBotBadgeText.Text = $"Bot detected — {result.BotName}";
                UaBotBadge.Visibility = Visibility.Visible;
            }
            else
            {
                UaBotBadge.Visibility = Visibility.Collapsed;
            }
        }, UaErrorText);
    }

    private static string Combine(string label, string? version) =>
        version is null ? label : $"{label} {version}";

    // ---- CHMOD ----

    private void Grid_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;
        Run(() => ApplyChmod(ReadPermissionsFromGrid(), updateGrid: false), ChmodErrorText);
    }

    private void Octal_Changed(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded)
            return;
        Run(() => ApplyChmod(ChmodTool.FromOctal(OctalBox.Text), updateOctal: false), ChmodErrorText);
    }

    private void Symbolic_Changed(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded)
            return;
        Run(() => ApplyChmod(ChmodTool.FromSymbolic(SymbolicBox.Text), updateSymbolic: false), ChmodErrorText);
    }

    private void CopyCommand_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(CommandBox.Text, CopyCommandBtn);

    private ChmodPermissions ReadPermissionsFromGrid() => new(
        OwnerReadBox.IsChecked == true, OwnerWriteBox.IsChecked == true, OwnerExecBox.IsChecked == true,
        GroupReadBox.IsChecked == true, GroupWriteBox.IsChecked == true, GroupExecBox.IsChecked == true,
        OtherReadBox.IsChecked == true, OtherWriteBox.IsChecked == true, OtherExecBox.IsChecked == true,
        SetuidBox.IsChecked == true, SetgidBox.IsChecked == true, StickyBox.IsChecked == true);

    private void ApplyChmod(ChmodPermissions p, bool updateGrid = true, bool updateOctal = true, bool updateSymbolic = true)
    {
        if (updateGrid)
        {
            OwnerReadBox.IsChecked = p.OwnerRead;
            OwnerWriteBox.IsChecked = p.OwnerWrite;
            OwnerExecBox.IsChecked = p.OwnerExecute;
            GroupReadBox.IsChecked = p.GroupRead;
            GroupWriteBox.IsChecked = p.GroupWrite;
            GroupExecBox.IsChecked = p.GroupExecute;
            OtherReadBox.IsChecked = p.OtherRead;
            OtherWriteBox.IsChecked = p.OtherWrite;
            OtherExecBox.IsChecked = p.OtherExecute;
            SetuidBox.IsChecked = p.Setuid;
            SetgidBox.IsChecked = p.Setgid;
            StickyBox.IsChecked = p.Sticky;
        }

        if (updateOctal)
            OctalBox.Text = ChmodTool.ToOctalString(p);
        if (updateSymbolic)
            SymbolicBox.Text = ChmodTool.ToSymbolic(p);

        CommandBox.Text = ChmodTool.ToCommand(p);
    }

    // ---- BYTES ----

    private void Bytes_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;
        RefreshBytes();
    }

    private void RefreshBytes()
    {
        Run(() =>
        {
            if (!decimal.TryParse(ByteValueBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                throw new FormatException("Enter a valid non-negative number.");

            var unitTag = (ByteUnitCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "B";
            var unit = Enum.Parse<ByteUnit>(unitTag);
            var result = ByteSizeTool.Convert(value, unit);

            ByteEquivalentsList.ItemsSource = result.Equivalents
                .Select(eq => new LabeledValue(eq.Label, FormatNumber(eq.Value)))
                .ToList();
            TransferTimeList.ItemsSource = result.TransferTimes;
        }, BytesErrorText);
    }

    private static string FormatNumber(decimal value) => value.ToString("#,##0.####", CultureInfo.InvariantCulture);

    // ---- shared ----

    /// <summary>Runs a conversion with reentrancy protection and inline error reporting (Base64View pattern).</summary>
    private void Run(Action convert, TextBlock errorText)
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            convert();
            errorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            errorText.Text = ex.Message;
            errorText.Visibility = Visibility.Visible;
        }
        finally
        {
            _updating = false;
        }
    }

    private sealed record LabeledValue(string Label, string Value);
}
