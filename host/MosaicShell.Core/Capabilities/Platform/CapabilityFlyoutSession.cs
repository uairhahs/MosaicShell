namespace MosaicShell.Core.Capabilities.Platform;

using MosaicShell.Core.Services;

/// <summary>
/// Per-module flyout session: Present/Patch/SoftRefresh routing, dismiss suppress, media identity.
/// Capabilities build <see cref="FlyoutRequest"/> and call <see cref="Route"/>; Core owns the rest.
/// </summary>
public sealed class CapabilityFlyoutSession
{
    private readonly string _moduleId;
    private readonly IFlyoutPresenter _presenter;
    private bool _suppressAutoPresent;
    private MediaSessionInfo? _lastMediaIdentity;
    private Timer? _presentSettle;
    private Action? _presentSettleAction;

    public CapabilityFlyoutSession(string moduleId, IFlyoutPresenter presenter)
    {
        _moduleId = moduleId;
        _presenter = presenter;
    }

    public string OpenKind { get; private set; } = "";
    public string? OpenStyle { get; private set; }

    public bool IsVisible => _presenter.IsVisible(_moduleId);

    internal void NotifyTransientDismissed() => _suppressAutoPresent = true;

    internal void ResetSuppress() => _suppressAutoPresent = false;

    /// <summary>
    /// Register a one-shot settle callback after cold media present (title/art catch-up).
    /// </summary>
    public void SetPresentSettleHandler(Action? handler) => _presentSettleAction = handler;

    /// <summary>
    /// Route show/update/soft-refresh with platform policies applied.
    /// </summary>
    public void Route(
        FlyoutRequest request,
        FlyoutSyncTrigger trigger,
        bool enableMediaFlyouts,
        string nextStyle,
        MediaSessionInfo? currentMedia = null)
    {
        if (FlyoutAutoPresentPolicy.IsUserIntentTrigger(trigger))
            _suppressAutoPresent = false;

        var openKind = OpenKind;
        var openStyle = OpenStyle;
        OpenKind = request.Kind;
        OpenStyle = nextStyle;

        var visible = _presenter.IsVisible(_moduleId);
        var action = FlyoutRefreshPolicy.ResolvePresentation(
            trigger,
            visible,
            openKind,
            request.Kind,
            openStyle,
            nextStyle,
            enableMediaFlyouts);

        currentMedia ??= null;

        if (action == FlyoutSyncAction.Present)
        {
            if (!FlyoutAutoPresentPolicy.ShouldColdPresent(_suppressAutoPresent, trigger))
                return;
            _presenter.Show(request);
        }
        else if (FlyoutDismissPolicy.ShouldResetAutoDismiss(trigger, _lastMediaIdentity, currentMedia))
        {
            _presenter.Update(request);
            if (trigger == FlyoutSyncTrigger.MediaSession
                && request.Kind.Equals("media", StringComparison.OrdinalIgnoreCase))
                _lastMediaIdentity = currentMedia;
        }
        else
            _presenter.SoftRefresh(request);

        if (MediaPresentPolicy.ShouldSchedulePresentSettle(request.Kind, action))
            SchedulePresentSettle();

        if (action == FlyoutSyncAction.Present
            && request.Kind.Equals("media", StringComparison.OrdinalIgnoreCase))
            _lastMediaIdentity = currentMedia;
    }

    public void SoftRefresh(FlyoutRequest request) => _presenter.SoftRefresh(request);

    public void Hide() => _presenter.Hide(_moduleId);

    public void ClearMediaIdentity() => _lastMediaIdentity = null;

    public bool ShouldColdPresentMedia(FlyoutSyncTrigger trigger) =>
        FlyoutAutoPresentPolicy.ShouldColdPresent(_suppressAutoPresent, trigger);

    private void SchedulePresentSettle()
    {
        _presentSettle?.Dispose();
        var ms = MediaPresentPolicy.PresentSettleMs;
        _presentSettle = new Timer(_ =>
        {
            try { _presentSettleAction?.Invoke(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CapabilityFlyoutSession settle] {ex}");
            }
        }, null, ms, Timeout.Infinite);
    }

    public void Dispose()
    {
        _presentSettle?.Dispose();
        _presentSettle = null;
    }
}
