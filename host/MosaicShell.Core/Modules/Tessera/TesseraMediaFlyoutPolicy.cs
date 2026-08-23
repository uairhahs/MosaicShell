namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// What Tessera should do when <see cref="Services.IMediaSessionService.Changed"/> fires
/// (track / play-state / art identity, not timeline ProgressChanged).
/// </summary>
public enum TesseraMediaChangeAction
{
    /// <summary>Media flyouts disabled and nothing to soft-refresh.</summary>
    Ignore,

    /// <summary>Update title/art on an already-visible vol/bright/media flyout.</summary>
    SoftRefreshVisible,

    /// <summary>Cold-present the dedicated media flyout.</summary>
    PresentMediaFlyout,
}

public static class TesseraMediaFlyoutPolicy
{
    public static TesseraMediaChangeAction Resolve(
        bool enableMediaFlyouts,
        bool flyoutVisible,
        string? lastKind)
    {
        if (flyoutVisible && IsStripKind(lastKind))
            return TesseraMediaChangeAction.SoftRefreshVisible;

        if (enableMediaFlyouts)
            return TesseraMediaChangeAction.PresentMediaFlyout;

        return TesseraMediaChangeAction.Ignore;
    }

    public static bool IsStripKind(string? kind) =>
        kind is not null
        && (kind.Equals("vol", StringComparison.OrdinalIgnoreCase)
            || kind.Equals("bright", StringComparison.OrdinalIgnoreCase)
            || kind.Equals("media", StringComparison.OrdinalIgnoreCase));
}
