using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace MosaicShell.Host.Capabilities;

/// <summary>
/// WH_MOUSE_LL outside-click dismiss. The hook callback must NEVER touch Avalonia;
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
    private readonly List<(int Left, int Top, int Right, int Bottom)> _rects = [];
    private bool _hasBounds;
    private int _dismissPosted;

    private const int WhMouseLl = 14;
    private const int WmLButtonDown = 0x0201;
    private const int WmRButtonDown = 0x0204;
    private const int WmMButtonDown = 0x0207;
    private const int WmNcLButtonDown = 0x00A1;

    public TesseraOutsideClickWatcher(FlyoutWindow flyout, Action dismiss)
        : this([flyout], dismiss)
    {
    }

    public TesseraOutsideClickWatcher(IReadOnlyList<FlyoutWindow> flyouts, Action dismiss)
    {
        _dismiss = dismiss;
        CaptureBounds(flyouts);
    }

    public void RefreshBounds(FlyoutWindow flyout) => CaptureBounds([flyout]);

    public void RefreshBounds(IReadOnlyList<FlyoutWindow> flyouts) => CaptureBounds(flyouts);

    public bool IsActive => _hook != IntPtr.Zero;

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

    private void CaptureBounds(IReadOnlyList<FlyoutWindow> flyouts)
    {
        _rects.Clear();
        try
        {
            foreach (var flyout in flyouts)
            {
                if (TryCaptureRect(flyout, out var rect))
                    _rects.Add(rect);
            }

            _hasBounds = _rects.Count > 0;
        }
        catch
        {
            _hasBounds = false;
        }
    }

    private static bool TryCaptureRect(FlyoutWindow flyout, out (int Left, int Top, int Right, int Bottom) rect)
    {
        rect = default;
        try
        {
            if (!flyout.IsVisible)
                return false;

            var position = flyout.Position;
            var bounds = flyout.Bounds;
            if (bounds.Width < 2 || bounds.Height < 2)
                return false;

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
            if (w <= 0 || h <= 0)
                return false;

            rect = (position.X, position.Y, position.X + w, position.Y + h);
            return true;
        }
        catch
        {
            return false;
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

    private bool HitTest(int screenX, int screenY)
    {
        foreach (var (left, top, right, bottom) in _rects)
        {
            if (screenX >= left && screenX < right && screenY >= top && screenY < bottom)
                return true;
        }

        return false;
    }

    private void PostDismissOnce()
    {
        // Hook may fire many downs; only queue dismiss once.
        if (Interlocked.CompareExchange(ref _dismissPosted, 1, 0) != 0)
            return;
        // Background, never Send/Invoke from the hook thread.
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
