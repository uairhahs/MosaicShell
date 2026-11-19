
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>
    /// Per-module flyout session: Present/Patch/SoftRefresh routing, dismiss suppress, media identity.
    /// Capabilities build <see cref="FlyoutRequest"/> and call <see cref="Route"/>; Core owns the rest.
    /// </summary>
    public sealed class CapabilityFlyoutSession(string moduleId, IFlyoutPresenter presenter)
    {
        private readonly string _moduleId = moduleId;
        private readonly IFlyoutPresenter _presenter = presenter;
        private bool _suppressAutoPresent;
        private MediaSessionInfo? _lastMediaIdentity;
        private Timer? _presentSettle;
        private Action? _presentSettleAction;

        // The last kind and style actually handed to the presenter. A request that was suppressed never gets here.
        private string _lastDeliveredKind = "";
        private string? _lastDeliveredStyle;

        /// <summary>
        /// The kind the presenter has open: the session's own kind when it has one, else the last delivered kind
        /// while something is visible, else empty. It is not "last requested" (audit F18): a suppressed present or a
        /// dismissed flyout has no open kind.
        /// </summary>
        public string OpenKind => Open().Kind;

        public string? OpenStyle => Open().Style;

        public bool IsVisible => _presenter.IsVisible(_moduleId);

        public bool IsSessionActive => _presenter.GetSessionSnapshot(_moduleId).IsSessionActive;

        public TesseraFlyoutPhase Phase => _presenter.GetSessionSnapshot(_moduleId).Phase;

        internal void NotifyTransientDismissed()
        {
            _suppressAutoPresent = true;
        }

        internal void ResetSuppress()
        {
            _suppressAutoPresent = false;
        }

        /// <summary>
        /// Register a one-shot settle callback after cold media present (title/art catch-up).
        /// </summary>
        public void SetPresentSettleHandler(Action? handler)
        {
            _presentSettleAction = handler;
        }

        /// <summary>
        /// Route show/update/soft-refresh with platform policies applied.
        /// </summary>
        public void Route(
            FlyoutRequest request,
            FlyoutSyncTrigger trigger,
            string nextStyle,
            MediaSessionInfo? currentMedia = null)
        {
            bool trackBoundary = MediaFlyoutRouter.IsTrackBoundary(_lastMediaIdentity, currentMedia);
            if (FlyoutAutoPresentPolicy.IsUserIntentTrigger(trigger)
                || (trigger == FlyoutSyncTrigger.MediaSession && trackBoundary))
            {
                _suppressAutoPresent = false;
            }

            bool visible = _presenter.IsVisible(_moduleId);
            TesseraFlyoutSessionSnapshot snapshot = _presenter.GetSessionSnapshot(_moduleId);
            (string openKind, string? openStyle) = Open(snapshot, visible);
            bool sessionActive = snapshot.IsSessionActive;
            FlyoutSyncAction action = FlyoutRefreshPolicy.ResolvePresentation(
                sessionActive,
                openKind,
                request.Kind,
                openStyle,
                nextStyle);

            currentMedia ??= null;

            FlyoutTrace.Write(
                $"route mod={_moduleId} kind={request.Kind} trigger={trigger} action={action} "
                + $"phase={snapshot.Phase} sessionActive={sessionActive} visible={visible} "
                + $"boundary={trackBoundary} suppress={_suppressAutoPresent} "
                + $"openKind={(string.IsNullOrEmpty(openKind) ? "-" : openKind)} "
                + $"openStyle={openStyle ?? "-"} nextStyle={nextStyle}");

            if (action == FlyoutSyncAction.Present)
            {
                if (!FlyoutAutoPresentPolicy.ShouldColdPresent(_suppressAutoPresent, trigger, trackBoundary))
                {
                    FlyoutTrace.Write($"route -> suppressed (no cold present) kind={request.Kind}");
                    return;
                }

                FlyoutTrace.Write($"route -> Show kind={request.Kind}");
                _presenter.Show(request);
                Delivered(request, nextStyle);
            }
            else if (FlyoutDismissPolicy.ShouldResetAutoDismiss(trigger, _lastMediaIdentity, currentMedia))
            {
                FlyoutTrace.Write($"route -> Update kind={request.Kind}");
                _presenter.Update(request);
                Delivered(request, nextStyle);
                if (trigger == FlyoutSyncTrigger.MediaSession
                    && request.Kind.Equals("media", StringComparison.OrdinalIgnoreCase))
                {
                    _lastMediaIdentity = currentMedia;
                }
            }
            else
            {
                FlyoutTrace.Write($"route -> SoftRefresh kind={request.Kind}");
                _presenter.SoftRefresh(request);
                Delivered(request, nextStyle);
            }

            if (MediaPresentPolicy.ShouldSchedulePresentSettle(request.Kind, action))
            {
                SchedulePresentSettle();
            }

            if (action == FlyoutSyncAction.Present
                && request.Kind.Equals("media", StringComparison.OrdinalIgnoreCase))
            {
                _lastMediaIdentity = currentMedia;
            }
        }

        private (string Kind, string? Style) Open()
        {
            return Open(_presenter.GetSessionSnapshot(_moduleId), _presenter.IsVisible(_moduleId));
        }

        private (string Kind, string? Style) Open(TesseraFlyoutSessionSnapshot snapshot, bool visible)
        {
            return snapshot.Mode != TesseraFlyoutSessionMode.None
                ? (snapshot.Kind, snapshot.StyleId)
                : visible ? (_lastDeliveredKind, _lastDeliveredStyle) : ("", null);
        }

        private void Delivered(FlyoutRequest request, string nextStyle)
        {
            _lastDeliveredKind = request.Kind;
            _lastDeliveredStyle = nextStyle;
        }

        public void SoftRefresh(FlyoutRequest request)
        {
            _presenter.SoftRefresh(request);
        }

        public void Hide()
        {
            _presenter.Hide(_moduleId);
        }

        public void ClearMediaIdentity()
        {
            _lastMediaIdentity = null;
        }

        public bool ShouldColdPresentMedia(FlyoutSyncTrigger trigger, bool isTrackBoundary = false)
        {
            return FlyoutAutoPresentPolicy.ShouldColdPresent(_suppressAutoPresent, trigger, isTrackBoundary);
        }

        private void SchedulePresentSettle()
        {
            _presentSettle?.Dispose();
            int ms = MediaPresentPolicy.PresentSettleMs;
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
}
