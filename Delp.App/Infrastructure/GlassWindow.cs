using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace Delp.App.Infrastructure;

/// <summary>
/// Borderless window with the Windows 11 acrylic ("glass") backdrop, dark chrome
/// and rounded corners — the same materials the OS uses for Quick Settings.
/// Falls back to a solid dark background where the backdrop API is unavailable.
/// </summary>
public class GlassWindow : Window
{
    public GlassWindow()
    {
        WindowStyle = WindowStyle.None;
        Background = Brushes.Transparent;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        SetResourceReference(ForegroundProperty, "Brush.Fg0");
        SetResourceReference(FontFamilyProperty, "Font.Ui");
        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            GlassFrameThickness = new Thickness(-1),
            CaptionHeight = 0,
            CornerRadius = new CornerRadius(0),
            ResizeBorderThickness = new Thickness(6),
            UseAeroCaptionButtons = false,
        });
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(hwnd);
        if (source?.CompositionTarget is { } target)
            target.BackgroundColor = Colors.Transparent;
        source?.AddHook(WndProc);

        int on = 1;
        _ = DwmSetWindowAttribute(hwnd, DwmaUseImmersiveDarkMode, ref on, sizeof(int));
        int corner = DwmwcpRound;
        _ = DwmSetWindowAttribute(hwnd, DwmaWindowCornerPreference, ref corner, sizeof(int));
        int backdrop = DwmsbtTransientWindow; // acrylic
        if (DwmSetWindowAttribute(hwnd, DwmaSystemBackdropType, ref backdrop, sizeof(int)) != 0)
            SetResourceReference(BackgroundProperty, "Brush.WindowFallback");
    }

    // Constrain maximize to the monitor work area (WindowStyle=None otherwise covers the taskbar).
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmGetMinMaxInfo)
        {
            ApplyWorkAreaBounds(hwnd, lParam);
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void ApplyWorkAreaBounds(IntPtr hwnd, IntPtr lParam)
    {
        var info = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor != IntPtr.Zero)
        {
            var monitorInfo = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfo(monitor, ref monitorInfo))
            {
                var work = monitorInfo.rcWork;
                var area = monitorInfo.rcMonitor;
                info.ptMaxPosition.X = work.Left - area.Left;
                info.ptMaxPosition.Y = work.Top - area.Top;
                info.ptMaxSize.X = work.Right - work.Left;
                info.ptMaxSize.Y = work.Bottom - work.Top;
            }
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        if (!double.IsNaN(MinWidth) && MinWidth > 0)
            info.ptMinTrackSize.X = (int)(MinWidth * dpi.DpiScaleX);
        if (!double.IsNaN(MinHeight) && MinHeight > 0)
            info.ptMinTrackSize.Y = (int)(MinHeight * dpi.DpiScaleY);

        Marshal.StructureToPtr(info, lParam, true);
    }

    private const int WmGetMinMaxInfo = 0x0024;
    private const int MonitorDefaultToNearest = 2;
    private const int DwmaUseImmersiveDarkMode = 20;
    private const int DwmaWindowCornerPreference = 33;
    private const int DwmaSystemBackdropType = 38;
    private const int DwmwcpRound = 2;
    private const int DwmsbtTransientWindow = 3;

    [StructLayout(LayoutKind.Sequential)]
    private struct PointStruct { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RectStruct { public int Left; public int Top; public int Right; public int Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public PointStruct ptReserved;
        public PointStruct ptMaxSize;
        public PointStruct ptMaxPosition;
        public PointStruct ptMinTrackSize;
        public PointStruct ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int cbSize;
        public RectStruct rcMonitor;
        public RectStruct rcWork;
        public int dwFlags;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
}
