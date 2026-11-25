namespace MosaicShell.Core.HostPlatform
{
    /// <summary>
    /// The EWMH window type MosaicShell's overlays (flyout, focus dim layer, click glint) declare on X11. GNOME Shell
    /// animates opening and closing NORMAL, DIALOG and MODAL_DIALOG windows (it shrank and faded the dim layer on top of
    /// its own fade), and leaves UTILITY windows alone, so on GNOME the overlays are utility windows. Elsewhere they keep
    /// the default: KWin hides utility windows while their application is not active, which would hide every flyout.
    /// Kept pure so it is tested on every OS.
    /// </summary>
    public static class X11OverlayWindowTypePolicy
    {
        public const string Utility = "_NET_WM_WINDOW_TYPE_UTILITY";

        /// <summary>The type to declare before the first map, or null to keep the toolkit's default.</summary>
        public static string? WindowTypeFor(string? xdgCurrentDesktop)
        {
            return !string.IsNullOrEmpty(xdgCurrentDesktop)
                && xdgCurrentDesktop.Split(':').Contains("GNOME", StringComparer.OrdinalIgnoreCase)
                ? Utility
                : null;
        }
    }
}
