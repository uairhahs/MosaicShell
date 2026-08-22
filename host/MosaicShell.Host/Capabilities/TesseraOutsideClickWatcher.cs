using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace MosaicShell.Host.Capabilities;

internal sealed class TesseraOutsideClickWatcher : IDisposable
{
    private delegate nint LowLevelMouseProc(int nCode, nint wParam, nint lParam);

    // Fields are written by Win32 via SetWindowsHookEx / Marshal.PtrToStructure.
#pragma warning disable CS0649
    private struct Point
    {
        public int x;
        public int y;
    }

    private struct MsllHookStruct
    {
        public Point pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public nint dwExtraInfo;
    }
#pragma warning restore CS0649

    private readonly FlyoutWindow _flyout;
    private readonly Action _dismiss;
    private nint _hook;
    private LowLevelMouseProc? _proc;

    private const int WhMouseLl = 14;
    private const int WmLButtonDown = 0x0201;
    private const int WmRButtonDown = 0x0204;
    private const int WmMButtonDown = 0x0207;
    private const int WmNcLButtonDown = 0x00A1;

    public bool IsActive => _hook != IntPtr.Zero;

    public TesseraOutsideClickWatcher(FlyoutWindow flyout, Action dismiss)
    {
        _flyout = flyout;
        _dismiss = dismiss;
    }

    public void Start()
    {
        if (IsActive) return;
        _proc = HookCallback;
        _hook = SetWindowsHookEx(WhMouseLl, _proc, GetModuleHandle(null), 0);
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
        _proc = null;
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            var msg = (int)wParam;
            if (msg is WmNcLButtonDown or WmLButtonDown or WmRButtonDown or WmMButtonDown)
            {
                try
                {
                    var info = Marshal.PtrToStructure<MsllHookStruct>(lParam);
                    if (!PointHitsFlyout(info.pt.x, info.pt.y))
                        Dispatcher.UIThread.Post(_dismiss, DispatcherPriority.Send);
                }
                catch { /* ignore */ }
            }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private bool PointHitsFlyout(int screenX, int screenY)
    {
        try
        {
            if (!_flyout.IsVisible) return false;

            var position = _flyout.Position;
            var bounds = _flyout.Bounds;
            if (bounds.Width < 2 || bounds.Height < 2) return false;

            var screens = _flyout.Screens?.All?.ToList() ?? [];
            var screen = screens.FirstOrDefault(s =>
            {
                var b = s.Bounds;
                return screenX >= b.X && screenX < b.X + b.Width
                       && screenY >= b.Y && screenY < b.Y + b.Height;
            }) ?? _flyout.Screens?.Primary;
            var scale = screen?.Scaling > 0.1 ? screen.Scaling : 1.0;
            var w = (int)Math.Ceiling(bounds.Width * scale);
            var h = (int)Math.Ceiling(bounds.Height * scale);
            return screenX >= position.X && screenX < position.X + w
                   && screenY >= position.Y && screenY < position.Y + h;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? lpModuleName);
}
