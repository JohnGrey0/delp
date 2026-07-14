using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("mime-lookup", "MIME Type Lookup", ToolCategory.DevUtilities,
    "Look up the MIME type for a file extension, or the extensions for a MIME type.",
    Keywords = "mime,content-type,extension,media", Order = 70)]
public partial class MimeLookupView : UserControl
{
    public MimeLookupView()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh();

    private void Refresh()
    {
        var results = MimeData.Search(SearchBox.Text);
        ResultsList.ItemsSource = results;
        CountText.Text = $"{results.Count} result{(results.Count == 1 ? "" : "s")}";
    }

    private void CopyMime_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string mime } button)
            Ui.Copy(mime, button);
    }
}
