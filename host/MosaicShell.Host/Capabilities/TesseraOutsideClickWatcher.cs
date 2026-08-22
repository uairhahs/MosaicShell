using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace MosaicShell.Host.Capabilities;

/// <summary>
/// WH_MOUSE_LL outside-click dismiss. The hook callback must NEVER touch Avalonia —
/// reading Bounds/Screens from the hook thread deadlocks the UI (and freezes the app).
/// Bounds are snapshotted on the UI thread when the watcher starts / is refreshed.
/// </summary>
internal sealed class TesseraOutsideClickWatcher : IDisposable
{
    private delegate nint LowLevelMouseProc(int nCode, nint wParam, nint lParam);

#pragma warning disable CS0649
    private struct Point
    {
        public int X;
        public int Y;
    }

    private struct MsllHookStruct
    {
        public Point Pt;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nint DwExtraInfo;
    }
#pragma warning restore CS0649

    private readonly Action _dismiss;
    private nint _hook;
    private LowLevelMouseProc? _proc;
    private int _left, _top, _right, _bottom;
    private bool _hasBounds;
    private int _dismissPosted;

    private const int WhMouseLl = 14;
    private const int WmLButtonDown = 0x0201;
    private const int WmRButtonDown = 0x0204;
    private const int WmMButtonDown = 0x0207;
    private const int WmNcLButtonDown = 0x00A1;

    public bool IsActive => _hook != IntPtr.Zero;

    public TesseraOutsideClickWatcher(FlyoutWindow flyout, Action dismiss)
    {
        _dismiss = dismiss;
        CaptureBounds(flyout);
    }

    public void RefreshBounds(FlyoutWindow flyout) => CaptureBounds(flyout);

    public void Start()
    {
        if (IsActive) return;
        _proc = HookCallback;
        // hMod must be null for WH_MOUSE_LL on modern Windows when the proc lives in the process.
        _hook = SetWindowsHookEx(WhMouseLl, _proc, IntPtr.Zero, 0);
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

    private void CaptureBounds(FlyoutWindow flyout)
    {
        try
        {
            if (!flyout.IsVisible)
            {
                _hasBounds = false;
                return;
            }

            var position = flyout.Position;
            var bounds = flyout.Bounds;
            if (bounds.Width < 2 || bounds.Height < 2)
            {
                _hasBounds = false;
                return;
            }

            var screens = flyout.Screens?.All?.ToList() ?? [];
            var screen = screens.FirstOrDefault(s =>
            {
                var b = s.Bounds;
                return position.X >= b.X && position.X < b.X + b.Width
                       && position.Y >= b.Y && position.Y < b.Y + b.Height;
            }) ?? flyout.Screens?.Primary;
            var scale = screen?.Scaling > 0.1 ? screen.Scaling : 1.0;
            var w = (int)Math.Ceiling(bounds.Width * scale);
            var h = (int)Math.Ceiling(bounds.Height * scale);
            _left = position.X;
            _top = position.Y;
            _right = position.X + w;
            _bottom = position.Y + h;
            _hasBounds = w > 0 && h > 0;
        }
        catch
        {
            _hasBounds = false;
        }
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
                    if (_hasBounds && !HitTest(info.Pt.X, info.Pt.Y))
                        PostDismissOnce();
                }
                catch
                {
                    // never throw out of a low-level hook
                }
            }
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private bool HitTest(int screenX, int screenY) =>
        screenX >= _left && screenX < _right && screenY >= _top && screenY < _bottom;

    private void PostDismissOnce()
    {
        // Hook may fire many downs; only queue dismiss once.
        if (Interlocked.CompareExchange(ref _dismissPosted, 1, 0) != 0)
            return;
        // Background — never Send/Invoke from the hook thread.
        Dispatcher.UIThread.Post(() =>
        {
            try { _dismiss(); }
            catch { /* ignore */ }
        }, DispatcherPriority.Background);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);
}
