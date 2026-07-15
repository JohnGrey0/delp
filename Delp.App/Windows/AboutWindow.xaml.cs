using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Delp.App.Infrastructure;

namespace Delp.App.Windows;

public partial class AboutWindow : GlassWindow
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = $"Version {AppInfo.Version} — developer toolbox";
        LicenseText.Text = AppInfo.LicenseLine;

        AddRow("Version", AppInfo.Version);
        AddRow("Tools", AppInfo.ToolCount.ToString());
        AddRow("Runtime", AppInfo.Framework);
        AddRow("OS", AppInfo.Os);
        AddRow("Install folder", AppInfo.InstallDirectory);
        AddRow("Settings", AppInfo.SettingsPath);
    }

    /// <summary>Opens the dialog centered on its owner (or the screen from the tray).</summary>
    public static void Open(Window? owner)
    {
        var about = new AboutWindow();
        if (owner is { IsVisible: true })
        {
            about.Owner = owner;
        }
        else
        {
            about.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            about.Topmost = true;
        }
        about.ShowDialog();
    }

    private void AddRow(string label, string value)
    {
        var grid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var key = new TextBlock { Text = label.ToUpperInvariant(), FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
        key.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Fg2");
        key.FontWeight = FontWeights.SemiBold;

        var val = new TextBox
        {
            Text = value,
            IsReadOnly = true,
            BorderThickness = new Thickness(0),
            Background = System.Windows.Media.Brushes.Transparent,
            Padding = new Thickness(0),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(val, 1);

        grid.Children.Add(key);
        grid.Children.Add(val);
        MetaRows.Children.Add(grid);
    }

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    private void GitHub_Click(object sender, RoutedEventArgs e) => OpenUrl(AppInfo.RepositoryUrl);

    private void Issues_Click(object sender, RoutedEventArgs e) => OpenUrl(AppInfo.IssuesUrl);

    private void CopyInfo_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(AppInfo.DiagnosticsText, CopyInfoBtn);

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed && e.OriginalSource is not TextBox)
            DragMove();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}
