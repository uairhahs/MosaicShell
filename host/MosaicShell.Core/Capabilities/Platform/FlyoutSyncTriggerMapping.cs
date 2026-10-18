
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>Maps Tessera module triggers to platform flyout triggers (tests and TesseraCapability).</summary>
    public static class FlyoutSyncTriggerMapping
    {
        public static FlyoutSyncTrigger FromTessera(TesseraFlyoutRefreshTrigger trigger)
        {
            return trigger switch
            {
                TesseraFlyoutRefreshTrigger.VolumeTick => FlyoutSyncTrigger.Volume,
                TesseraFlyoutRefreshTrigger.BrightnessTick => FlyoutSyncTrigger.Brightness,
                TesseraFlyoutRefreshTrigger.MediaSessionChanged => FlyoutSyncTrigger.MediaSession,
                TesseraFlyoutRefreshTrigger.ShellMediaHook => FlyoutSyncTrigger.ShellMedia,
                TesseraFlyoutRefreshTrigger.StatusToggle => FlyoutSyncTrigger.StatusToggle,
                _ => FlyoutSyncTrigger.Volume,
            };
        }
    }
}
