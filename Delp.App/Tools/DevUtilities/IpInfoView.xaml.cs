using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("ip-info", "IP Address Info", ToolCategory.DevUtilities,
    "Inspect an IPv4/IPv6 address or CIDR block: classification, integer/binary forms, PTR name, and CIDR math — fully offline.",
    Keywords = "ip,ipv4,ipv6,cidr,subnet,private", Order = 90)]
public partial class IpInfoView : UserControl
{
    private bool _updating;

    public IpInfoView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Refresh();
            RefreshAdapters();
        };
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded)
            Run(Refresh);
    }

    private void RefreshAdapters_Click(object sender, RoutedEventArgs e) => RefreshAdapters();

    private void ShowAllBox_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            RefreshAdapters();
    }

    private void Refresh()
    {
        var text = InputBox.Text.Trim();
        if (text.Length == 0)
        {
            ResultCard.Visibility = Visibility.Collapsed;
            return;
        }

        var rows = new List<Row>();
        string canonical;
        string classification;
        bool isGlobal;

        if (text.Contains('/'))
        {
            var addressPart = text[..text.IndexOf('/')];
            var address = IpTool.Analyze(addressPart);
            var cidr = IpTool.AnalyzeCidr(text);

            canonical = $"{address.Canonical}/{cidr.PrefixLength}";
            classification = address.Classification;
            isGlobal = address.IsGlobal;

            rows.Add(new Row("Version", $"IPv{address.Version}"));
            rows.Add(new Row("Network", cidr.Network));
            if (cidr.Broadcast is not null)
                rows.Add(new Row("Broadcast", cidr.Broadcast));
            rows.Add(new Row("First usable", cidr.FirstUsable));
            rows.Add(new Row("Last usable", cidr.LastUsable));
            rows.Add(new Row("Host count", cidr.HostCount));
            if (cidr.PrefixMaskDotted is not null)
                rows.Add(new Row("Prefix mask", cidr.PrefixMaskDotted));
            rows.Add(new Row("PTR name", address.PtrName));
        }
        else
        {
            var address = IpTool.Analyze(text);
            canonical = address.Canonical;
            classification = address.Classification;
            isGlobal = address.IsGlobal;

            rows.Add(new Row("Version", $"IPv{address.Version}"));
            rows.Add(new Row("Integer form", address.IntegerForm));
            if (address.BinaryForm is not null)
                rows.Add(new Row("Binary form", address.BinaryForm));
            rows.Add(new Row("PTR name", address.PtrName));
        }

        ResultCard.Visibility = Visibility.Visible;
        CanonicalText.Text = canonical;
        ClassificationBadge.Text = classification;
        ClassificationBadge.Foreground = (Brush)FindResource(isGlobal ? "Brush.Success" : "Brush.Warning");
        RowsList.ItemsSource = rows;
    }

    private void RefreshAdapters()
    {
        var showAll = ShowAllBox.IsChecked == true;
        var adapters = IpTool.LocalAdapters()
            .Where(a => showAll || a.Status == OperationalStatus.Up.ToString())
            .ToList();
        AdaptersList.ItemsSource = adapters;
    }

    /// <summary>Runs a conversion with reentrancy protection and inline error reporting.</summary>
    private void Run(Action convert)
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            convert();
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
            ResultCard.Visibility = Visibility.Collapsed;
        }
        finally
        {
            _updating = false;
        }
    }

    private sealed record Row(string Label, string Value);
}
