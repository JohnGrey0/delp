using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("ascii-art", "ASCII Art Text", ToolCategory.DevUtilities,
    "Render text as a large ASCII-art banner in a choice of block fonts.",
    Keywords = "ascii,art,figlet,banner,text", Order = 50)]
public partial class AsciiArtView : UserControl
{
    public AsciiArtView()
    {
        InitializeComponent();
        foreach (var font in AsciiArtTool.FontNames)
            FontCombo.Items.Add(new ComboBoxItem { Content = font });
        FontCombo.SelectedIndex = 0;
        Loaded += (_, _) => Render();
    }

    private void Input_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Render();
    }

    private void Render()
    {
        try
        {
            var font = (FontCombo.SelectedItem as ComboBoxItem)?.Content as string ?? AsciiArtTool.FontNames[0];
            OutputText.Text = AsciiArtTool.Render(TextInputBox.Text, font);
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            OutputText.Text = "";
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(OutputText.Text, CopyBtn);
}
