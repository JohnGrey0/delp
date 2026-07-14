using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Delp.App.Infrastructure;
using Delp.App.Windows;
using H.NotifyIcon;

namespace Delp.App;

/// <summary>
/// Tray-first application: a taskbar icon opens the compact flyout
/// (Ctrl+Alt+Space anywhere), which can expand into the full window.
/// </summary>
public partial class App : Application
{
    private TaskbarIcon? _tray;
    private FlyoutWindow? _flyout;
    private MainWindow? _main;
    private HotKey? _hotKey;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _flyout = new FlyoutWindow();
        _flyout.ExpandRequested += (_, toolId) => OpenMain(toolId);

        _tray = new TaskbarIcon
        {
            ToolTipText = "Delp — developer tools (Ctrl+Alt+Space)",
            IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/delp.ico")),
            ContextMenu = BuildTrayMenu(),
        };
        _tray.TrayLeftMouseUp += (_, _) => ToggleFlyout();
        _tray.ForceCreate();

        _hotKey = new HotKey(ModifierKeys.Control | ModifierKeys.Alt, Key.Space, ToggleFlyout);

        OpenMain(null);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotKey?.Dispose();
        _tray?.Dispose();
        base.OnExit(e);
    }

    private ContextMenu BuildTrayMenu()
    {
        var menu = new ContextMenu();

        var open = new MenuItem { Header = "Open Delp" };
        open.Click += (_, _) => OpenMain(null);
        menu.Items.Add(open);

        var quick = new MenuItem { Header = "Quick panel   (Ctrl+Alt+Space)" };
        quick.Click += (_, _) => ToggleFlyout();
        menu.Items.Add(quick);

        menu.Items.Add(new Separator());

        var exit = new MenuItem { Header = "Exit" };
        exit.Click += (_, _) => Shutdown();
        menu.Items.Add(exit);

        return menu;
    }

    private void ToggleFlyout()
    {
        if (_flyout is null)
            return;
        if (_flyout.IsVisible)
            _flyout.HideFlyout();
        else
            _flyout.ShowFlyout();
    }

    private void OpenMain(string? toolId)
    {
        _flyout?.HideFlyout();
        _main ??= new MainWindow();
        if (toolId is not null)
            _main.SelectTool(toolId);
        _main.Show();
        if (_main.WindowState == WindowState.Minimized)
            _main.WindowState = WindowState.Normal;
        _main.Activate();
    }
}
