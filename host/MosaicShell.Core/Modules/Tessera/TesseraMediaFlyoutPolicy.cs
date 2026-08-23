using MosaicShell.Core.Services;

namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// What Tessera should do when <see cref="Services.IMediaSessionService.Changed"/> fires
/// (track / play-state / art identity, not timeline ProgressChanged).
/// </summary>
public enum TesseraMediaChangeAction
{
    /// <summary>Media flyouts disabled and nothing to soft-refresh.</summary>
    Ignore,

    /// <summary>Update title/art on an already-visible vol/bright/media flyout.</summary>
    SoftRefreshVisible,

    /// <summary>Cold-present the dedicated media flyout.</summary>
    PresentMediaFlyout,
}

public static class TesseraMediaFlyoutPolicy
{
    public static TesseraMediaChangeAction Resolve(
        bool enableMediaFlyouts,
        bool flyoutVisible,
        string? lastKind)
    {
        if (flyoutVisible && IsStripKind(lastKind)
            && !string.Equals(lastKind, "media", StringComparison.OrdinalIgnoreCase))
            return TesseraMediaChangeAction.SoftRefreshVisible;

        if (enableMediaFlyouts)
            return TesseraMediaChangeAction.PresentMediaFlyout;

        return TesseraMediaChangeAction.Ignore;
    }

    public static bool IsStripKind(string? kind) =>
        kind is not null
        && (kind.Equals("vol", StringComparison.OrdinalIgnoreCase)
            || kind.Equals("bright", StringComparison.OrdinalIgnoreCase)
            || kind.Equals("media", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// While Tessera is armed, poll SMTC/WNP timeline even when no flyout is showing.
    /// Host only pumps while visible; YTM/Chrome often skip MediaPropertiesChanged on skip.
    /// </summary>
    public const int ArmedTimelinePollMs = 500;

    public static bool MustPollTimelineWhileArmed => true;

    /// <summary>
    /// True when <see cref="IMediaSessionService.Changed"/> reflects a new track, not late art or play-state only.
    /// </summary>
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

    /// <summary>
    /// Only title changes (or first present) reset auto-dismiss. Position restarts with a stale
    /// title and late art must SoftRefresh so YTM/SMTC churn does not hold the flyout open forever.
    /// </summary>
    public static bool ShouldResetDismissForMediaChange(MediaSessionInfo? previous, MediaSessionInfo? next)
    {
        if (next is null) return false;
        if (previous is null) return true;

        return !string.IsNullOrWhiteSpace(next.Title)
               && !string.Equals(previous.Title, next.Title, StringComparison.Ordinal);
    }
}
