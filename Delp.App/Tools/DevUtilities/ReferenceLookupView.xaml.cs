using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("reference", "Reference Lookup", ToolCategory.DevUtilities,
    "Look up HTTP status codes, MIME types, and well-known ports in one place.",
    Keywords = "http,status,mime,content-type,port,tcp,udp,reference,lookup,http-status,mime-lookup,port-lookup", Order = 60)]
public partial class ReferenceLookupView : UserControl
{
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
}
