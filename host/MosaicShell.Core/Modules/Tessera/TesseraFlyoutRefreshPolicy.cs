
using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// Tessera-facing alias for <see cref="FlyoutRefreshPolicy"/> (platform owns behavior).
    /// </summary>
    public static class TesseraFlyoutRefreshPolicy
    {
        public static TesseraFlyoutSyncAction ResolvePresentation(
            TesseraFlyoutRefreshTrigger trigger,
            bool isEffectivelyShowing,
            string openKind,
            string nextKind,
            string? openStyle,
            string? nextStyle,
            bool enableMediaFlyouts)
        {
            FlyoutSyncAction action = FlyoutRefreshPolicy.ResolvePresentation(
                FlyoutSyncTriggerMapping.FromTessera(trigger),
                isEffectivelyShowing,
                openKind,
                nextKind,
                openStyle,
                nextStyle,
                enableMediaFlyouts);
            return action == FlyoutSyncAction.Present
                ? TesseraFlyoutSyncAction.Present
                : TesseraFlyoutSyncAction.Patch;
        }
    }
}
