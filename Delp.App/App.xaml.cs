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

        if (e.Args.Contains("--smoke"))
        {
            RunViewSmokeTest();
            return;
        }

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

    /// <summary>
    /// Headless QA mode (run with --smoke): constructs and lays out every
    /// registered tool view, then writes smoke-report.txt next to the exe.
    /// Exit code = number of failing views.
    /// </summary>
    private void RunViewSmokeTest()
    {
        var report = new System.Text.StringBuilder();
        var failures = 0;
        var total = System.Diagnostics.Stopwatch.StartNew();

        foreach (var info in ToolCatalog.All)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var view = ToolCatalog.CreateView(info);
                view.Measure(new Size(800, 600));
                view.Arrange(new Rect(0, 0, 800, 600));
                report.AppendLine($"OK   {sw.ElapsedMilliseconds,5} ms  {info.Id}");
            }
            catch (Exception ex)
            {
                failures++;
                var root = ex.GetBaseException();
                report.AppendLine($"FAIL {sw.ElapsedMilliseconds,5} ms  {info.Id}: {root.GetType().Name}: {root.Message}");
            }
        }

        report.Insert(0,
            $"Delp view smoke test — {ToolCatalog.All.Count} tools, {failures} failures, {total.ElapsedMilliseconds} ms total{Environment.NewLine}");
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, "smoke-report.txt");
        System.IO.File.WriteAllText(path, report.ToString());
        Shutdown(failures);
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
