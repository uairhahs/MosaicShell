namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>
    /// Where MosaicShell cannot see the media keys (GNOME today), a player switching between playing and paused is
    /// taken as the play/pause key it almost always comes from, so the media flyout opens as it does for the key. A
    /// pause the app makes on its own opens it too; that is the cost until the desktop has a key route.
    /// </summary>
    public static class MediaKeyStandInPolicy
    {
        public static FlyoutSyncTrigger Trigger(MediaSessionSignal signal, bool hasMediaKeyRoute)
        {
            return signal.IsPlaybackToggle && !hasMediaKeyRoute ? FlyoutSyncTrigger.ShellMedia : FlyoutSyncTrigger.MediaSession;
        }
    }
}
