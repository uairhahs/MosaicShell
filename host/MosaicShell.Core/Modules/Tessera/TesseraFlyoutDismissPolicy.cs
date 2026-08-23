namespace MosaicShell.Core.Modules.Tessera;

using MosaicShell.Core.Services;

/// <summary>
/// Which Tessera flyout updates reset the auto-dismiss timer vs in-place SoftRefresh only.
/// Timeline ticks and late album art must not extend dismiss forever (see TesseraCapability OnMediaProgress).
/// </summary>
public static class TesseraFlyoutDismissPolicy
{
    /// <summary>
    /// Patch/Update paths that represent user intent reset auto-dismiss.
    /// Media session changes delegate to <see cref="TesseraMediaFlyoutPolicy.ShouldResetDismissForMediaChange"/>.
    /// </summary>
    public static bool ShouldResetAutoDismiss(
        TesseraFlyoutRefreshTrigger trigger,
        MediaSessionInfo? previousMedia,
        MediaSessionInfo? nextMedia) =>
        trigger switch
        {
            TesseraFlyoutRefreshTrigger.MediaSessionChanged =>
                TesseraMediaFlyoutPolicy.ShouldResetDismissForMediaChange(previousMedia, nextMedia),
            TesseraFlyoutRefreshTrigger.VolumeTick
                or TesseraFlyoutRefreshTrigger.BrightnessTick
                or TesseraFlyoutRefreshTrigger.StatusToggle
                or TesseraFlyoutRefreshTrigger.ShellMediaHook => true,
            _ => false,
        };
}
