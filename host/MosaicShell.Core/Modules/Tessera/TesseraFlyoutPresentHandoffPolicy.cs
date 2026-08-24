namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// CapsLock ↔ volume/media Present handoffs. Pending Update/Patch queues and a live
/// outside-click hook from the prior session must not outlive a kind or stack-mode change.
/// </summary>
public static class TesseraFlyoutPresentHandoffPolicy
{
    /// <summary>
    /// Kind, style, or single↔stacked mode change must cancel coalesced Update / deferred Patch
    /// so a late flush cannot replace the new session.
    /// </summary>
    public const bool PresentMustInvalidatePendingQueuesOnSessionChange = true;

    /// <summary>
    /// Present re-arm must dispose the prior outside-click watcher immediately; waiting for the
    /// ~1.5s arm Tick leaves stale HWND bounds active across handoffs.
    /// </summary>
    public const bool PresentMustStopOutsideClickBeforeRearm = true;

    /// <summary>
    /// SoftRefresh shares the patch coalescer; a rejected flush must schedule last-value retry
    /// (same as Patch), never drop the request.
    /// </summary>
    public const bool SoftRefreshMustScheduleDeferredLastValue = true;

    /// <summary>
    /// HideAll already posted to the UI thread must run Hide inline. Nested Hide Posts let a
    /// later Show land between CloseStackedSession and the delayed Hide callbacks.
    /// </summary>
    public const bool HideAllMustRunHideSynchronouslyOnUiThread = true;

    public static bool ShouldPostHideToUiThread(bool alreadyOnUiThread) =>
        !alreadyOnUiThread;

    /// <summary>
    /// True when Host must <c>CancelDeferredPatch</c> (and stop outside-click) before applying
    /// <paramref name="nextKind"/> / stack mode.
    /// </summary>
    public static bool MustInvalidatePendingWork(
        bool hasOpenSession,
        bool openStacked,
        bool nextStacked,
        string? openKind,
        string? nextKind,
        string? openStyle,
        string? nextStyle)
    {
        if (!PresentMustInvalidatePendingQueuesOnSessionChange || !hasOpenSession)
            return false;

        if (openStacked != nextStacked)
            return true;

        if (!string.Equals(openKind ?? "", nextKind ?? "", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.Equals(openStyle ?? "", nextStyle ?? "", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
