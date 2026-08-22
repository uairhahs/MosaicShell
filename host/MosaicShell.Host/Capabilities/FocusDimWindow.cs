using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace MosaicShell.Host.Capabilities;

/// <summary>
/// Full-screen dim behind Tessera flyouts. Hit-testing is off in Avalonia;
/// Win32 WS_EX_TRANSPARENT is kept so mouse still reaches windows underneath
/// (Avalonia IsHitTestVisible alone does not make the HWND click-through).
/// </summary>
internal sealed class FocusDimWindow : Window
{
    private int _monitorIndex;

    private static readonly IBrush FallbackBrush =
        new SolidColorBrush(Color.FromArgb(200, 0x11, 0x11, 0x1b));

    public FocusDimWindow(int monitorIndexOneBased)
    {
        _monitorIndex = monitorIndexOneBased;
        Title = "MosaicShell - Focus dim";
        SystemDecorations = SystemDecorations.None;
        CanResize = false;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        Focusable = false;
        IsHitTestVisible = false;
        // docs: Transparent + Background Transparent + TransparencyBackgroundFallback
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Background = Brushes.Transparent;
        TransparencyBackgroundFallback = FallbackBrush;
        Opacity = 0;
        Content = new Border
        {
            IsHitTestVisible = false,
            Background = new SolidColorBrush(Color.FromArgb(68, 17, 17, 27))
        };
        Opened += (_, _) =>
        {
            PlaceOnMonitor(_monitorIndex);
            Win32WindowChrome.ApplyClickThrough(this);
        };
    }

    public void PlaceOnMonitor(int monitorIndexOneBased)
    {
        _monitorIndex = monitorIndexOneBased;
        try
        {
            var screens = Screens?.All?.ToList() ?? [];
            var screen = ResolveScreen(screens, _monitorIndex) ?? Screens?.Primary;
            if (screen is null) return;

            var bounds = screen.Bounds;
            var scale = screen.Scaling > 0.1 ? screen.Scaling : 1.0;
            Position = new PixelPoint(bounds.X, bounds.Y);
            Width = Math.Max(1, bounds.Width / scale);
            Height = Math.Max(1, bounds.Height / scale);
            Win32WindowChrome.ApplyClickThrough(this);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FocusDim] {ex.Message}");
        }
    }

    public void FadeIn()
    {
        Win32WindowChrome.ApplyClickThrough(this);
        Opacity = 1;
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
        if (screens.Count == 0) return null;
        if (monitorIndexOneBased <= 1)
            return screens.FirstOrDefault(s => s.IsPrimary) ?? screens[0];

        var idx = Math.Clamp(monitorIndexOneBased - 1, 0, screens.Count - 1);
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

    public static void ApplyClickThrough(Window window)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (handle == IntPtr.Zero) return;

                var current = GetWindowLongPtr(handle, GwlExStyle);
                var next = current | WsExLayered | WsExTransparent | WsExNoActivate | WsExToolWindow;
                if (next != current)
                    SetWindowLongPtr(handle, GwlExStyle, next);
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
            var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (handle == IntPtr.Zero) return;
            SetWindowPos(handle, insertAfter, 0, 0, 0, 0,
                SwpNoMove | SwpNoSize | SwpNoActivate);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Win32 z-order] {ex.Message}");
        }
    }

    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoActivate = 0x0010;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
}
