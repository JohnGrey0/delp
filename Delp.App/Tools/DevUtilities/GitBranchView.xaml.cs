using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("git-branch", "Git Branch Name Generator", ToolCategory.DevUtilities,
    "Turn a description and ticket number into a clean, git-safe branch name.",
    Keywords = "git,branch,name,slug,ticket", Order = 30)]
public partial class GitBranchView : UserControl
{
    public GitBranchView()
    {
        InitializeComponent();
        TypeCombo.SelectedIndex = 0;
        Loaded += (_, _) =>
        {
            GenerateName();
            CheckName();
        };
    }

    private string SelectedType =>
        (TypeCombo.SelectedItem as ComboBoxItem)?.Tag as string == "custom"
            ? CustomTypeBox.Text
            : (TypeCombo.SelectedItem as ComboBoxItem)?.Content as string ?? "feature";

    private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        var isCustom = (TypeCombo.SelectedItem as ComboBoxItem)?.Tag as string == "custom";
        CustomTypeBox.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
        GenerateName();
    }

    private void Input_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            GenerateName();
    }

    private void GenerateName()
    {
        try
        {
            var template = string.IsNullOrWhiteSpace(TemplateBox.Text) ? BranchOptions.DefaultTemplate : TemplateBox.Text;
            var options = new BranchOptions(SelectedType, TicketBox.Text, template, BranchOptions.DefaultMaxLength);
            var name = BranchTool.Make(DescriptionBox.Text, options);

            BranchNameBox.Text = name;
            CheckoutBox.Text = BranchTool.CheckoutCommand(name);
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            BranchNameBox.Text = "";
            CheckoutBox.Text = "";
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void Check_TextChanged(object sender, TextChangedEventArgs e) => CheckName();

    private void CheckName()
    {
        var name = CheckNameBox.Text;
        if (string.IsNullOrEmpty(name))
        {
            CheckResultText.Text = "";
            return;
        }

        var violations = BranchTool.Validate(name);
        if (violations.Count == 0)
        {
            CheckResultText.Text = "✓ Valid branch name.";
            CheckResultText.Foreground = (Brush)FindResource("Brush.Success");
        }
        else
        {
            CheckResultText.Text = "✗ " + string.Join("\n✗ ", violations);
            CheckResultText.Foreground = (Brush)FindResource("Brush.Danger");
        }
    }

    private void CopyName_Click(object sender, RoutedEventArgs e) => Ui.Copy(BranchNameBox.Text, CopyNameBtn);

    private void CopyCheckout_Click(object sender, RoutedEventArgs e) => Ui.Copy(CheckoutBox.Text, CopyCheckoutBtn);
}
