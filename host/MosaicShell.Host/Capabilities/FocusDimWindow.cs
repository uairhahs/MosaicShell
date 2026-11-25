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
    /// Full-screen layer behind Tessera flyouts: the focus dim, and the outside-click hitbox on every platform (a press on
    /// it dismisses the flyout and is swallowed, popup-menu style). On Windows it uses constant LWA_ALPHA (not Transparent
    /// composition) so RedirectionSurface builds stay a subtle wash instead of an opaque black fill via FallbackBrush;
    /// with the dim off it is one step above invisible, so it still takes the click.
    /// </summary>
    internal sealed class FocusDimWindow : Window
    {
        private int _monitorIndex;
        private readonly FocusDimRoute _route;
        private readonly bool _dimEnabled;
        private readonly bool _onFlyoutScreen;
        private readonly double _fixedScaling = 1.0;

        /// <summary>A press on the layer where it is the outside-click hitbox, at its screen pixel.</summary>
        public event EventHandler<PixelPoint>? OutsideClicked;

        /// <summary>
        /// A bare outside-click hitbox over a screen other than the flyout's (see
        /// <see cref="TesseraFocusDimPolicy.LayerCoversEveryScreen"/>). It is never dimmed and stays on that screen.
        /// </summary>
        public FocusDimWindow(Screen otherScreen, FocusDimRoute route)
            : this(monitorIndexOneBased: 0, route, dimEnabled: false, onFlyoutScreen: false)
        {
            FixedBounds = otherScreen.Bounds;
            _fixedScaling = otherScreen.Scaling > 0.1 ? otherScreen.Scaling : 1.0;
        }

        public FocusDimWindow(int monitorIndexOneBased, FocusDimRoute route, bool dimEnabled = true)
            : this(monitorIndexOneBased, route, dimEnabled, onFlyoutScreen: true)
        {
        }

        private FocusDimWindow(int monitorIndexOneBased, FocusDimRoute route, bool dimEnabled, bool onFlyoutScreen)
        {
            _monitorIndex = monitorIndexOneBased;
            _route = route;
            _dimEnabled = dimEnabled;
            _onFlyoutScreen = onFlyoutScreen;
            Title = "MosaicShell - Focus dim";
            WindowDecorations = WindowDecorations.None;
            CanResize = false;
            Topmost = true;
            ShowInTaskbar = false;
            X11WindowState.ReassertOnShow(this);
            ShowActivated = false;
            Focusable = false;
            IsHitTestVisible = TesseraFocusDimPolicy.LayerIsOutsideClickHitbox(route);
            // Windows: opaque HWND + layered constant alpha (see ApplySubtleDim). Avoid Transparent
            // composition there: with RedirectionSurface, FallbackBrush was painting a near-black fill.
            // X11: a Transparent (ARGB) window whose background brush carries the dim alpha.
            TransparencyLevelHint = TesseraFocusDimPolicy.PaintsAlphaInWindow(route)
                ? [WindowTransparencyLevel.Transparent]
                : [WindowTransparencyLevel.None];
            Background = new SolidColorBrush(Color.FromArgb(
                TesseraFocusDimPolicy.ResolveLayerAlpha(route, dimEnabled, onFlyoutScreen),
                TesseraFocusDimPolicy.CrustR,
                TesseraFocusDimPolicy.CrustG,
                TesseraFocusDimPolicy.CrustB));
            Opacity = 0;
            Content = null;
            PointerPressed += (_, e) =>
            {
                e.Handled = true;
                OutsideClicked?.Invoke(this, this.PointToScreen(e.GetPosition(this)));
            };
            Opened += (_, _) =>
            {
                PlaceOnMonitor(_monitorIndex);
                ApplyDimChrome();
            };
        }

        /// <summary>The screen a bare hitbox covers; null for the layer that follows the flyout's monitor.</summary>
        public PixelRect? FixedBounds { get; }

        /// <summary>The screens other than the one this layer's monitor index resolves to.</summary>
        public List<Screen> OtherScreens()
        {
            List<Screen> screens = Screens?.All?.ToList() ?? [];
            Screen? target = ResolveScreen(screens, _monitorIndex) ?? Screens?.Primary;
            return target is null ? [] : [.. screens.Where(s => s.Bounds != target.Bounds)];
        }

        public void PlaceOnMonitor(int monitorIndexOneBased)
        {
            _monitorIndex = monitorIndexOneBased;
            try
            {
                if (FixedBounds is PixelRect fixedBounds)
                {
                    Position = new PixelPoint(fixedBounds.X, fixedBounds.Y);
                    Width = Math.Max(1, fixedBounds.Width / _fixedScaling);
                    Height = Math.Max(1, fixedBounds.Height / _fixedScaling);
                    ApplyDimChrome();
                    return;
                }

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
            if (_route == FocusDimRoute.X11ArgbInputShape)
            {
                // The XID exists from construction too, so the dim is click-through from its first frame.
                _ = X11FocusDim.Apply(this, clickThrough: !TesseraFocusDimPolicy.LayerIsOutsideClickHitbox(_route));
                return;
            }

            if (TesseraFocusDimPolicy.UseConstantLayeredAlpha)
            {
                _ = Win32WindowChrome.ApplySubtleDim(this, TesseraFocusDimPolicy.ResolveLayeredAlpha(_dimEnabled, _onFlyoutScreen));
            }

            Win32WindowChrome.ApplyOverlayStylesNow(this, clickThrough: !TesseraFocusDimPolicy.LayerIsOutsideClickHitbox(_route));
        }

        public void FadeIn()
        {
            ApplyDimChrome();
            Opacity = 1;
        }

        private void ApplyDimChrome()
        {
            if (_route == FocusDimRoute.X11ArgbInputShape)
            {
                _ = X11FocusDim.Apply(this, clickThrough: !TesseraFocusDimPolicy.LayerIsOutsideClickHitbox(_route));
                return;
            }

            if (TesseraFocusDimPolicy.UseConstantLayeredAlpha)
            {
                _ = Win32WindowChrome.ApplySubtleDim(this, TesseraFocusDimPolicy.ResolveLayeredAlpha(_dimEnabled, _onFlyoutScreen));
            }

            Win32WindowChrome.ApplyOverlayStyles(this, clickThrough: !TesseraFocusDimPolicy.LayerIsOutsideClickHitbox(_route));
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

        /// <summary>Overlay styles plus click-through, applied at once (before Show); for the glint window.</summary>
        public static void ApplyClickThroughNow(Window window)
        {
            ApplyOverlayStylesNow(window, clickThrough: true);
        }

        /// <summary>
        /// An overlay window's styles, applied at once: layered, never activated, no taskbar entry; click-through
        /// (WS_EX_TRANSPARENT) only when <paramref name="clickThrough"/>, so a layer that is the outside-click hitbox
        /// takes the click.
        /// </summary>
        public static void ApplyOverlayStylesNow(Window window, bool clickThrough)
        {
            try
            {
                nint handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (handle == IntPtr.Zero)
                {
                    return;
                }

                nint current = GetWindowLongPtr(handle, GwlExStyle);
                nint next = current | WsExLayered | WsExNoActivate | WsExToolWindow;
                next = clickThrough ? next | WsExTransparent : next & ~WsExTransparent;
                if (next != current)
                {
                    _ = SetWindowLongPtr(handle, GwlExStyle, next);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Win32 overlay styles] {ex.Message}");
            }
        }

        /// <summary><see cref="ApplyOverlayStylesNow"/>, posted until the window has loaded.</summary>
        public static void ApplyOverlayStyles(Window window, bool clickThrough)
        {
            Dispatcher.UIThread.Post(() => ApplyOverlayStylesNow(window, clickThrough), DispatcherPriority.Loaded);
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
        /// Clip stacked native translucency flyouts to pill/card geometry. HWND backdrop is rectangular;
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
