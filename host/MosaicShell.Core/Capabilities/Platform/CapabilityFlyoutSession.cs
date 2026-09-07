
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

        public string OpenKind { get; private set; } = "";
        public string? OpenStyle { get; private set; }

        public bool IsVisible => _presenter.IsVisible(_moduleId);

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
            bool enableMediaFlyouts,
            string nextStyle,
            MediaSessionInfo? currentMedia = null)
        {
            bool trackBoundary = MediaFlyoutRouter.IsTrackBoundary(_lastMediaIdentity, currentMedia);
            if (FlyoutAutoPresentPolicy.IsUserIntentTrigger(trigger)
                || (trigger == FlyoutSyncTrigger.MediaSession && trackBoundary))
            {
                _suppressAutoPresent = false;
            }

            string openKind = OpenKind;
            string? openStyle = OpenStyle;
            OpenKind = request.Kind;
            OpenStyle = nextStyle;

            bool visible = _presenter.IsVisible(_moduleId);
            FlyoutSyncAction action = FlyoutRefreshPolicy.ResolvePresentation(
                trigger,
                visible,
                openKind,
                request.Kind,
                openStyle,
                nextStyle,
                enableMediaFlyouts);

            currentMedia ??= null;

            FlyoutTrace.Write(
                $"route mod={_moduleId} kind={request.Kind} trigger={trigger} action={action} "
                + $"visible={visible} boundary={trackBoundary} suppress={_suppressAutoPresent} "
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
            }
            else if (FlyoutDismissPolicy.ShouldResetAutoDismiss(trigger, _lastMediaIdentity, currentMedia))
            {
                FlyoutTrace.Write($"route -> Update kind={request.Kind}");
                _presenter.Update(request);
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
