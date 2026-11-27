namespace MosaicShell.Core.HostPlatform
{
    /// <summary>Something a desktop may or may not let MosaicShell do. A module page row names the one it needs.</summary>
    public enum DesktopFeature
    {
        /// <summary>Shortcuts that work while another app has focus.</summary>
        GlobalHotkeys,

        /// <summary>Telling whether the app in front is full screen.</summary>
        FullscreenProbe,

        /// <summary>Seeing the play, pause and track keys as they are pressed.</summary>
        MediaKeys,

        /// <summary>An icon in the system tray that keeps MosaicShell reachable while its window is closed.</summary>
        Tray,
    }
}
