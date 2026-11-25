
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>
    /// Normalized media-session reactions for flyout-capable modules (track boundaries, strip refresh).
    /// </summary>
    public enum MediaFlyoutAction
    {
        Ignore,
        SoftRefreshVisible,
        PresentMediaFlyout,
    }

    public static class MediaFlyoutRouter
    {
        public static MediaFlyoutAction Resolve(
            bool enableMediaFlyouts,
            bool flyoutVisible,
            string? lastKind)
        {
            if (flyoutVisible && IsStripKind(lastKind)
                && !FlyoutKinds.Is(lastKind, FlyoutKinds.Media))
            {
                return MediaFlyoutAction.SoftRefreshVisible;
            }

            // A visible status chip (caps/airplane) owns the session. Presenting media paints a
            // full media shell straight over the chip, which is larger than it and outlives it,
            // so the chip is never readable. Strip kinds soft-refresh instead; status kinds have
            // no media strip to refresh, so the change is simply dropped.
            return flyoutVisible && IsStatusKind(lastKind)
                ? MediaFlyoutAction.Ignore
                : enableMediaFlyouts ? MediaFlyoutAction.PresentMediaFlyout : MediaFlyoutAction.Ignore;
        }

        public static bool IsStripKind(string? kind)
        {
            return FlyoutKinds.Is(kind, FlyoutKinds.Volume)
                || FlyoutKinds.Is(kind, FlyoutKinds.Brightness)
                || FlyoutKinds.Is(kind, FlyoutKinds.Media);
        }

        /// <summary>
        /// Ephemeral status chips (caps lock, airplane). Single owner for this classification;
        /// <c>TesseraStatusFlyoutPolicy.IsStatusKind</c> delegates here (ADR-0001).
        /// </summary>
        public static bool IsStatusKind(string? kind)
        {
            return FlyoutKinds.Is(kind, FlyoutKinds.Locks) || FlyoutKinds.Is(kind, FlyoutKinds.Flight);
        }

        public static bool IsTrackBoundary(MediaSessionInfo? previous, MediaSessionInfo? next)
        {
            return next is not null && (previous is null || (!string.IsNullOrWhiteSpace(next.Title)
                && !string.Equals(previous.Title, next.Title, StringComparison.Ordinal)) || MediaSessionChangePolicy.LooksLikeNewTrackPosition(
                previous.PositionSeconds, next.PositionSeconds));
        }

        public static bool ShouldResetDismissForMediaChange(MediaSessionInfo? previous, MediaSessionInfo? next)
        {
            return IsTrackBoundary(previous, next);
        }
    }
}
