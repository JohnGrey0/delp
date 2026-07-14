using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Delp.App.Infrastructure;
using Delp.Core.Tools.WebDev;

namespace Delp.App.Tools.WebDev;

[Tool("svg-path", "SVG Path Visualizer", ToolCategory.WebDev,
    "Visualize and validate an SVG path's \"d\" attribute.",
    Keywords = "svg,path,d,vector,bezier,visualize", Order = 80)]
public partial class SvgPathView : UserControl
{
    // Material Design "favorite" heart glyph (24x24 viewBox) — a friendly default to visualize.
    private const string SamplePath =
        "M12,21.35l-1.45-1.32C5.4,15.36,2,12.28,2,8.5 2,5.42,4.42,3,7.5,3c1.74,0,3.41,0.81,4.5,2.09 " +
        "C13.09,3.81,14.76,3,16.5,3 19.58,3,22,5.42,22,8.5c0,3.78-3.4,6.86-8.55,11.54L12,21.35z";

    private readonly DispatcherTimer _debounce;

    public SvgPathView()
    {
        InitializeComponent();

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Render();
        };

        PathBox.Text = SamplePath;

        Unloaded += (_, _) => _debounce.Stop();
    }

    // Debounced: a malformed intermediate path (typical while typing) throws inside Analyze/
    // Geometry.Parse, and paying for exception unwinding on every keystroke is wasteful.
    private void PathBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Render();
    }

    private void Render()
    {
        var d = PathBox.Text ?? "";

        if (string.IsNullOrWhiteSpace(d))
        {
            PreviewPath.Data = null;
            StatusText.Text = "0 commands";
            ErrorText.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            // Core validates the command/number grammar so parse errors name the offending
            // token; WPF's Geometry.Parse (same grammar) then supplies renderable geometry + bounds.
            var info = SvgPathTool.Analyze(d);
            var geometry = Geometry.Parse(d);
            PreviewPath.Data = geometry;

            var bounds = geometry.Bounds;
            StatusText.Text = bounds.IsEmpty
                ? $"{info.CommandCount} command{(info.CommandCount == 1 ? "" : "s")} · empty bounds"
                : $"{info.CommandCount} command{(info.CommandCount == 1 ? "" : "s")} · " +
                  $"bounds {bounds.Width:F1} × {bounds.Height:F1} at ({bounds.X:F1}, {bounds.Y:F1})";
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            PreviewPath.Data = null;
            StatusText.Text = "";
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
