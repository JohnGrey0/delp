using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;

namespace Delp.App.Tools.Hashing;

[Tool("uuid-v3", "UUID v3 (MD5 Name-based)", ToolCategory.Hashing,
    "Generate a deterministic RFC 9562 version 3 UUID from a namespace and name using MD5.",
    Keywords = "uuid,guid,v3,md5,namespace,rfc9562", Order = 130)]
public partial class UuidV3View : UserControl
{
    public UuidV3View()
    {
        InitializeComponent();
    }

    private UuidStyle FormatStyle => new(
        Uppercase: UppercaseBox.IsChecked == true,
        Braces: BracesBox.IsChecked == true,
        NoHyphens: NoHyphensBox.IsChecked == true);

    private void View_Loaded(object sender, RoutedEventArgs e) => Recompute();

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;
        CustomNsPanel.Visibility = NamespaceCombo.SelectedIndex == 4 ? Visibility.Visible : Visibility.Collapsed;
        Recompute();
    }

    private void Recompute()
    {
        try
        {
            var ns = ResolveNamespace();
            var guid = UuidNameBased.GenerateV3(ns, NameBox.Text);
            OutputBox.Text = UuidFormat.Apply(guid, FormatStyle);
            HideError();
        }
        catch (Exception ex)
        {
            OutputBox.Text = "";
            ShowError(ex);
        }
    }

    private Guid ResolveNamespace() => NamespaceCombo.SelectedIndex switch
    {
        0 => UuidNamespaces.Dns,
        1 => UuidNamespaces.Url,
        2 => UuidNamespaces.Oid,
        3 => UuidNamespaces.X500,
        _ => UuidNameBased.ParseNamespace(CustomNsBox.Text),
    };

    private void HideError() => ErrorText.Visibility = Visibility.Collapsed;

    private void ShowError(Exception ex)
    {
        ErrorText.Text = ex.Message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(OutputBox.Text, CopyBtn);

    private void CopyJson_Click(object sender, RoutedEventArgs e)
    {
        var lines = OutputBox.Text.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
        Ui.Copy(System.Text.Json.JsonSerializer.Serialize(lines), CopyJsonBtn);
    }
}
