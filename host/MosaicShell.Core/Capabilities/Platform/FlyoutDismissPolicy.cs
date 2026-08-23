namespace MosaicShell.Core.Capabilities.Platform;

using MosaicShell.Core.Services;

/// <summary>
/// Which flyout updates reset auto-dismiss vs SoftRefresh only.
/// </summary>
public static class FlyoutDismissPolicy
{
    public static bool ShouldResetAutoDismiss(
        FlyoutSyncTrigger trigger,
        MediaSessionInfo? previousMedia,
        MediaSessionInfo? nextMedia) =>
        trigger switch
        {
            FlyoutSyncTrigger.MediaSession =>
                MediaFlyoutRouter.ShouldResetDismissForMediaChange(previousMedia, nextMedia),
            FlyoutSyncTrigger.Volume
                or FlyoutSyncTrigger.Brightness
                or FlyoutSyncTrigger.StatusToggle
                or FlyoutSyncTrigger.ShellMedia => true,
            _ => false,
        };
}
