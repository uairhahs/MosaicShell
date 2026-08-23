namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Present session vs content session for Tessera flyouts.
/// High-frequency Audio.Changed must Patch (coalesced), never Present/restack/re-arm.
/// </summary>
public enum TesseraFlyoutSyncAction
{
    /// <summary>Open or rebuild HWND session (Z-order, FocusDim, outside-click arm).</summary>
    Present,

    /// <summary>In-place binding update only; must not Present.</summary>
    Patch,
}

public static class TesseraFlyoutLiveSyncPolicy
{
    /// <summary>Bounded UI flush rate for volume/brightness patches (research: coalesce, not per-event Post).</summary>
    public const int MaxRefreshHz = 30;

    public static TimeSpan MinFlushInterval { get; } = TimeSpan.FromMilliseconds(1000.0 / MaxRefreshHz);

    public const bool PatchImpliesPresent = false;
    public const bool PatchImpliesWin32Restack = false;
    public const bool PatchImpliesOutsideClickRearm = false;

    /// <summary>
    /// Host dictionary Closed handlers must only Remove when the closing window is still
    /// the registered instance. Otherwise a superseded flyout's Closed unregisters the
    /// live session and rapid Try now stacks orphan SoftFrost HWNDs.
    /// </summary>
    public const bool ClosedMustOnlyUnregisterSameInstance = true;

    /// <summary>
    /// While a flyout HWND is registered, Host must ApplyRequest/Show on that instance,
    /// never Close()+new. SoftFrost composition surfaces overlap and stack when recreated.
    /// </summary>
    public static bool MustReuseRegisteredFlyoutHwnd => true;

    /// <summary>
    /// Auto-dismiss / outside-click must <c>Hide</c> (opacity 0) the SoftFrost HWND, not
    /// <c>Close</c>, so the next Present can revive the same surface without a dying HWND.
    /// </summary>
    public static bool TransientDismissMustHideNotClose => true;

    /// <summary>
    /// SoftFrost starts / dismisses at Opacity 0 while HWND.IsVisible can still be true.
    /// Soft-refresh and capability IsVisible must use this so track-change presents a flyout
    /// instead of patching an invisible surface.
    /// </summary>
    public const double MinVisibleOpacity = 0.05;

    public static bool IsEffectivelyShowing(bool windowVisible, double opacity) =>
        windowVisible && opacity > MinVisibleOpacity;

    /// <summary>Live pump may call Media.PumpTimeline while visible.</summary>
    public const bool PumpMayAdvanceMediaTimeline = true;

    /// <summary>Volume bindings have a single owner: coalesced Patch, not the pump.</summary>
    public const bool PumpMayWriteVolumeBindings = false;

    public const bool LiveHostRequiredForTesseraSuccess = true;

    public static bool IsSuccessfulTesseraContent(bool hasLiveHost, bool isFallbackContent) =>
        LiveHostRequiredForTesseraSuccess
            ? hasLiveHost && !isFallbackContent
            : !isFallbackContent;

    /// <summary>
    /// Visible + same kind/style → Patch. Otherwise Present (cold show or structural change).
    /// </summary>
    public static TesseraFlyoutSyncAction ResolveAction(
        bool isVisible,
        string openKind,
        string nextKind,
        string? openStyle,
        string? nextStyle)
    {
        if (!isVisible)
            return TesseraFlyoutSyncAction.Present;

        if (!string.Equals(openKind, nextKind, StringComparison.OrdinalIgnoreCase))
            return TesseraFlyoutSyncAction.Present;

        if (!string.Equals(openStyle ?? "", nextStyle ?? "", StringComparison.OrdinalIgnoreCase))
            return TesseraFlyoutSyncAction.Present;

        return TesseraFlyoutSyncAction.Patch;
    }
}

/// <summary>
/// Last-value coalesce gate: immediate flush after idle, then at most one flush per <see cref="MinInterval"/>.
/// Host schedules UI work only when <see cref="TryBeginFlush"/> returns true.
/// </summary>
public sealed class TesseraFlyoutLiveSyncCoalescer
{
    private readonly TimeSpan _minInterval;
    private TimeSpan? _lastFlush;

    public TesseraFlyoutLiveSyncCoalescer(TimeSpan minInterval)
    {
        if (minInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(minInterval));
        _minInterval = minInterval;
    }

    public TesseraFlyoutLiveSyncCoalescer()
        : this(TesseraFlyoutLiveSyncPolicy.MinFlushInterval)
    {
    }

    public bool TryBeginFlush(TimeSpan now) => TryBeginFlush(now, out _);

    /// <summary>
    /// When false, <paramref name="retryAfter"/> is the wait until the next allowed flush
    /// (Host schedules a deferred last-value patch).
    /// </summary>
    public bool TryBeginFlush(TimeSpan now, out TimeSpan retryAfter)
    {
        if (_lastFlush is { } last)
        {
            var elapsed = now - last;
            if (elapsed < _minInterval)
            {
                retryAfter = _minInterval - elapsed;
                return false;
            }
        }

        _lastFlush = now;
        retryAfter = TimeSpan.Zero;
        return true;
    }

    public void Reset() => _lastFlush = null;
}
