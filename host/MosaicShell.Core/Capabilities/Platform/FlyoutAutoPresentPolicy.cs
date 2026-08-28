namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>
    /// After user dismiss (auto or outside click), block cold Present/revive from passive events
    /// until the next user-intent trigger (volume, brightness, lock, shell media hook) or a genuine
    /// track boundary from the player (title change or position restart).
    /// </summary>
    public static class FlyoutAutoPresentPolicy
    {
        public static bool IsUserIntentTrigger(FlyoutSyncTrigger trigger)
        {
            return trigger is FlyoutSyncTrigger.Volume
                or FlyoutSyncTrigger.Brightness
                or FlyoutSyncTrigger.StatusToggle
                or FlyoutSyncTrigger.ShellMedia;
        }

        public static bool ShouldColdPresent(
            bool suppressAfterUserDismiss,
            FlyoutSyncTrigger trigger,
            bool isTrackBoundary = false)
        {
            return !suppressAfterUserDismiss
            || IsUserIntentTrigger(trigger)
            || (trigger == FlyoutSyncTrigger.MediaSession && isTrackBoundary);
        }
    }
}
