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

        /// <summary>
        /// Whether this OS has the watcher at all. It is a WH_MOUSE_LL hook in user32, so only Windows does; elsewhere
        /// the Host must not arm it (every start would throw inside a dispatcher timer) and outside clicks do not
        /// dismiss until that platform has its own watcher. Pass <c>OperatingSystem.IsWindows()</c>.
        /// </summary>
        public static bool PlatformCanWatch(bool isWindows)
        {
            return isWindows;
        }

        /// <summary>
        /// Delay between a present and the watcher's arm. It was 1.5 s, because the watcher snapshotted the flyout's
        /// bounds through Avalonia and had to wait for the entrance to settle, so the first click outside a fresh
        /// flyout was ignored. The hook now reads each flyout HWND's live rect at the press (<see cref="IsOutside"/>),
        /// so it arms at once, as the X11 layer takes clicks from the moment it shows.
        /// </summary>
        public const int ArmDelayMs = 0;

        /// <summary>
        /// A press at (<paramref name="x"/>, <paramref name="y"/>) is outside when no flyout rect (left, top, right,
        /// bottom; right and bottom exclusive, as GetWindowRect) holds it. With no rect read, nothing is outside.
        /// </summary>
        public static bool IsOutside(int x, int y, IReadOnlyList<(int Left, int Top, int Right, int Bottom)> rects)
        {
            if (rects.Count == 0)
            {
                return false;
            }

            foreach ((int left, int top, int right, int bottom) in rects)
            {
                if (x >= left && x < right && y >= top && y < bottom)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
