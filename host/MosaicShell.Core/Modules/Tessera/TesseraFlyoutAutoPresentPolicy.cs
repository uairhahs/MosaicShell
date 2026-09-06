
using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// Tessera-facing alias for <see cref="FlyoutAutoPresentPolicy"/> (platform owns behavior).
    /// </summary>
    public static class TesseraFlyoutAutoPresentPolicy
    {
        public static bool IsUserIntentTrigger(TesseraFlyoutRefreshTrigger trigger)
        {
            return FlyoutAutoPresentPolicy.IsUserIntentTrigger(FlyoutSyncTriggerMapping.FromTessera(trigger));
        }

        public static bool ShouldColdPresent(
            bool suppressAfterUserDismiss,
            TesseraFlyoutRefreshTrigger trigger,
            bool isTrackBoundary = false)
        {
            return FlyoutAutoPresentPolicy.ShouldColdPresent(
                suppressAfterUserDismiss,
                FlyoutSyncTriggerMapping.FromTessera(trigger),
                isTrackBoundary);
        }
    }
}
