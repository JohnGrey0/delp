using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Delp.App.Infrastructure;

namespace Delp.App.Windows;

/// <summary>
/// The compact quick panel that opens from the tray icon or global hotkey,
/// in the spirit of a macOS menu-bar app. Hosts any tool inline and can
/// "roll open" into the full main window.
/// </summary>
public partial class FlyoutWindow : GlassWindow
{
    private const string StarOutline = "";
    private const string StarFilled = "";

    private readonly Dictionary<string, UserControl> _views = new();
    private ToolInfo? _current;

    /// <summary>Raised when the user asks to expand into the full app; argument is the active tool id (or null).</summary>
    public event EventHandler<string?>? ExpandRequested;

    public FlyoutWindow()
    {
        InitializeComponent();
        SettingsService.FavoritesChanged += OnFavoritesChanged;
        RefreshList(null);
    }

    public void ShowFlyout()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 12;
        Top = area.Bottom - Height - 12;
        Show();
        Activate();
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    public void HideFlyout() => Hide();

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        Hide();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // The flyout lives for the whole app session; closing just hides it.
        e.Cancel = true;
        Hide();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key != Key.Escape)
            return;
        if (ToolPane.Visibility == Visibility.Visible)
            ShowList();
        else
            Hide();
        e.Handled = true;
    }

    private void RefreshList(string? query)
    {
        var searching = !string.IsNullOrWhiteSpace(query);
        var items = searching
            ? ToolCatalog.Search(query).Select(t => new NavItem(t, t.CategoryName)).ToList()
            : NavItem.BuildGrouped(ToolCatalog.All);

        var view = new ListCollectionView(items);
        if (!searching)
            view.GroupDescriptions!.Add(new PropertyGroupDescription(nameof(NavItem.Group)));
        ResultsList.ItemsSource = view;
    }

    private void ShowList()
    {
        _current = null;
        ToolPane.Visibility = Visibility.Collapsed;
        ResultsList.Visibility = Visibility.Visible;
        SearchBox.Visibility = Visibility.Visible;
        BackBtn.Visibility = Visibility.Collapsed;
        StarBtn.Visibility = Visibility.Collapsed;
        HeaderText.Text = "Delp";
        ResultsList.SelectedItem = null;
        SearchBox.Focus();
    }

    private void OpenTool(ToolInfo info)
    {
        if (!_views.TryGetValue(info.Id, out var view))
        {
            view = ToolCatalog.CreateView(info);
            _views[info.Id] = view;
        }

        _current = info;
        ToolHost.Content = view;
        HeaderText.Text = info.Name;
        BackBtn.Visibility = Visibility.Visible;
        StarBtn.Visibility = Visibility.Visible;
        ResultsList.Visibility = Visibility.Collapsed;
        SearchBox.Visibility = Visibility.Collapsed;
        ToolPane.Visibility = Visibility.Visible;
        UpdateStar();
    }

    private void UpdateStar()
    {
        if (_current is null)
            return;
        var favorite = SettingsService.IsFavorite(_current.Id);
        StarBtn.Content = favorite ? StarFilled : StarOutline;
        StarBtn.ToolTip = favorite ? "Remove from favorites" : "Add to favorites";
    }

    private void Star_Click(object sender, RoutedEventArgs e)
    {
        if (_current is not null)
            SettingsService.ToggleFavorite(_current.Id);
    }

    private void OnFavoritesChanged()
    {
        if (ResultsList.Visibility == Visibility.Visible)
            RefreshList(SearchBox.Text);
        UpdateStar();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) =>
        RefreshList(SearchBox.Text);

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter when ResultsList.Items.Count > 0:
                OpenTool(((NavItem)ResultsList.Items[0]!).Tool);
                e.Handled = true;
                break;
            case Key.Down when ResultsList.Items.Count > 0:
                ResultsList.Focus();
                ResultsList.SelectedIndex = 0;
                e.Handled = true;
                break;
        }
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is NavItem item)
            OpenTool(item.Tool);
    }

    private void Back_Click(object sender, RoutedEventArgs e) => ShowList();

    private void Expand_Click(object sender, RoutedEventArgs e) =>
        ExpandRequested?.Invoke(this, _current?.Id);
}
