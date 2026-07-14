using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Delp.App.Infrastructure;

namespace Delp.App.Windows;

public partial class MainWindow : GlassWindow
{
    private const string StarOutline = "";
    private const string StarFilled = "";

    private readonly Dictionary<string, UserControl> _views = new();
    private ToolInfo? _currentTool;

    public MainWindow()
    {
        InitializeComponent();
        WelcomeHint.Text =
            $"{ToolCatalog.All.Count} tools ready. Pick one from the sidebar, or press Ctrl+Alt+Space anywhere for the quick panel.";
        SettingsService.FavoritesChanged += OnFavoritesChanged;
        RefreshNav(null);
    }

    public void SelectTool(string id)
    {
        var item = FindNavItem(id);
        if (item is not null)
            NavList.SelectedItem = item; // opens via SelectionChanged
        else if (ToolCatalog.Find(id) is { } info)
            OpenTool(info);
    }

    private NavItem? FindNavItem(string id) =>
        NavList.Items.OfType<NavItem>()
            .Where(n => n.Tool.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n.Group == NavItem.FavoritesGroup ? 1 : 0)
            .FirstOrDefault();

    private void RefreshNav(string? query)
    {
        var searching = !string.IsNullOrWhiteSpace(query);
        var items = searching
            ? ToolCatalog.Search(query).Select(t => new NavItem(t, t.CategoryName)).ToList()
            : NavItem.BuildGrouped(ToolCatalog.All);

        var view = new ListCollectionView(items);
        if (!searching)
            view.GroupDescriptions!.Add(new PropertyGroupDescription(nameof(NavItem.Group)));
        NavList.ItemsSource = view;
    }

    private void OpenTool(ToolInfo info)
    {
        if (!_views.TryGetValue(info.Id, out var view))
        {
            view = ToolCatalog.CreateView(info);
            _views[info.Id] = view;
        }

        _currentTool = info;
        ToolTitle.Text = info.Name;
        ToolDesc.Text = info.Description;
        ToolHost.Content = view;
        HeaderPanel.Visibility = Visibility.Visible;
        Welcome.Visibility = Visibility.Collapsed;
        UpdateStar();
    }

    private void UpdateStar()
    {
        if (_currentTool is null)
            return;
        var favorite = SettingsService.IsFavorite(_currentTool.Id);
        StarBtn.Content = favorite ? StarFilled : StarOutline;
        StarBtn.ToolTip = favorite ? "Remove from favorites" : "Add to favorites";
    }

    private void Star_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTool is not null)
            SettingsService.ToggleFavorite(_currentTool.Id);
    }

    private void OnFavoritesChanged()
    {
        RefreshNav(SearchBox.Text);
        UpdateStar();
    }

    private void GroupToggle_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggle && toggle.DataContext is CollectionViewGroup group)
            toggle.IsChecked = !SettingsService.IsGroupCollapsed(group.Name?.ToString() ?? "");
    }

    private void GroupToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton { IsLoaded: true } toggle
            && toggle.DataContext is CollectionViewGroup group)
            SettingsService.SetGroupCollapsed(group.Name?.ToString() ?? "", toggle.IsChecked != true);
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
        if (NavList.SelectedItem is NavItem item)
            OpenTool(item.Tool);
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
