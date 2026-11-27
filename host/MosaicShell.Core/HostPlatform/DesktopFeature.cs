namespace MosaicShell.Core.HostPlatform
{
    /// <summary>Something a desktop may or may not let MosaicShell do. A module page row names the one it needs.</summary>
    public enum DesktopFeature
    {
        /// <summary>Shortcuts that work while another app has focus.</summary>
        GlobalHotkeys,

        /// <summary>Telling whether the app in front is full screen.</summary>
        FullscreenProbe,
    }
}
