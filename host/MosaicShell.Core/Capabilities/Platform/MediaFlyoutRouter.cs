namespace MosaicShell.Core.Capabilities.Platform;

using MosaicShell.Core.Services;

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
            && !string.Equals(lastKind, "media", StringComparison.OrdinalIgnoreCase))
            return MediaFlyoutAction.SoftRefreshVisible;

        if (enableMediaFlyouts)
            return MediaFlyoutAction.PresentMediaFlyout;

        return MediaFlyoutAction.Ignore;
    }

    public static bool IsStripKind(string? kind) =>
        kind is not null
        && (kind.Equals("vol", StringComparison.OrdinalIgnoreCase)
            || kind.Equals("bright", StringComparison.OrdinalIgnoreCase)
            || kind.Equals("media", StringComparison.OrdinalIgnoreCase));

    public static bool IsTrackBoundary(MediaSessionInfo? previous, MediaSessionInfo? next)
    {
        if (next is null) return false;
        if (previous is null) return true;

        if (!string.IsNullOrWhiteSpace(next.Title)
            && !string.Equals(previous.Title, next.Title, StringComparison.Ordinal))
            return true;

        return MediaSessionChangePolicy.LooksLikeNewTrackPosition(
            previous.PositionSeconds, next.PositionSeconds);
    }

    public static bool ShouldResetDismissForMediaChange(MediaSessionInfo? previous, MediaSessionInfo? next) =>
        IsTrackBoundary(previous, next);
}
