namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Tessera module view of capability refresh triggers (maps to <see cref="Capabilities.Platform.FlyoutSyncTrigger"/>).
/// </summary>
public enum TesseraFlyoutRefreshTrigger
{
    VolumeTick,
    BrightnessTick,
    MediaSessionChanged,
    ShellMediaHook,
    StatusToggle,
}
