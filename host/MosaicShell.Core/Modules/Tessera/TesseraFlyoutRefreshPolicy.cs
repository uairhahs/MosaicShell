namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Capability event that drives flyout refresh cadence (Present vs Patch).
/// </summary>
public enum TesseraFlyoutRefreshTrigger
{
    VolumeTick,
    BrightnessTick,
    MediaSessionChanged,
    ShellMediaHook,
    StatusToggle,
}

/// <summary>
/// Present vs Patch for Tessera flyouts. Cold/hidden sessions Present; warm same-kind updates Patch.
/// Aligns with ModernFlyouts/YourFlyouts in-place bind patterns (see .cursor/docs/research/tessera-flyout-refresh-patterns.md).
/// </summary>
public static class TesseraFlyoutRefreshPolicy
{
    /// <summary>
    /// Resolve Present (cold/structural/revive) vs Patch (in-place live bind).
    /// </summary>
    public static TesseraFlyoutSyncAction ResolvePresentation(
        TesseraFlyoutRefreshTrigger trigger,
        bool isEffectivelyShowing,
        string openKind,
        string nextKind,
        string? openStyle,
        string? nextStyle,
        bool enableMediaFlyouts)
    {
        if (!isEffectivelyShowing)
            return TesseraFlyoutSyncAction.Present;

        if (!string.Equals(openKind, nextKind, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(openStyle ?? "", nextStyle ?? "", StringComparison.OrdinalIgnoreCase))
            return TesseraFlyoutSyncAction.Present;

        if (trigger == TesseraFlyoutRefreshTrigger.MediaSessionChanged
            && !enableMediaFlyouts
            && TesseraMediaFlyoutPolicy.IsStripKind(openKind))
            return TesseraFlyoutSyncAction.Patch;

        if (trigger == TesseraFlyoutRefreshTrigger.ShellMediaHook
            && enableMediaFlyouts
            && openKind.Equals("media", StringComparison.OrdinalIgnoreCase))
            return TesseraFlyoutSyncAction.Patch;

        if (trigger == TesseraFlyoutRefreshTrigger.MediaSessionChanged
            && enableMediaFlyouts
            && openKind.Equals("media", StringComparison.OrdinalIgnoreCase))
            return TesseraFlyoutSyncAction.Patch;

        return TesseraFlyoutSyncAction.Patch;
    }
}
