using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("http-status", "HTTP Status Codes", ToolCategory.DevUtilities,
    "Look up HTTP status codes with practical guidance on when to return each one.",
    Keywords = "http,status,codes,404,500,reference", Order = 60)]
public partial class HttpStatusView : UserControl
{
    public HttpStatusView()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyFilter();
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            ApplyFilter();
    }

    private void ApplyFilter()
    {
        var selectedGroup = (ClassFilter.SelectedItem as ComboBoxItem)?.Content as string ?? "All classes";
        var results = HttpStatusData.Search(SearchBox.Text);

        if (selectedGroup != "All classes")
            results = results.Where(e => HttpStatusData.GroupLabel(e.Code) == selectedGroup).ToList();

        ResultsList.ItemsSource = results;
        CountText.Text = $"{results.Count} of {HttpStatusData.All.Count} codes";

        if (results.Count > 0)
            ResultsList.SelectedIndex = 0;
        else
            ShowDetail(null);
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ShowDetail(ResultsList.SelectedItem as HttpStatusEntry);

    private void ShowDetail(HttpStatusEntry? entry)
    {
        if (entry is null)
        {
            EmptyText.Visibility = Visibility.Visible;
            DetailContent.Visibility = Visibility.Collapsed;
            return;
        }

        EmptyText.Visibility = Visibility.Collapsed;
        DetailContent.Visibility = Visibility.Visible;

        DetailCodeName.Text = $"{entry.Code} {entry.Name}";
        DetailSummary.Text = entry.Summary;
        DetailWhen.Text = entry.When;
        DetailRfc.Text = string.IsNullOrEmpty(entry.Rfc) ? "" : entry.Rfc;

        DetailClassBadge.Text = entry.Class;
        DetailClassBadge.Foreground = BrushForClass(entry.Class);
    }

    private Brush BrushForClass(string className) => (Brush)(className switch
    {
        HttpStatusData.Success => FindResource("Brush.Success"),
        HttpStatusData.Redirection => FindResource("Brush.Accent"),
        HttpStatusData.ClientError => FindResource("Brush.Warning"),
        HttpStatusData.ServerError => FindResource("Brush.Danger"),
        _ => FindResource("Brush.Fg2"),
    });
}
