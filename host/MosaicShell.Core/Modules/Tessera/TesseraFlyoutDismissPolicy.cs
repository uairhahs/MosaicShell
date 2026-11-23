
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

        /// <summary>
        /// Stacked form of <see cref="ShouldFinishTransientDismiss"/>. A same-kind request that lands during the exit
        /// re-presents the slots without starting a new session, so the session generation alone cannot see it; each
        /// slot's motion generation can, because the re-present supersedes its motion. The dismissal finishes only when
        /// every slot still runs the exit it started (<paramref name="exitMotionGenerations"/>, read right after the
        /// exit began) and the single-window rules hold.
        /// </summary>
        public static bool ShouldFinishStackedTransientDismiss(
            int dismissalSessionGeneration,
            int currentSessionGeneration,
            IReadOnlyList<int> exitMotionGenerations,
            IReadOnlyList<int> currentMotionGenerations,
            TesseraFlyoutPhase phase,
            bool anyWindowVisible)
        {
            return exitMotionGenerations.SequenceEqual(currentMotionGenerations)
                && ShouldFinishTransientDismiss(dismissalSessionGeneration, currentSessionGeneration, phase, anyWindowVisible);
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
