namespace MosaicShell.Core.Capabilities.Platform;

/// <summary>
/// After user dismiss (auto or outside click), block cold Present/revive from passive events
/// until the next user-intent trigger (volume, brightness, lock, shell media hook).
/// </summary>
public static class FlyoutAutoPresentPolicy
{
    public static bool IsUserIntentTrigger(FlyoutSyncTrigger trigger) =>
        trigger is FlyoutSyncTrigger.Volume
            or FlyoutSyncTrigger.Brightness
            or FlyoutSyncTrigger.StatusToggle
            or FlyoutSyncTrigger.ShellMedia;

    public static bool ShouldColdPresent(bool suppressAfterUserDismiss, FlyoutSyncTrigger trigger) =>
        !suppressAfterUserDismiss || IsUserIntentTrigger(trigger);
}
