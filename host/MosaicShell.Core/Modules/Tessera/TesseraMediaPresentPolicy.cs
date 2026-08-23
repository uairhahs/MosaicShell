namespace MosaicShell.Core.Modules.Tessera;

using MosaicShell.Core.Capabilities.Platform;

/// <summary>
/// Tessera-facing alias for <see cref="MediaPresentPolicy"/> (platform owns behavior).
/// </summary>
public static class TesseraMediaPresentPolicy
{
    public const bool MustPumpBeforeMediaPresent = MediaPresentPolicy.MustPumpBeforeMediaPresent;
    public const int PresentSettleMs = MediaPresentPolicy.PresentSettleMs;

    public static bool ShouldSchedulePresentSettle(string kind, TesseraFlyoutSyncAction action) =>
        MediaPresentPolicy.ShouldSchedulePresentSettle(
            kind,
            action == TesseraFlyoutSyncAction.Present
                ? FlyoutSyncAction.Present
                : FlyoutSyncAction.Patch);

    public static bool ShouldPumpBeforeBuild(string kind, TesseraFlyoutSyncAction action) =>
        MustPumpBeforeMediaPresent
        && kind.Equals("media", StringComparison.OrdinalIgnoreCase)
        && action == TesseraFlyoutSyncAction.Present;

    public static bool ShouldPumpBeforeShellMediaPresent => MediaPresentPolicy.ShouldPumpBeforeShellMediaPresent;
}
