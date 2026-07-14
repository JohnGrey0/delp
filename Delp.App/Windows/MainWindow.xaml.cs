using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Delp.App.Infrastructure;

namespace Delp.App.Windows;

public partial class MainWindow : GlassWindow
{
    private readonly Dictionary<string, UserControl> _views = new();

    public MainWindow()
    {
        InitializeComponent();
        WelcomeHint.Text =
            $"{ToolCatalog.All.Count} tools ready. Pick one from the sidebar, or press Ctrl+Alt+Space anywhere for the quick panel.";
        RefreshNav(null);
    }

    public void SelectTool(string id)
    {
        var info = ToolCatalog.Find(id);
        if (info is null)
            return;
        if (NavList.Items.Contains(info))
            NavList.SelectedItem = info; // triggers OpenTool via SelectionChanged
        else
            OpenTool(info);
    }

    private void RefreshNav(string? query)
    {
        var view = new ListCollectionView(ToolCatalog.Search(query).ToList());
        if (string.IsNullOrWhiteSpace(query))
            view.GroupDescriptions!.Add(new PropertyGroupDescription(nameof(ToolInfo.CategoryName)));
        NavList.ItemsSource = view;
    }

    private void OpenTool(ToolInfo info)
    {
        if (!_views.TryGetValue(info.Id, out var view))
        {
            view = ToolCatalog.CreateView(info);
            _views[info.Id] = view;
        }

        ToolTitle.Text = info.Name;
        ToolDesc.Text = info.Description;
        ToolHost.Content = view;
        HeaderPanel.Visibility = Visibility.Visible;
        Welcome.Visibility = Visibility.Collapsed;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Closing hides to the tray; "Exit" lives in the tray menu.
        e.Cancel = true;
        Hide();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) =>
        RefreshNav(SearchBox.Text);

    private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is ToolInfo info)
            OpenTool(info);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
