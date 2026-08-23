namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Shell-hook and early SMTC ticks can present a media flyout before title/art catch up.
/// </summary>
public static class TesseraMediaPresentPolicy
{
    /// <summary>Pump SMTC/WNP once before building a cold media flyout request.</summary>
    public const bool MustPumpBeforeMediaPresent = true;

    /// <summary>One-shot refresh after present so late title/art updates land on the flyout.</summary>
    public const int PresentSettleMs = 450;

    public static bool ShouldSchedulePresentSettle(string kind, TesseraFlyoutSyncAction action) =>
        kind.Equals("media", StringComparison.OrdinalIgnoreCase)
        && action == TesseraFlyoutSyncAction.Present;

    public static bool ShouldPumpBeforeBuild(string kind, TesseraFlyoutSyncAction action) =>
        MustPumpBeforeMediaPresent
        && kind.Equals("media", StringComparison.OrdinalIgnoreCase)
        && action == TesseraFlyoutSyncAction.Present;

    /// <summary>Only shell-hook presents may pump before show; never during Media.Changed handling.</summary>
    public static bool ShouldPumpBeforeShellMediaPresent => MustPumpBeforeMediaPresent;
}
