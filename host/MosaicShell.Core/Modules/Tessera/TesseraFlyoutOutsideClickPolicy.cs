namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Outside-click dismiss contracts for Tessera flyout sessions.
/// </summary>
public static class TesseraFlyoutOutsideClickPolicy
{
    /// <summary>Single-window sessions use one HWND bounds snapshot.</summary>
    public const bool SingleWindowUsesPrimaryBounds = true;

    /// <summary>
    /// N-window stacked acrylic must treat clicks outside the union of all slot bounds as dismiss.
    /// </summary>
    public static bool MustUseUnionBounds(bool multiWindowStackedAcrylic) =>
        multiWindowStackedAcrylic && TesseraOsAcrylicStackedPolicy.OutsideClickUsesUnionBounds;

    /// <summary>Low-level hook must never call into Avalonia on the hook thread.</summary>
    public const bool BoundsSnapshotOnUiThreadOnly = true;

    /// <summary>Re-arm after Present only; Patch must not reinstall the hook (live sync policy).</summary>
    public const bool RearmOnlyOnPresent = true;

    /// <summary>
    /// Patch must snapshot current HWND bounds into the live watcher without reinstalling
    /// WH_MOUSE_LL. Stacked placement can move slots after a live patch.
    /// </summary>
    public const bool PatchMustRefreshBoundsWithoutRearm = true;

    /// <summary>
    /// Present must dispose the live outside-click watcher before scheduling the arm delay.
    /// Stopping only the arm timer leaves stale HWND bounds for ~1.5s across CapsLock↔volume.
    /// </summary>
    public static bool PresentMustStopPriorWatcherBeforeRearm =>
        TesseraFlyoutPresentHandoffPolicy.PresentMustStopOutsideClickBeforeRearm;
}
