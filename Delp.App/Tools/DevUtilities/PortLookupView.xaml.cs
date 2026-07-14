using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("port-lookup", "Port Number Reference", ToolCategory.DevUtilities,
    "Look up well-known and commonly-used TCP/UDP ports by number, service, or description.",
    Keywords = "port,tcp,udp,well-known,service", Order = 80)]
public partial class PortLookupView : UserControl
{
    public PortLookupView()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh();

    private void Refresh()
    {
        var results = PortData.Search(SearchBox.Text);
        ResultsList.ItemsSource = results;
        CountText.Text = $"{results.Count} result{(results.Count == 1 ? "" : "s")}";
    }
}
