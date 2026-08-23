namespace MosaicShell.Core.Modules.Tessera;

using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Services;

/// <summary>
/// Tessera-facing alias for <see cref="FlyoutDismissPolicy"/> (platform owns behavior).
/// </summary>
public static class TesseraFlyoutDismissPolicy
{
    public static bool ShouldResetAutoDismiss(
        TesseraFlyoutRefreshTrigger trigger,
        MediaSessionInfo? previousMedia,
        MediaSessionInfo? nextMedia) =>
        FlyoutDismissPolicy.ShouldResetAutoDismiss(
            FlyoutSyncTriggerMapping.FromTessera(trigger),
            previousMedia,
            nextMedia);
}
