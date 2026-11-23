namespace MosaicShell.Core.Modules
{
    /// <summary>The stacking layer a tile window sits on, as its "Change Z layer" menu offers.</summary>
    public enum TileZLayer
    {
        /// <summary>Under every other window, on the desktop.</summary>
        Desktop,

        /// <summary>Stacked like any ordinary window.</summary>
        Normal,

        /// <summary>Above every ordinary window.</summary>
        AlwaysOnTop,
    }

    /// <summary>
    /// How each <see cref="TileZLayer"/> maps to window manager state. Windows orders the HWND with SetWindowPos.
    /// On X11 the layers are the EWMH states ABOVE (Avalonia's Topmost) and BELOW, which Avalonia has no way to send,
    /// so the Host sends it; both are cleared by the window manager when the window is hidden.
    /// </summary>
    public static class TileZLayerPolicy
    {
        /// <summary>Desktop widgets, and Pulse whatever its kind, start on the desktop; other tiles start on top.</summary>
        public static TileZLayer InitialLayer(ModuleKind kind, string moduleId)
        {
            return kind == ModuleKind.Widget || moduleId.Equals("Pulse", StringComparison.OrdinalIgnoreCase)
                ? TileZLayer.Desktop
                : TileZLayer.AlwaysOnTop;
        }

        public static bool IsTopmost(TileZLayer layer)
        {
            return layer == TileZLayer.AlwaysOnTop;
        }

        /// <summary>Whether the window carries the "below" state (EWMH _NET_WM_STATE_BELOW, Win32 HWND_BOTTOM).</summary>
        public static bool IsBelow(TileZLayer layer)
        {
            return layer == TileZLayer.Desktop;
        }

        /// <summary>Whether this system has a way to put a window under the others.</summary>
        public static bool CanPlaceBelow(bool isWindows, bool hasX11Display)
        {
            return isWindows || hasX11Display;
        }
    }
}
