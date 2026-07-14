using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace Delp.App.Infrastructure;

/// <summary>Global hotkey backed by a hidden message-only window.</summary>
public sealed class HotKey : IDisposable
{
    private const int WmHotKey = 0x0312;
    private const int HotKeyId = 0xD31F;

    private readonly HwndSource _source;
    private readonly Action _callback;

    public bool IsRegistered { get; }

    public HotKey(ModifierKeys modifiers, Key key, Action callback)
    {
        _callback = callback;
        var parameters = new HwndSourceParameters("DelpHotKey")
        {
            WindowStyle = 0,
            Width = 0,
            Height = 0,
            ParentWindow = new IntPtr(-3), // HWND_MESSAGE
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
        // ModifierKeys flag values match the Win32 MOD_* constants exactly.
        IsRegistered = RegisterHotKey(
            _source.Handle, HotKeyId, (uint)modifiers, (uint)KeyInterop.VirtualKeyFromKey(key));
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotKey && wParam.ToInt32() == HotKeyId)
        {
            _callback();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (IsRegistered)
            UnregisterHotKey(_source.Handle, HotKeyId);
        _source.Dispose();
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
}
