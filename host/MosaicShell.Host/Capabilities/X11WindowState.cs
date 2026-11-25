using Avalonia;
using Avalonia.Controls;

namespace MosaicShell.Host.Capabilities
{
    /// <summary>
    /// Keeps a window's stacking state across hide and show off Windows. Avalonia's X11 backend sends
    /// _NET_WM_STATE (ABOVE, SKIP_TASKBAR) only when <see cref="Window.Topmost"/> or
    /// <see cref="Window.ShowInTaskbar"/> change, and the window manager clears that state when the window is hidden
    /// (unmapped), as EWMH requires. A flyout shown again would come back as an ordinary window: KWin stacks it
    /// under the active window and lists it in the taskbar. On Windows a hidden window keeps WS_EX_TOPMOST, so
    /// nothing is needed there.
    /// </summary>
    internal static class X11WindowState
    {
        private static readonly string? OverlayType =
            Core.HostPlatform.X11OverlayWindowTypePolicy.WindowTypeFor(Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP"));

        /// <summary>
        /// Declares <paramref name="window"/> an overlay (<see cref="Core.HostPlatform.X11OverlayWindowTypePolicy"/>), now,
        /// before its first map, and again on each open in case the toolkit wrote the type back.
        /// </summary>
        public static void DeclareOverlay(Window window)
        {
            if (OperatingSystem.IsWindows() || OverlayType is not string type)
            {
                return;
            }

            _ = X11WindowLayer.SetWindowType(window, type);
            window.Opened += (_, _) => X11WindowLayer.SetWindowType(window, type);
        }

        /// <summary>Sends the window's Topmost and taskbar state again each time it becomes visible.</summary>
        public static void ReassertOnShow(Window window)
        {
            if (OperatingSystem.IsWindows())
            {
                return;
            }

            window.PropertyChanged += (_, e) =>
            {
                if (e.Property == Visual.IsVisibleProperty && e.NewValue is true)
                {
                    Reassert(window);
                }
            };
        }

        private static void Reassert(Window window)
        {
            // Toggling is the only way to make Avalonia send the state again; the window manager applies both
            // messages in order, so the window ends where it should.
            if (window.Topmost)
            {
                window.Topmost = false;
                window.Topmost = true;
            }

            if (!window.ShowInTaskbar)
            {
                window.ShowInTaskbar = true;
                window.ShowInTaskbar = false;
            }
        }
    }
}
