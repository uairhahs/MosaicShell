using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using MosaicShell.Core.Modules.Tessera;

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

                _ = SetWindowRgn(handle, IntPtr.Zero, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Win32 region clear] {ex.Message}");
            }
        }

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
