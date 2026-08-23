namespace MosaicShell.Core.Capabilities.Platform;

using MosaicShell.Core.Modules.Tessera;

/// <summary>Maps Tessera module triggers to platform flyout triggers (tests and TesseraCapability).</summary>
public static class FlyoutSyncTriggerMapping
{
    public static FlyoutSyncTrigger FromTessera(TesseraFlyoutRefreshTrigger trigger) =>
        trigger switch
        {
            TesseraFlyoutRefreshTrigger.VolumeTick => FlyoutSyncTrigger.Volume,
            TesseraFlyoutRefreshTrigger.BrightnessTick => FlyoutSyncTrigger.Brightness,
            TesseraFlyoutRefreshTrigger.MediaSessionChanged => FlyoutSyncTrigger.MediaSession,
            TesseraFlyoutRefreshTrigger.ShellMediaHook => FlyoutSyncTrigger.ShellMedia,
            TesseraFlyoutRefreshTrigger.StatusToggle => FlyoutSyncTrigger.StatusToggle,
            _ => FlyoutSyncTrigger.Volume,
        };

    public static FlyoutSyncAction ToPlatform(TesseraFlyoutSyncAction action) =>
        action == TesseraFlyoutSyncAction.Present
            ? FlyoutSyncAction.Present
            : FlyoutSyncAction.Patch;
}
