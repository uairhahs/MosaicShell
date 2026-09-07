
using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// Tessera-facing alias for <see cref="FlyoutRefreshPolicy"/> (platform owns behavior).
    /// </summary>
    public static class TesseraFlyoutRefreshPolicy
    {
        public static TesseraFlyoutSyncAction ResolvePresentation(
            bool isEffectivelyShowing,
            string openKind,
            string nextKind,
            string? openStyle,
            string? nextStyle)
        {
            FlyoutSyncAction action = FlyoutRefreshPolicy.ResolvePresentation(
                isEffectivelyShowing,
                openKind,
                nextKind,
                openStyle,
                nextStyle);
            return action == FlyoutSyncAction.Present
                ? TesseraFlyoutSyncAction.Present
                : TesseraFlyoutSyncAction.Patch;
        }
    }
}
