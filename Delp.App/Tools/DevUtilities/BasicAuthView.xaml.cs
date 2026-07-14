using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("basic-auth", "Basic Auth Header Builder", ToolCategory.DevUtilities,
    "Build and decode HTTP Basic Authentication headers, with ready-to-paste curl forms.",
    Keywords = "basic,auth,authorization,header,curl", Order = 120)]
public partial class BasicAuthView : UserControl
{
    private bool _updating;

    public BasicAuthView()
    {
        InitializeComponent();
        Loaded += (_, _) => Run();
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Run();
    }

    private void Run()
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            if (ModeTabs.SelectedIndex == 1)
            {
                var creds = BasicAuthTool.Decode(DecodeInputBox.Text);
                DecodedUserBox.Text = creds.Username;
                DecodedPasswordBox.Text = creds.Password;
            }
            else
            {
                var result = BasicAuthTool.Encode(UserBox.Text, PasswordBox.Text);
                HeaderBox.Text = result.Header;
                CurlHeaderBox.Text = result.CurlHeaderFlag;
                CurlUserBox.Text = result.CurlUserFlag;
            }
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

    private void CopyHeader_Click(object sender, RoutedEventArgs e) => Ui.Copy(HeaderBox.Text, CopyHeaderBtn);
    private void CopyCurlHeader_Click(object sender, RoutedEventArgs e) => Ui.Copy(CurlHeaderBox.Text, CopyCurlHeaderBtn);
    private void CopyCurlUser_Click(object sender, RoutedEventArgs e) => Ui.Copy(CurlUserBox.Text, CopyCurlUserBtn);
    private void CopyUser_Click(object sender, RoutedEventArgs e) => Ui.Copy(DecodedUserBox.Text, CopyUserBtn);
    private void CopyPass_Click(object sender, RoutedEventArgs e) => Ui.Copy(DecodedPasswordBox.Text, CopyPassBtn);
}
