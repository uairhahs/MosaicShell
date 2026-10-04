using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;

namespace MosaicShell.Host.Capabilities
{
    /// <summary>
    /// Full-screen dim behind Tessera flyouts. Hit-testing is off in Avalonia;
    /// Win32 WS_EX_TRANSPARENT is kept so mouse still reaches windows underneath
    /// (Avalonia IsHitTestVisible alone does not make the HWND click-through).
    /// Uses constant LWA_ALPHA (not Transparent composition) so RedirectionSurface
    /// builds stay a subtle wash instead of an opaque black fill via FallbackBrush.
    /// </summary>
    internal sealed class FocusDimWindow : Window
    {
        private int _monitorIndex;

        public FocusDimWindow(int monitorIndexOneBased)
        {
            _monitorIndex = monitorIndexOneBased;
            Title = "MosaicShell - Focus dim";
            WindowDecorations = WindowDecorations.None;
            CanResize = false;
            Topmost = true;
            ShowInTaskbar = false;
            ShowActivated = false;
            Focusable = false;
            IsHitTestVisible = false;
            // Opaque HWND + layered constant alpha (see ApplySubtleDim). Avoid Transparent
            // composition: with RedirectionSurface, FallbackBrush was painting a near-black fill.
            TransparencyLevelHint = [WindowTransparencyLevel.None];
            Background = new SolidColorBrush(Color.FromRgb(
                TesseraFocusDimPolicy.CrustR,
                TesseraFocusDimPolicy.CrustG,
                TesseraFocusDimPolicy.CrustB));
            Opacity = 0;
            Content = null;
            Opened += (_, _) =>
            {
                PlaceOnMonitor(_monitorIndex);
                ApplyDimChrome();
            };
        }

        public void PlaceOnMonitor(int monitorIndexOneBased)
        {
            _monitorIndex = monitorIndexOneBased;
            try
            {
                List<Screen> screens = Screens?.All?.ToList() ?? [];
                Screen? screen = ResolveScreen(screens, _monitorIndex) ?? Screens?.Primary;
                if (screen is null)
                {
                    return;
                }

                PixelRect bounds = screen.Bounds;
                double scale = screen.Scaling > 0.1 ? screen.Scaling : 1.0;
                Position = new PixelPoint(bounds.X, bounds.Y);
                Width = Math.Max(1, bounds.Width / scale);
                Height = Math.Max(1, bounds.Height / scale);
                ApplyDimChrome();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FocusDim] {ex.Message}");
            }
        }

        /// <summary>
        /// Everything that decides how the dim first appears, done before <c>Show</c>: monitor
        /// bounds, layered alpha and click-through. Done only in <c>Opened</c> (after the window was
        /// already on screen), the dim flashed for a frame as an opaque dark box at Avalonia's
        /// default size and place before it was moved, sized and made translucent (reported
        /// 2026-10-04), and it often kept that default size (1920x1011 on a 2560 wide monitor).
        /// The HWND exists from construction, so the Win32 styles can be set now.
        /// </summary>
        public void PrepareBeforeShow()
        {
            PlaceOnMonitor(_monitorIndex);
            if (TesseraFocusDimPolicy.UseConstantLayeredAlpha)
            {
                _ = Win32WindowChrome.ApplySubtleDim(this, TesseraFocusDimPolicy.ResolveLayeredAlpha());
            }

            Win32WindowChrome.ApplyClickThroughNow(this);
        }

        public void FadeIn()
        {
            ApplyDimChrome();
            Opacity = 1;
        }

        private void ApplyDimChrome()
        {
            if (TesseraFocusDimPolicy.UseConstantLayeredAlpha)
            {
                _ = Win32WindowChrome.ApplySubtleDim(this, TesseraFocusDimPolicy.ResolveLayeredAlpha());
            }

            Win32WindowChrome.ApplyClickThrough(this);
        }

        public void InstantClose()
        {
            try
            {
                Opacity = 0;
                Close();
            }
            catch { /* ignore */ }
        }

        private static Screen? ResolveScreen(IReadOnlyList<Screen> screens, int monitorIndexOneBased)
        {
            if (screens.Count == 0)
            {
                return null;
            }

            if (monitorIndexOneBased <= 1)
            {
                return screens.FirstOrDefault(s => s.IsPrimary) ?? screens[0];
            }

            int idx = Math.Clamp(monitorIndexOneBased - 1, 0, screens.Count - 1);
            return screens[idx];
        }
    }

    /// <summary>
    /// Minimal Win32 helpers used only where Avalonia APIs are insufficient
    /// (HWND click-through, HWND Z-order below/above). Prefer Avalonia Topmost /
    /// IsHitTestVisible / transparency hints first.
    /// </summary>
    internal static class Win32WindowChrome
    {
        private const int GwlExStyle = -20;
        private const nint WsExLayered = 0x00080000;
        private const nint WsExTransparent = 0x00000020;
        private const nint WsExNoActivate = 0x08000000;
        private const nint WsExToolWindow = 0x00000080;

        public static readonly IntPtr HwndTopmost = new(-1);
        public static readonly IntPtr HwndNoTopmost = new(-2);
        public static readonly IntPtr HwndBottom = new(1);

        private const uint SwpNoMove = 0x0002;
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoActivate = 0x0010;

        /// <summary>Same styles as <see cref="ApplyClickThrough"/>, applied at once (before Show).</summary>
        public static void ApplyClickThroughNow(Window window)
        {
            try
            {
                nint handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (handle == IntPtr.Zero)
                {
                    return;
                }

                nint current = GetWindowLongPtr(handle, GwlExStyle);
                nint next = current | WsExLayered | WsExTransparent | WsExNoActivate | WsExToolWindow;
                if (next != current)
                {
                    _ = SetWindowLongPtr(handle, GwlExStyle, next);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Win32 click-through] {ex.Message}");
            }
        }

        public static void ApplyClickThrough(Window window)
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    nint handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                    if (handle == IntPtr.Zero)
                    {
                        return;
                    }

                    nint current = GetWindowLongPtr(handle, GwlExStyle);
                    nint next = current | WsExLayered | WsExTransparent | WsExNoActivate | WsExToolWindow;
                    if (next != current)
                    {
                        _ = SetWindowLongPtr(handle, GwlExStyle, next);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Win32 click-through] {ex.Message}");
                }
            }, DispatcherPriority.Loaded);
        }

        public static void SetZOrder(Window window, IntPtr insertAfter)
        {
            try
            {
                nint handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (handle == IntPtr.Zero)
                {
                    return;
                }

                _ = SetWindowPos(handle, insertAfter, 0, 0, 0, 0,
                    SwpNoMove | SwpNoSize | SwpNoActivate);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Win32 z-order] {ex.Message}");
            }
        }

        /// <summary>
        /// Place <paramref name="above"/> as HWND_TOPMOST, then tuck <paramref name="below"/>
        /// immediately under it via hWndInsertAfter. Returns false if either HWND is missing.
        /// </summary>
        public static bool TryStackAbove(Window above, Window? below, out string detail)
        {
            try
            {
                if (!OperatingSystem.IsWindows())
                {
                    detail = "skipped (non-Windows)";
                    return false;
                }

                nint aboveHwnd = above.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (aboveHwnd == IntPtr.Zero)
                {
                    detail = "above HWND=0";
                    return false;
                }

                if (!SetWindowPos(aboveHwnd, HwndTopmost, 0, 0, 0, 0,
                        SwpNoMove | SwpNoSize | SwpNoActivate))
                {
                    detail = $"SetWindowPos(TOPMOST) failed err={Marshal.GetLastWin32Error()}";
                    return false;
                }

                if (below is null)
                {
                    detail = $"flyout={aboveHwnd:X} topmost (no dim)";
                    return true;
                }

                nint belowHwnd = below.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (belowHwnd == IntPtr.Zero)
                {
                    detail = $"flyout={aboveHwnd:X} topmost; dim HWND=0 (not ready)";
                    return false;
                }

                // Place dim BELOW flyout: insertAfter = flyout HWND.
                if (!SetWindowPos(belowHwnd, aboveHwnd, 0, 0, 0, 0,
                        SwpNoMove | SwpNoSize | SwpNoActivate))
                {
                    detail = $"SetWindowPos(dim under flyout) failed err={Marshal.GetLastWin32Error()}";
                    return false;
                }

                detail = $"flyout={aboveHwnd:X} above dim={belowHwnd:X}";
                return true;
            }
            catch (Exception ex)
            {
                detail = ex.Message;
                return false;
            }
        }

        /// <summary>Legacy wrapper, prefer <see cref="TryStackAbove"/>.</summary>
        public static void StackAbove(Window above, Window? below)
        {
            _ = TryStackAbove(above, below, out _);
        }

        /// <summary>
        /// When Avalonia still creates a layered HWND (ActualTransparencyLevel=Transparent despite
        /// hint=None), force constant alpha 255 so opaque brushes are actually visible.
        /// </summary>
        public static string ForceOpaqueLayer(Window window)
        {
            return ApplyLayeredAlpha(window, TesseraFlyoutWindowPolicy.PresentableLayeredAlpha);
        }

        /// <summary>
        /// Constant LWA_ALPHA wash for FocusDim, per <see cref="TesseraFocusDimPolicy.UseConstantLayeredAlpha"/>.
        /// </summary>
        public static string ApplySubtleDim(Window window, byte alpha)
        {
            return ApplyLayeredAlpha(window, alpha);
        }

        private static string ApplyLayeredAlpha(Window window, byte alpha)
        {
            try
            {
                if (!OperatingSystem.IsWindows())
                {
                    return "skipped (non-Windows)";
                }

                nint handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (handle == IntPtr.Zero)
                {
                    return "HWND=0";
                }

                nint ex = GetWindowLongPtr(handle, GwlExStyle);
                nint next = ex | WsExLayered;
                if (next != ex)
                {
                    _ = SetWindowLongPtr(handle, GwlExStyle, next);
                }

                return !SetLayeredWindowAttributes(handle, 0, alpha, LwaAlpha)
                    ? $"SetLayeredWindowAttributes failed err={Marshal.GetLastWin32Error()} hwnd={handle:X}"
                    : $"hwnd={handle:X} LWA_ALPHA={alpha} ex=0x{GetWindowLongPtr(handle, GwlExStyle):X}";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private const uint LwaAlpha = 0x00000002;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        public static void ClearWindowRegion(Window window)
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            try
            {
                nint handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (handle == IntPtr.Zero)
                {
                    return;
                }

                long seq = RequestRegionWrite(window);
                if (!TryBeginRegionWrite(window, seq, handle, "clear", "sync", 0, 0, 0))
                {
                    return;
                }

                _ = SetWindowRgn(handle, IntPtr.Zero, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Win32 region clear] {ex.Message}");
            }
        }

        /// <summary>
        /// Clips the window to nothing: an explicit empty region (not a cleared one, which shows the
        /// whole window, and not a skipped write, which leaves the previous region). Used when a
        /// reveal is fully collapsed, so no sliver of the acrylic backdrop stays on screen.
        /// </summary>
        public static void ApplyEmptyRegion(Window window)
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            try
            {
                nint handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (handle == IntPtr.Zero)
                {
                    return;
                }

                long seq = RequestRegionWrite(window);
                if (!TryBeginRegionWrite(window, seq, handle, "empty", "sync", 0, 0, 0))
                {
                    return;
                }

                nint rgn = CreateRectRgn(0, 0, 0, 0);
                if (rgn == IntPtr.Zero)
                {
                    return;
                }

                // SetWindowRgn owns the handle from here, success or not.
                _ = SetWindowRgn(handle, rgn, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Win32 region empty] {ex.Message}");
            }
        }

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

        // Audit H2: some region writes apply synchronously and some are posted, so an older posted
        // write can arrive after a newer one. One tracker per window numbers every write when it is
        // requested and refuses an older one when it arrives, so a stale region never reaches the
        // screen (it made the Modern media card flicker on a cold show). Always on; logging is Debug.
        private static readonly ConditionalWeakTable<Window, FlyoutRegionWriteTracker> RegionTrackers = [];

        private static long RequestRegionWrite(Window window)
        {
            return RegionTrackers.GetValue(window, static _ => new FlyoutRegionWriteTracker()).Request();
        }

        /// <summary>True when write <paramref name="seq"/> is still the newest and may be applied.</summary>
        private static bool TryBeginRegionWrite(Window window, long seq, nint handle, string op, string mode, int widthPx, int heightPx, int radiusPx)
        {
            FlyoutRegionWriteTracker tracker = RegionTrackers.GetValue(window, static _ => new FlyoutRegionWriteTracker());
            bool apply = tracker.TryApply(seq);
            if (TesseraFlyoutDiagnostics.IsEnabled(DiagnosticLogLevel.Debug))
            {
                TesseraFlyoutDiagnostics.Log(
                    DiagnosticLogLevel.Debug,
                    $"region seq={seq} op={op} mode={mode} hwnd=0x{handle:X} size={widthPx}x{heightPx} r={radiusPx} " +
                    $"{(apply ? "applied" : "dropped-stale")} droppedTotal={tracker.StaleCount}");
            }

            return apply;
        }

        /// <summary>Top-level windows owned by this process, all and visible (A2: orphan windows, audit H3).</summary>
        public static (int Total, int Visible) CountProcessTopLevelWindows()
        {
            if (!OperatingSystem.IsWindows())
            {
                return (0, 0);
            }

            uint pid = (uint)Environment.ProcessId;
            int total = 0;
            int visible = 0;
            _ = EnumWindows(
                (hWnd, lParam) =>
                {
                    _ = GetWindowThreadProcessId(hWnd, out uint owner);
                    if (owner == pid)
                    {
                        total++;
                        if (IsWindowVisible(hWnd))
                        {
                            visible++;
                        }
                    }

                    return true;
                },
                IntPtr.Zero);
            return (total, visible);
        }

        /// <summary>
        /// Debug only: every visible top-level window of this process as
        /// <c>hwnd WxH@x,y layered=0|1 "title"</c>. Added to explain why entrances with the focus dim
        /// on render at 40 to 70 ms a frame only after some point in a run (a leaked or extra visible
        /// window would show here).
        /// </summary>
        public static string DescribeVisibleProcessWindows()
        {
            if (!OperatingSystem.IsWindows())
            {
                return "n/a";
            }

            uint pid = (uint)Environment.ProcessId;
            List<string> rows = [];
            _ = EnumWindows(
                (hWnd, lParam) =>
                {
                    _ = GetWindowThreadProcessId(hWnd, out uint owner);
                    if (owner != pid || !IsWindowVisible(hWnd))
                    {
                        return true;
                    }

                    string size = GetWindowRect(hWnd, out NativeRect r)
                        ? $"{r.Right - r.Left}x{r.Bottom - r.Top}@{r.Left},{r.Top}"
                        : "?";
                    bool layered = (GetWindowLongPtr(hWnd, GwlExStyle) & WsExLayered) != 0;
                    System.Text.StringBuilder title = new(64);
                    _ = GetWindowText(hWnd, title, title.Capacity);
                    rows.Add($"{hWnd:X} {size} layered={(layered ? 1 : 0)} \"{title}\"");
                    return true;
                },
                IntPtr.Zero);
            return $"{rows.Count} [{string.Join("; ", rows)}]";
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int maxCount);

        /// <summary>
        /// The window as the OS has it: frame rectangle, region box and layered alpha. Diagnostic
        /// only (motion frame sampler), so a failure reads as "?" rather than throwing.
        /// </summary>
        internal static string DescribeNativeFrame(Window window)
        {
            if (!OperatingSystem.IsWindows())
            {
                return "native=n/a";
            }

            try
            {
                nint handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (handle == IntPtr.Zero)
                {
                    return "native=nohwnd";
                }

                string rect = GetWindowRect(handle, out NativeRect r)
                    ? $"{r.Left},{r.Top} {r.Right - r.Left}x{r.Bottom - r.Top}"
                    : "?";
                int rgnType = GetWindowRgnBox(handle, out NativeRect box);
                string rgn = rgnType is NullRegion or RegionError
                    ? "none"
                    : $"{box.Left},{box.Top} {box.Right - box.Left}x{box.Bottom - box.Top}";
                string alpha = GetLayeredWindowAttributes(handle, out _, out byte a, out uint flags)
                    && (flags & LwaAlpha) != 0
                    ? a.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : "-";
                return $"native={rect} rgn={rgn} alpha={alpha}";
            }
            catch (Exception ex)
            {
                return $"native=? ({ex.GetType().Name})";
            }
        }

        private const int RegionError = 0;
        private const int NullRegion = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

        [DllImport("user32.dll")]
        private static extern int GetWindowRgnBox(IntPtr hWnd, out NativeRect rect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetLayeredWindowAttributes(IntPtr hWnd, out uint crKey, out byte bAlpha, out uint dwFlags);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// Clip stacked OS acrylic flyouts to pill/card geometry. HWND backdrop is rectangular;
        /// without a region, acrylic leaks outside inner rounded content.
        /// When <paramref name="applySynchronously"/> is true (status CapsLock), do not Post at
        /// Loaded: that races SoftFrost Opacity reveal and flashes a black rectangle.
        /// </summary>
        public static void ApplyRoundRectRegion(
            Window window,
            int widthPx,
            int heightPx,
            int cornerRadiusPx,
            bool applySynchronously = false,
            bool redrawClient = true)
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            long seq = RequestRegionWrite(window);
            string mode = "posted";

            void Apply()
            {
                try
                {
                    nint handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                    if (handle == IntPtr.Zero)
                    {
                        return;
                    }

                    widthPx = Math.Max(widthPx, TesseraFlyoutHwndRegionSpec.MinRenderableRegionPx);
                    heightPx = Math.Max(heightPx, TesseraFlyoutHwndRegionSpec.MinRenderableRegionPx);

                    int radius = Math.Clamp(cornerRadiusPx, 1, Math.Min(widthPx, heightPx) / 2);
                    // Checked before creating the region: SetWindowRgn takes ownership of the
                    // handle, so a dropped write must not create one.
                    if (!TryBeginRegionWrite(window, seq, handle, "apply", mode, widthPx, heightPx, radius))
                    {
                        return;
                    }

                    nint rgn = CreateRoundRectRgn(0, 0, widthPx + 1, heightPx + 1, radius * 2, radius * 2);
                    if (rgn == IntPtr.Zero)
                    {
                        return;
                    }

                    _ = SetWindowRgn(handle, rgn, redrawClient);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Win32 region] {ex.Message}");
                }
            }

            if (applySynchronously && Dispatcher.UIThread.CheckAccess())
            {
                nint handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (handle != IntPtr.Zero)
                {
                    mode = "sync";
                    Apply();
                    return;
                }

                Dispatcher.UIThread.Post(Apply, DispatcherPriority.Loaded);
                return;
            }

            Dispatcher.UIThread.Post(Apply, DispatcherPriority.Loaded);
        }

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int widthEllipse, int heightEllipse);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);
    }
}
