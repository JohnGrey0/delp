using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Delp.App.Tools.WebDev;

/// <summary>
/// Full-virtual-screen, click-through-proof transparent overlay used to
/// sample a pixel color from anywhere on any monitor. This is the one place
/// this tool needs a bespoke <see cref="Window"/> instead of a UserControl,
/// as explicitly allowed for color-blotter.
///
/// Left-click captures the color under the cursor and closes; Esc cancels.
/// Sampling is throttled with a ~30 ms timer rather than raw MouseMove
/// events, per the spec.
/// </summary>
public partial class ColorPickerOverlayWindow : Window
{
    private readonly DispatcherTimer _timer;
    private byte _r, _g, _b;

    public bool Picked { get; private set; }
    public byte PickedR { get; private set; }
    public byte PickedG { get; private set; }
    public byte PickedB { get; private set; }

    public ColorPickerOverlayWindow()
    {
        InitializeComponent();

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        _timer.Tick += (_, _) => SampleUnderCursor();

        Loaded += (_, _) =>
        {
            Activate();
            Focus();
            SampleUnderCursor();
            _timer.Start();
        };
        Closed += (_, _) => _timer.Stop();

        PreviewMouseLeftButtonDown += (_, _) =>
        {
            Picked = true;
            PickedR = _r;
            PickedG = _g;
            PickedB = _b;
            Close();
        };
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape)
                return;
            Picked = false;
            Close();
        };
    }

    private void SampleUnderCursor()
    {
        if (!GetCursorPos(out var pt))
            return;

        var hdc = GetDC(IntPtr.Zero);
        if (hdc == IntPtr.Zero)
            return;

        try
        {
            var colorRef = GetPixel(hdc, pt.X, pt.Y);
            if (colorRef == ClrInvalid)
                return;

            // COLORREF is 0x00bbggrr.
            _r = (byte)(colorRef & 0xFF);
            _g = (byte)((colorRef >> 8) & 0xFF);
            _b = (byte)((colorRef >> 16) & 0xFF);
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, hdc);
        }

        Swatch.Background = new SolidColorBrush(Color.FromRgb(_r, _g, _b));
        HexLabel.Text = $"#{_r:X2}{_g:X2}{_b:X2}";

        // Physical cursor pixels -> this window's DIPs, so the card tracks
        // correctly across monitors with different DPI scaling.
        var dpi = VisualTreeHelper.GetDpi(this);
        var dipX = pt.X / dpi.DpiScaleX - Left;
        var dipY = pt.Y / dpi.DpiScaleY - Top;

        const double cardWidth = 160;
        const double cardHeight = 46;
        var cardX = dipX + 18;
        var cardY = dipY + 18;
        if (cardX + cardWidth > ActualWidth) cardX = dipX - cardWidth - 4;
        if (cardY + cardHeight > ActualHeight) cardY = dipY - cardHeight - 4;

        Canvas.SetLeft(InfoCard, Math.Max(0, cardX));
        Canvas.SetTop(InfoCard, Math.Max(0, cardY));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PointStruct
    {
        public int X;
        public int Y;
    }

    private const uint ClrInvalid = 0xFFFFFFFF;

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out PointStruct point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(IntPtr hdc, int x, int y);
}
