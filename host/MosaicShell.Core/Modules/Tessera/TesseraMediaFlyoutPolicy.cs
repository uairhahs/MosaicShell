namespace MosaicShell.Core.Modules.Tessera;

using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Services;

/// <summary>
/// Tessera-facing alias for <see cref="MediaFlyoutRouter"/> (platform owns behavior).
/// </summary>
public static class TesseraMediaFlyoutPolicy
{
    public const int ArmedTimelinePollMs = 500;
    public static bool MustPollTimelineWhileArmed => false;

    public static TesseraMediaChangeAction Resolve(
        bool enableMediaFlyouts,
        bool flyoutVisible,
        string? lastKind)
    {
        return (TesseraMediaChangeAction)MediaFlyoutRouter.Resolve(
            enableMediaFlyouts, flyoutVisible, lastKind);
    }

    public static bool IsStripKind(string? kind) => MediaFlyoutRouter.IsStripKind(kind);

    public static bool IsTrackBoundary(MediaSessionInfo? previous, MediaSessionInfo? next) =>
        MediaFlyoutRouter.IsTrackBoundary(previous, next);

    public static bool ShouldResetDismissForMediaChange(MediaSessionInfo? previous, MediaSessionInfo? next) =>
        MediaFlyoutRouter.ShouldResetDismissForMediaChange(previous, next);
}
