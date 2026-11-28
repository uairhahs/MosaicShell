
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// Tessera-facing alias for <see cref="FlyoutDismissPolicy"/> (platform owns behavior).
    /// </summary>
    public static class TesseraFlyoutDismissPolicy
    {
        /// <summary>How long a flyout stays once the pointer has left it, at most (see <see cref="ResolvePointerLeaveDismissMs"/>).</summary>
        public const int PointerLeaveDismissMs = 1000;

        /// <summary>
        /// The dismiss timer to restart when the pointer leaves the flyout: <see cref="PointerLeaveDismissMs"/>, or the
        /// flyout's own timer when that is shorter. Zero (or less) means the flyout never leaves on its own, and that
        /// still holds after a leave. Clicks outside pass through (<see cref="TesseraFocusDimPolicy.LayerPassesClicksThrough"/>),
        /// so moving away is how most people send a flyout off once they have used it.
        /// </summary>
        public static int ResolvePointerLeaveDismissMs(int autoDismissMs)
        {
            return autoDismissMs <= 0 ? 0 : Math.Min(autoDismissMs, PointerLeaveDismissMs);
        }

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

        /// <summary>
        /// Whether a stacked dismiss starts an exit. While an exit runs (<paramref name="runningExitMotionGenerations"/>,
        /// read right after it began; null when none runs) and every slot is still on that motion, a second dismiss
        /// would only restart the fade from part way and replay the collapse, so it leaves that exit to finish. Once a
        /// present supersedes the exit the generations move and a dismiss begins a new one; the phase cannot tell,
        /// because re-presented slots can still report <see cref="TesseraFlyoutPhase.Exiting"/>.
        /// </summary>
        public static bool ShouldBeginStackedDismiss(
            IReadOnlyList<int>? runningExitMotionGenerations,
            IReadOnlyList<int> currentMotionGenerations)
        {
            return runningExitMotionGenerations is null
                || !runningExitMotionGenerations.SequenceEqual(currentMotionGenerations);
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
