using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("url-slug", "URL Slug Generator", ToolCategory.TextProcessing,
    "Turn any text into a clean, URL-friendly slug with diacritic stripping and stopword removal.",
    Keywords = "slug,url,seo,kebab,permalink", Order = 120)]
public partial class UrlSlugView : UserControl
{
    private bool _updating;

    public UrlSlugView()
    {
        InitializeComponent();
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh();

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Refresh();
    }

    private void Refresh()
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            int? maxLength = null;
            if (int.TryParse(MaxLengthBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
                maxLength = parsed;

            var options = new SlugOptions(
                Separator: UnderscoreBox.IsChecked == true ? '_' : '-',
                Lowercase: LowercaseBox.IsChecked == true,
                MaxLength: maxLength,
                RemoveStopwords: RemoveStopwordsBox.IsChecked == true);

            OutputBox.Text = SlugTool.Make(InputBox.Text, options);
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            _updating = false;
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(OutputBox.Text, CopyBtn);
}
