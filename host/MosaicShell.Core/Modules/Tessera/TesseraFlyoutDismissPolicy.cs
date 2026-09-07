
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// Tessera-facing alias for <see cref="FlyoutDismissPolicy"/> (platform owns behavior).
    /// </summary>
    public static class TesseraFlyoutDismissPolicy
    {
        public static bool ShouldFinishTransientDismiss(
            int dismissalGeneration,
            int currentGeneration,
            TesseraFlyoutPhase phase,
            bool windowVisible)
        {
            return dismissalGeneration == currentGeneration
                && phase is TesseraFlyoutPhase.Exiting or TesseraFlyoutPhase.Hidden
                && windowVisible;
        }

        public static bool ShouldResetAutoDismiss(
            TesseraFlyoutRefreshTrigger trigger,
            MediaSessionInfo? previousMedia,
            MediaSessionInfo? nextMedia)
        {
            return FlyoutDismissPolicy.ShouldResetAutoDismiss(
                FlyoutSyncTriggerMapping.FromTessera(trigger),
                previousMedia,
                nextMedia);
        }
    }
}
