
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>
    /// Which flyout updates reset auto-dismiss vs SoftRefresh only.
    /// </summary>
    public static class FlyoutDismissPolicy
    {
        public static bool ShouldResetAutoDismiss(
            FlyoutSyncTrigger trigger,
            MediaSessionInfo? previousMedia,
            MediaSessionInfo? nextMedia)
        {
            return trigger switch
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
    }
}
