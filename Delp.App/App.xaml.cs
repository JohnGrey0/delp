using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        var shotIndex = Array.IndexOf(e.Args, "--shot");
        if (shotIndex >= 0)
        {
            RunWindowShot(e.Args.Length > shotIndex + 1 ? e.Args[shotIndex + 1] : null);
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

        var about = new MenuItem { Header = "About Delp" };
        about.Click += (_, _) => AboutWindow.Open(_main is { IsVisible: true } ? _main : null);
        menu.Items.Add(about);

        var startup = new MenuItem();
        void RefreshStartupHeader() =>
            startup.Header = (StartupManager.IsEnabled() ? "✓  " : "") + "Start with Windows";
        RefreshStartupHeader();
        startup.Click += (_, _) =>
        {
            StartupManager.SetEnabled(!StartupManager.IsEnabled());
            RefreshStartupHeader();
        };
        menu.Items.Add(startup);

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

                var lint = new List<string>();
                LintVisualTree(view, lint);

                view.Measure(new Size(400, 5000));
                if (view.DesiredSize.Width > 402)
                    lint.Add($"overflows flyout width (needs {view.DesiredSize.Width:0} px at 400 px)");

                if (lint.Count > 0)
                {
                    failures++;
                    report.AppendLine($"LINT {sw.ElapsedMilliseconds,5} ms  {info.Id}: {string.Join(" | ", lint)}");
                }
                else
                {
                    report.AppendLine($"OK   {sw.ElapsedMilliseconds,5} ms  {info.Id}");
                }
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

    /// <summary>
    /// Headless QA mode (run with --shot [toolId]): opens the main window
    /// (optionally with a tool selected), renders the app's own visual tree
    /// to window-shot.png next to the exe, and exits. Renders only Delp's
    /// UI — never the screen.
    /// </summary>
    private void RunWindowShot(string? toolId)
    {
        Window window;
        if (toolId == "about")
        {
            window = new AboutWindow { WindowStartupLocation = WindowStartupLocation.CenterScreen };
        }
        else
        {
            var main = new MainWindow();
            if (toolId is not null)
                main.SelectTool(toolId);
            window = main;
        }
        window.Show();

        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, () =>
        {
            var width = (int)window.ActualWidth;
            var height = (int)window.ActualHeight;
            var root = (Visual)window.Content;

            var visual = new System.Windows.Media.DrawingVisual();
            using (var ctx = visual.RenderOpen())
            {
                ctx.DrawRectangle(
                    new SolidColorBrush(Color.FromRgb(0x1E, 0x21, 0x26)), null,
                    new Rect(0, 0, width, height));
                ctx.DrawRectangle(
                    new System.Windows.Media.VisualBrush(root), null,
                    new Rect(0, 0, width, height));
            }

            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            bitmap.Render(visual);

            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "window-shot.png");
            using (var stream = System.IO.File.Create(path))
                encoder.Save(stream);

            Shutdown(0);
        });
    }

    /// <summary>
    /// Automated UX lint over a laid-out view: catches the recurring WPF traps
    /// this codebase has actually hit — ListBoxes nested inside an outer
    /// ScrollViewer (mouse wheel dies, virtualization breaks) and fixed-height
    /// TextBoxes too short for their content (glyphs clip).
    /// </summary>
    private static void LintVisualTree(DependencyObject root, List<string> lint)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);

            if (child is System.Windows.Controls.ListBox listBox)
            {
                for (var a = VisualTreeHelper.GetParent(listBox); a is not null; a = VisualTreeHelper.GetParent(a))
                {
                    if (a is System.Windows.Controls.ScrollViewer)
                    {
                        lint.Add($"ListBox '{listBox.Name}' is wrapped in an outer ScrollViewer (wheel-trap)");
                        break;
                    }
                }
            }

            if (child is System.Windows.Controls.TextBox box
                && !double.IsNaN(box.Height)
                && box.DesiredSize.Height > box.Height + 1)
            {
                lint.Add($"TextBox '{box.Name}' fixed Height={box.Height:0} clips content (needs {box.DesiredSize.Height:0})");
            }

            LintVisualTree(child, lint);
        }
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
