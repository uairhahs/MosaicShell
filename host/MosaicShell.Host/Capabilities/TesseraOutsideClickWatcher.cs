using System.Runtime.InteropServices;
using Avalonia.Threading;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Capabilities
{
    /// <summary>
    /// WH_MOUSE_LL outside-click dismiss. The hook callback must NEVER touch Avalonia;
    /// reading Bounds/Screens from the hook thread deadlocks the UI (and freezes the app).
    /// The flyouts' HWNDs are taken on the UI thread when the watcher starts / is refreshed,
    /// and the hook reads their rects through user32 at each press.
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

        private readonly Action<Avalonia.PixelPoint> _dismiss;
        private nint _hook;
        private LowLevelMouseProc? _proc;
        private volatile nint[] _handles = [];
        private int _dismissPosted;

        private const int WhMouseLl = 14;
        private const int WmLButtonDown = 0x0201;
        private const int WmRButtonDown = 0x0204;
        private const int WmMButtonDown = 0x0207;
        private const int WmNcLButtonDown = 0x00A1;

        public TesseraOutsideClickWatcher(FlyoutWindow flyout, Action<Avalonia.PixelPoint> dismiss)
            : this([flyout], dismiss)
        {
        }

        public TesseraOutsideClickWatcher(IReadOnlyList<FlyoutWindow> flyouts, Action<Avalonia.PixelPoint> dismiss)
        {
            _dismiss = dismiss;
            CaptureBounds(flyouts);
        }

        public void RefreshBounds(FlyoutWindow flyout)
        {
            CaptureBounds([flyout]);
        }

        public void RefreshBounds(IReadOnlyList<FlyoutWindow> flyouts)
        {
            CaptureBounds(flyouts);
        }

        public bool IsActive => _hook != IntPtr.Zero;

        public void Start()
        {
            if (IsActive)
            {
                return;
            }

            _proc = HookCallback;
            // hMod must be null for WH_MOUSE_LL on modern Windows when the proc lives in the process.
            _hook = SetWindowsHookEx(WhMouseLl, _proc, IntPtr.Zero, 0);
        }

        public void Dispose()
        {
            if (_hook != IntPtr.Zero)
            {
                _ = UnhookWindowsHookEx(_hook);
                _hook = IntPtr.Zero;
            }
            _proc = null;
        }

        /// <summary>
        /// Takes the flyouts' HWNDs on the UI thread. Their rects are read at each press (<see cref="LiveRects"/>), so
        /// the watcher can arm during the entrance (<see cref="TesseraFlyoutOutsideClickPolicy.ArmDelayMs"/>) and
        /// follows a slot that moves or resizes.
        /// </summary>
        private void CaptureBounds(IReadOnlyList<FlyoutWindow> flyouts)
        {
            List<nint> handles = [];
            foreach (FlyoutWindow flyout in flyouts)
            {
                try
                {
                    nint handle = flyout.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                    if (handle != IntPtr.Zero)
                    {
                        handles.Add(handle);
                    }
                }
                catch
                {
                    // a window closing under us has no handle to watch
                }
            }

            _handles = [.. handles];
        }

        /// <summary>The visible flyout HWNDs' rects now; user32 only, so safe on the hook thread.</summary>
        private List<(int Left, int Top, int Right, int Bottom)> LiveRects()
        {
            List<(int Left, int Top, int Right, int Bottom)> rects = [];
            foreach (nint handle in _handles)
            {
                if (IsWindowVisible(handle) && GetWindowRect(handle, out Rect r) && r.Right - r.Left > 1 && r.Bottom - r.Top > 1)
                {
                    rects.Add((r.Left, r.Top, r.Right, r.Bottom));
                }
            }

            return rects;
        }

        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(nint hWnd, out Rect rect);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(nint hWnd);

        private nint HookCallback(int nCode, nint wParam, nint lParam)
        {
            if (nCode >= 0)
            {
                int msg = (int)wParam;
                if (msg is WmNcLButtonDown or WmLButtonDown or WmRButtonDown or WmMButtonDown)
                {
                    try
                    {
                        MsllHookStruct info = Marshal.PtrToStructure<MsllHookStruct>(lParam);
                        if (TesseraFlyoutOutsideClickPolicy.IsOutside(info.Pt.X, info.Pt.Y, LiveRects()))
                        {
                            PostDismissOnce(new Avalonia.PixelPoint(info.Pt.X, info.Pt.Y));
                        }
                    }
                    catch
                    {
                        // never throw out of a low-level hook
                    }
                }
            }

            return CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        private void PostDismissOnce(Avalonia.PixelPoint at)
        {
            // Hook may fire many downs; only queue dismiss once.
            if (Interlocked.CompareExchange(ref _dismissPosted, 1, 0) != 0)
            {
                return;
            }
            // Background, never Send/Invoke from the hook thread.
            Dispatcher.UIThread.Post(() =>
            {
                try { _dismiss(at); }
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
}
