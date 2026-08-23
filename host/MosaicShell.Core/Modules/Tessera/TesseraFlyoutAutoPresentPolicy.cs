namespace MosaicShell.Core.Modules.Tessera;

using MosaicShell.Core.Capabilities.Platform;

/// <summary>
/// Tessera-facing alias for <see cref="FlyoutAutoPresentPolicy"/> (platform owns behavior).
/// </summary>
public static class TesseraFlyoutAutoPresentPolicy
{
    public static bool IsUserIntentTrigger(TesseraFlyoutRefreshTrigger trigger) =>
        FlyoutAutoPresentPolicy.IsUserIntentTrigger(FlyoutSyncTriggerMapping.FromTessera(trigger));

    public static bool ShouldColdPresent(
        bool suppressAfterUserDismiss,
        TesseraFlyoutRefreshTrigger trigger,
        bool isTrackBoundary = false) =>
        FlyoutAutoPresentPolicy.ShouldColdPresent(
            suppressAfterUserDismiss,
            FlyoutSyncTriggerMapping.FromTessera(trigger),
            isTrackBoundary);
}
