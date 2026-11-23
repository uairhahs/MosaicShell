using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// Outside-click dismiss contracts for Tessera flyout sessions.
    /// </summary>
    public static class TesseraFlyoutOutsideClickPolicy
    {

        /// <summary>
        /// Patch must snapshot current HWND bounds into the live watcher without reinstalling
        /// WH_MOUSE_LL. Stacked placement can move slots after a live patch.
        /// </summary>
        public const bool PatchMustRefreshBoundsWithoutRearm = true;

        /// <summary>
        /// Present must dispose the live outside-click watcher before scheduling the arm delay.
        /// Stopping only the arm timer leaves stale HWND bounds for ~1.5s across CapsLock↔volume.
        /// </summary>
        public static bool PresentMustStopPriorWatcherBeforeRearm =>
            TesseraFlyoutPresentHandoffPolicy.PresentMustStopOutsideClickBeforeRearm;

        /// <summary>
        /// Whether an ingress must schedule the watcher's arm. Transient dismiss stops the watcher, so every ingress
        /// that presents the session must arm it again, a revived hidden window included (see
        /// <see cref="FlyoutIngressPolicy.PresentsSession"/>).
        /// </summary>
        public static bool MustArmOnIngress(FlyoutIngressDecision decision)
        {
            return FlyoutIngressPolicy.PresentsSession(decision);
        }
    }
}
