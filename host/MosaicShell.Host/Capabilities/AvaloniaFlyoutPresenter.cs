using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;
using MosaicShell.Host.Tiles.Tessera;

namespace MosaicShell.Host.Capabilities
{
    public sealed class AvaloniaCapabilityUiBridge(IFlyoutPresenter flyouts, IHostUiBridge hostUi) : ICapabilityUiBridge
    {
        public IFlyoutPresenter Flyouts { get; } = flyouts;
        public IHostUiBridge HostUi { get; } = hostUi;

        public void RunOnHostThread(Action action)
        {
            Dispatcher.UIThread.Invoke(action);
        }
    }

    public sealed partial class AvaloniaFlyoutPresenter : IFlyoutPresenter
    {
        public event Action<string>? TransientDismissed;

        private readonly HostServices _services;
        private IHostUiBridge? _hostUi;
        private readonly Lock _gate = new();
        private readonly Dictionary<string, FlyoutWindow> _windows = new(StringComparer.OrdinalIgnoreCase);
        private FocusDimWindow? _focusDim;
        private TesseraOutsideClickWatcher? _outsideClick;
        private DispatcherTimer? _outsideClickArm;
        private readonly TesseraFlyoutLiveSyncCoalescer _patchCoalesce = new();
        private readonly TesseraFlyoutUpdateDispatchGate _updateDispatch = new();
        private readonly TesseraFlyoutSessionState _session = new();
        private readonly TesseraFlyoutIngressQueue _ingress = new();
        private DispatcherTimer? _deferredIngress;
        private bool _ingressFlushDeferred;

        public AvaloniaFlyoutPresenter(HostServices services, IHostUiBridge? hostUi = null)
        {
            _services = services;
            _hostUi = hostUi;
            Log($"presenter ctor build={typeof(AvaloniaFlyoutPresenter).Assembly.GetName().Version}");
        }

        public void AttachHostUi(IHostUiBridge hostUi)
        {
            _hostUi = hostUi;
        }

        public void Show(FlyoutRequest request)
        {
            Log($"Show queued kind={request.Kind} style={request.StyleId} thread={Environment.CurrentManagedThreadId}");
            EnqueueIngress(TesseraFlyoutIngressKind.Present, request, resetDismiss: true, immediate: IsImmediateStatusKind(request));
        }

        private static bool IsImmediateStatusKind(FlyoutRequest request)
        {
            return request.Kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
            || request.Kind.Equals("flight", StringComparison.OrdinalIgnoreCase);
        }

        public void Update(FlyoutRequest request)
        {
            Log($"Update queued kind={request.Kind} style={request.StyleId} thread={Environment.CurrentManagedThreadId}");
            EnqueueIngress(TesseraFlyoutIngressKind.Patch, request, resetDismiss: true, immediate: IsImmediateStatusKind(request));
        }

        private void EnqueueIngress(
            TesseraFlyoutIngressKind kind,
            FlyoutRequest request,
            bool resetDismiss,
            bool immediate)
        {
            TesseraFlyoutIngressWork work = new(kind, _session.Generation, request, resetDismiss);
            if (immediate)
            {
                Dispatcher.UIThread.Invoke(() => ExecuteIngressWork(work));
                return;
            }

            _ingress.Enqueue(work);
            if (_ingressFlushDeferred)
            {
                return;
            }

            if (_updateDispatch.TryEnqueue() != TesseraFlyoutUpdateDispatchKind.PostNow)
            {
                return;
            }

            Dispatcher.UIThread.Post(FlushIngress);
        }

        private void FlushIngress()
        {
            try
            {
                if (_ingressFlushDeferred)
                {
                    return;
                }

                TesseraFlyoutIngressWork? work = _ingress.Take(_session.Generation);
                if (work is null)
                {
                    return;
                }

                ExecuteIngressWork(work.Value);
            }
            finally
            {
                _updateDispatch.CompleteDispatch();
            }

            if (!_ingressFlushDeferred
                && _ingress.HasPending
                && _updateDispatch.TryEnqueue() == TesseraFlyoutUpdateDispatchKind.PostNow)
            {
                Dispatcher.UIThread.Post(FlushIngress);
            }
        }

        private void ExecuteIngressWork(TesseraFlyoutIngressWork work)
        {
            if (TesseraFlyoutIngressPolicy.IsStale(work.Generation, _session.Generation, work.Kind))
            {
                return;
            }

            switch (work.Kind)
            {
                case TesseraFlyoutIngressKind.SoftRefresh:
                    ApplySoftRefreshCore(work.Request);
                    break;
                case TesseraFlyoutIngressKind.Patch:
                    SafeShowOrUpdate(work.Request, work.ResetDismiss, allowLivePatch: true);
                    break;
                default:
                    SafeShowOrUpdate(work.Request, work.ResetDismiss, allowLivePatch: false);
                    break;
            }
        }

        public void SoftRefresh(FlyoutRequest request)
        {
            Log($"SoftRefresh queued kind={request.Kind} style={request.StyleId} gen={_session.Generation}");
            EnqueueIngress(TesseraFlyoutIngressKind.SoftRefresh, request, resetDismiss: false, immediate: false);
        }

        private void ApplySoftRefreshCore(FlyoutRequest request)
        {
            try
            {
                lock (_gate)
                {
                    if (ShouldUseStackedOsAcrylic(request))
                    {
                        string volumeKey = TesseraOsAcrylicStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Volume);
                        if (!_windows.TryGetValue(volumeKey, out FlyoutWindow? volumeWin)
                            || !volumeWin.IsFlyoutSessionShowing)
                        {
                            return;
                        }

                        if (!_patchCoalesce.TryBeginFlush(MonoNow(), out TimeSpan retryAfter))
                        {
                            DeferIngress(
                                new TesseraFlyoutIngressWork(
                                    TesseraFlyoutIngressKind.SoftRefresh,
                                    _session.Generation,
                                    request,
                                    ResetDismiss: false),
                                retryAfter);
                            return;
                        }

                        volumeWin.ApplyLiveOnly(request, _services);
                        return;
                    }

                    if (!_windows.TryGetValue(request.ModuleId, out FlyoutWindow? existing)
                        || !existing.IsFlyoutSessionShowing)
                    {
                        return;
                    }

                    if (!_patchCoalesce.TryBeginFlush(MonoNow(), out TimeSpan retryAfterSingle))
                    {
                        DeferIngress(
                            new TesseraFlyoutIngressWork(
                                TesseraFlyoutIngressKind.SoftRefresh,
                                _session.Generation,
                                request,
                                ResetDismiss: false),
                            retryAfterSingle);
                        return;
                    }

                    existing.ApplyLiveOnly(request, _services);
                }
            }
            catch (Exception ex) { Log($"soft refresh {ex}"); }
        }

        public void Hide(string moduleId)
        {
            if (TesseraFlyoutPresentHandoffPolicy.ShouldPostHideToUiThread(Dispatcher.UIThread.CheckAccess()))
            {
                Dispatcher.UIThread.Post(() => HideCore(moduleId));
                return;
            }

            HideCore(moduleId);
        }

        public void HideAll()
        {
            if (TesseraFlyoutPresentHandoffPolicy.ShouldPostHideToUiThread(Dispatcher.UIThread.CheckAccess()))
            {
                Dispatcher.UIThread.Post(HideAllCore);
                return;
            }

            HideAllCore();
        }

        private void HideCore(string moduleId)
        {
            if (moduleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
            {
                CloseStackedSession();
                FlyoutWindow? single;
                lock (_gate)
                {
                    _ = _windows.Remove(moduleId, out single);
                }
                try { single?.Close(); } catch { /* ignore */ }
                StopOutsideClickWatcher();
                CloseFocusDim();
                CancelDeferredPatch();
                _ = _session.Clear();
                return;
            }

            FlyoutWindow? w;
            lock (_gate)
            {
                if (!_windows.Remove(moduleId, out w))
                {
                    return;
                }
            }
            try { w.Close(); } catch { /* ignore */ }
        }

        private void HideAllCore()
        {
            CloseStackedSession();
            List<string> ids;
            lock (_gate)
            {
                ids = [.. _windows.Keys];
            }

            foreach (string id in ids)
            {
                HideCore(id);
            }

            StopOutsideClickWatcher();
            CloseFocusDim();
            CancelDeferredPatch();
            _ = _session.Clear();
        }

        public bool IsVisible(string moduleId)
        {
            if (moduleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase) && IsStackedTesseraVisible())
            {
                return true;
            }

            lock (_gate)
            {
                return _windows.TryGetValue(moduleId, out FlyoutWindow? w) && w.IsFlyoutSessionShowing;
            }
        }

        public TesseraFlyoutSessionSnapshot GetSessionSnapshot(string moduleId)
        {
            bool showing = IsVisible(moduleId);
            return !moduleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase)
                ? new(showing, 0, TesseraFlyoutSessionMode.None, "", null)
                : _session.Snapshot(showing);
        }

        private void SafeShowOrUpdate(FlyoutRequest request, bool resetDismiss = true, bool allowLivePatch = true)
        {
            try { ShowOrUpdateCore(request, resetDismiss, allowLivePatch); }
            catch (Exception ex)
            {
                Log($"EXCEPTION {ex}");
                CloseFocusDim();
            }
        }

        private static TimeSpan MonoNow()
        {
            return TimeSpan.FromMilliseconds(Environment.TickCount64);
        }

        private void ShowOrUpdateCore(FlyoutRequest request, bool resetDismiss = true, bool allowLivePatch = true)
        {
            InvalidateTesseraQueuesOnHandoff(request);

            if (ShouldUseStackedOsAcrylic(request))
            {
                ShowOrUpdateStacked(request, resetDismiss, allowLivePatch);
                return;
            }

            if (request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
            {
                CloseStackedSession();
            }

            ShowOrUpdateCoreSinglePath(request, resetDismiss, allowLivePatch);
        }

        private void InvalidateTesseraQueuesOnHandoff(FlyoutRequest request)
        {
            if (!request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            bool nextStacked = ShouldUseStackedOsAcrylic(request);
            string? openKind = null;
            string? openStyle = null;
            bool openStacked = _stackedSession is not null;
            bool hasOpen = openStacked;

            if (openStacked)
            {
                openKind = _stackedSession!.Kind;
                openStyle = _stackedSession.StyleId;
            }
            else
            {
                lock (_gate)
                {
                    if (_windows.TryGetValue("Tessera", out FlyoutWindow? single))
                    {
                        hasOpen = true;
                        openKind = single.Kind;
                        openStyle = single.StyleId;
                    }
                }
            }

            if (!TesseraFlyoutPresentHandoffPolicy.MustInvalidatePendingWork(
                    hasOpen, openStacked, nextStacked, openKind, request.Kind, openStyle, request.StyleId))
            {
                return;
            }

            Log($"handoff invalidate open={openKind}/{openStyle} stacked={openStacked} → {request.Kind}/{request.StyleId} stacked={nextStacked}");
            CancelDeferredPatch();
            if (TesseraFlyoutPresentHandoffPolicy.PresentMustStopOutsideClickBeforeRearm)
            {
                StopOutsideClickWatcher();
            }
        }

        private void ShowOrUpdateCoreSinglePath(FlyoutRequest request, bool resetDismiss = true, bool allowLivePatch = true)
        {
            Log($"ShowOrUpdateCore enter kind={request.Kind}");
            FlyoutWindow? reuse = null;
            bool reuseWasVisible = false;

            lock (_gate)
            {
                if (_windows.TryGetValue(request.ModuleId, out FlyoutWindow? existing)
                    && TesseraFlyoutLiveSyncPolicy.MustReuseRegisteredFlyoutHwnd)
                {
                    reuse = existing;
                    reuseWasVisible = existing.IsFlyoutSessionShowing;

                    if (reuseWasVisible
                        && allowLivePatch
                        && string.Equals(existing.Kind, request.Kind, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(existing.StyleId ?? "", request.StyleId ?? "", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!_patchCoalesce.TryBeginFlush(MonoNow(), out TimeSpan retryAfter))
                        {
                            DeferIngress(
                                new TesseraFlyoutIngressWork(
                                    TesseraFlyoutIngressKind.Patch,
                                    _session.Generation,
                                    request,
                                    resetDismiss),
                                retryAfter);
                            return;
                        }

                        if (TryPatchLive(existing, request, resetDismiss))
                        {
                            return;
                        }

                        Log($"live-apply missed kind={request.Kind} style={request.StyleId}, rebuilding");
                    }
                }
                else if (_windows.TryGetValue(request.ModuleId, out FlyoutWindow? old))
                {
                    try { old.Close(); } catch { /* ignore */ }
                    _ = _windows.Remove(request.ModuleId);
                }
            }

            if (reuse is not null)
            {
                if (TesseraStatusFlyoutPolicy.MustRecreateHwndAfterMediaShellKind(reuse.Kind, request.Kind))
                {
                    Log($"status after media shell: recreate HWND open={reuse.Kind} next={request.Kind}");
                    reuse.TransientDismissed -= OnFlyoutTransientDismissed;
                    reuse.SuppressAutoDismiss();
                    try { reuse.Close(); } catch { /* ignore */ }
                    lock (_gate)
                    {
                        _ = _windows.Remove(request.ModuleId);
                    }

                    reuse = null;
                }
            }

            if (reuse is not null)
            {
                if (!reuseWasVisible
                    && string.Equals(reuse.Kind, request.Kind, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(reuse.StyleId ?? "", request.StyleId ?? "", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryPatchLive(reuse, request, resetDismiss))
                    {
                        if (!reuse.IsVisible)
                        {
                            reuse.Show();
                        }

                        Log($"revive kind={request.Kind} style={request.StyleId}");
                        reuse.PlayShowAnimation();
                        return;
                    }
                }

                Control reusedContent;
                try { reusedContent = BuildContent(request, reuseWasVisible); }
                catch (Exception ex)
                {
                    Log($"BuildContent failed on reuse, using fallback: {ex}");
                    reusedContent = BuildFallbackContent(request, ex.Message);
                }

                reuse.ApplyRequest(request, reusedContent);
                WireTesseraSession(reuse);
                EnsureTesseraSession(request, TesseraFlyoutSessionMode.Single);
                if (!reuse.IsVisible)
                {
                    reuse.Show();
                }

                reuse.EnsureLivePump();
                PresentFlyout(reuse, request);
                if (TesseraFlyoutLiveSyncPolicy.ShouldPlayShowAnimationAfterApplyRequest(
                        reuseWasVisible, TesseraFlyoutWindowPolicy.HideUntilCompositionReady))
                {
                    reuse.PlayShowAnimation();
                }

                return;
            }

            Control content;
            try
            {
                content = BuildContent(request);
            }
            catch (Exception ex)
            {
                Log($"BuildContent failed, using fallback: {ex}");
                content = BuildFallbackContent(request, ex.Message);
                Log("FALLBACK content, Tessera live session unsuccessful");
            }

            if (request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
            {
                bool hasHost = content is Control c && TesseraLiveHost.FindIn(c) is not null;
                bool isFallback = content is Border { Child: TextBlock };
                if (!TesseraFlyoutLiveSyncPolicy.IsSuccessfulTesseraContent(hasHost, isFallback))
                {
                    Log($"live session unsuccessful hasLiveHost={hasHost} fallback={isFallback}");
                }
            }

            FlyoutWindow window = new(request, content, _services);
            WireTesseraSession(window);
            window.Closed += (_, _) =>
            {
                lock (_gate)
                {
                    // ClosedMustOnlyUnregisterSameInstance: a superseded HWND must not clear the live session.
                    if (_windows.TryGetValue(request.ModuleId, out FlyoutWindow? current)
                        && ReferenceEquals(current, window))
                    {
                        _ = _windows.Remove(request.ModuleId);
                    }
                }
                if (request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase)
                    && !IsVisible("Tessera"))
                {
                    StopOutsideClickWatcher();
                    CloseFocusDim();
                    CancelDeferredPatch();
                }
            };
            lock (_gate)
            {
                _windows[request.ModuleId] = window;
            }

            EnsureTesseraSession(request, TesseraFlyoutSessionMode.Single);

            // Two unowned Topmost windows: the one shown LAST usually wins Z-order on Win32.
            // Show FocusDim first (if enabled), then the flyout, then HWND-stack as belt-and-suspenders.
            SyncFocusDim(request);

            // Consolidation (60e883e) used unowned Show(). Show(owner) from 83a9e57 made the
            // flyout lose Z-order to unowned FocusDim and often paint as an empty Transparent HWND.
            // SoftFrost: Show at Opacity 0, layout, then reveal, avoids black composition-clear flash.
            window.Show();

            window.EnsureLivePump();
            PresentFlyout(window, request);
            window.PlayShowAnimation();
        }

        private bool TryPatchLive(FlyoutWindow existing, FlyoutRequest request, bool resetDismiss)
        {
            if (!existing.TryApplyLive(request, _services, resetDismiss))
            {
                return false;
            }

            Log($"patch kind={request.Kind} style={request.StyleId}");
            // (PatchImpliesPresent|Win32Restack|OutsideClickRearm are false, Core tests).
            existing.EnsureLivePump();
            if (TesseraFlyoutOutsideClickPolicy.PatchMustRefreshBoundsWithoutRearm)
            {
                _outsideClick?.RefreshBounds(existing);
            }

            return true;
        }

        private void EnsureTesseraSession(FlyoutRequest request, TesseraFlyoutSessionMode mode)
        {
            if (!request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (_session.Mode == mode
                && string.Equals(_session.Kind, request.Kind, StringComparison.OrdinalIgnoreCase)
                && string.Equals(_session.StyleId ?? "", request.StyleId ?? "", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _ = _session.Begin(mode, request.Kind, request.StyleId);
        }

        private void DeferIngress(TesseraFlyoutIngressWork work, TimeSpan delay)
        {
            if (work.Kind == TesseraFlyoutIngressKind.SoftRefresh
                && !TesseraFlyoutLiveSyncPolicy.SoftRefreshMustScheduleDeferredLastValue)
            {
                return;
            }

            _ingress.Enqueue(work);
            if (delay <= TimeSpan.Zero)
            {
                delay = TesseraFlyoutLiveSyncPolicy.MinFlushInterval;
            }

            _ingressFlushDeferred = true;
            _deferredIngress?.Stop();
            _deferredIngress = new DispatcherTimer { Interval = delay };
            _deferredIngress.Tick += (_, _) =>
            {
                _deferredIngress?.Stop();
                _deferredIngress = null;
                _ingressFlushDeferred = false;
                if (_updateDispatch.TryEnqueue() == TesseraFlyoutUpdateDispatchKind.PostNow)
                {
                    Dispatcher.UIThread.Post(FlushIngress);
                }
            };
            _deferredIngress.Start();
        }

        private void CancelDeferredPatch()
        {
            _deferredIngress?.Stop();
            _deferredIngress = null;
            _ingressFlushDeferred = false;
            _ingress.CancelAll();
            _updateDispatch.CompleteDispatch();
        }

        private void PresentFlyout(FlyoutWindow window, FlyoutRequest request)
        {
            if (TesseraStatusFlyoutPolicy.IsStatusKind(request.Kind))
            {
                window.BackdropCornerRadiusDip =
                    TesseraStatusFlyoutPolicy.ResolveChipCornerRadiusDip(request.StyleId);
            }
            else if (window.ClusterOriginX is null)
            {
                // Single-shell volume/media acrylic: process-wide radius; clear per-window override.
                window.BackdropCornerRadiusDip = TesseraOsAcrylicTrialPolicy.SpikeCornerRadius;
            }

            window.FinishLayout();
            // Keep dim in sync on live updates; first Show already called SyncFocusDim.
            SyncFocusDim(request);
            RestackAboveDim(window);

            bool focusDimOn = TesseraFocusDimPolicy.EnabledFromPayload(request.Payload);
            string layered = TesseraFlyoutWindowPolicy.MustApplyPresentableLayeredAlpha
                ? Win32WindowChrome.ForceOpaqueLayer(window)
                : "skipped";
            Screen? screen = window.Screens?.ScreenFromWindow(window)
                         ?? window.Screens?.ScreenFromPoint(window.Position);
            Log(
                $"presented kind={request.Kind} style={request.StyleId} visible={window.IsVisible} " +
                $"bounds={window.Bounds.Width:0}x{window.Bounds.Height:0} " +
                $"desired={window.DesiredSize.Width:0}x{window.DesiredSize.Height:0} " +
                $"pos={window.Position} mon={request.MonitorIndex} focusDim={focusDimOn} " +
                $"screen=({screen?.WorkingArea.X},{screen?.WorkingArea.Y} {screen?.WorkingArea.Width}x{screen?.WorkingArea.Height}) " +
                $"scaling={window.RenderScaling:0.##} " +
                $"hint={string.Join('|', window.TransparencyLevelHint)} " +
                $"actual={window.ActualTransparencyLevel} layered={layered}");

            if (request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
            {
                ScheduleOutsideClickArm(window);
            }
        }

        private void ScheduleOutsideClickArm(FlyoutWindow window)
        {
            // Present owns the hook: dispose prior watcher before the arm delay (stale bounds otherwise).
            if (TesseraFlyoutOutsideClickPolicy.PresentMustStopPriorWatcherBeforeRearm)
            {
                StopOutsideClickWatcher();
            }
            else
            {
                _outsideClickArm?.Stop();
            }

            _outsideClickArm = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
            FlyoutWindow captured = window;
            _outsideClickArm.Tick += (_, _) =>
            {
                _outsideClickArm?.Stop();
                _outsideClickArm = null;
                if (captured.IsVisible)
                {
                    StartOutsideClickWatcher(captured);
                    Log("outside-click armed");
                }
            };
            _outsideClickArm.Start();
        }

        private void RestackAboveDim(FlyoutWindow flyout)
        {
            void Stack(string when)
            {
                try
                {
                    flyout.Topmost = true;
                    _focusDim?.Topmost = true;

                    bool ok = Win32WindowChrome.TryStackAbove(flyout, _focusDim, out string? detail);
                    Log($"z-order {when} ok={ok} {detail}");
                }
                catch (Exception ex)
                {
                    Log($"z-order {when} EXCEPTION {ex}");
                }
            }

            Stack("immediate");
            // Dim click-through / HWND often lands at Loaded, restack then and once more at Input.
            Dispatcher.UIThread.Post(() => Stack("loaded"), DispatcherPriority.Loaded);
            Dispatcher.UIThread.Post(() => Stack("input"), DispatcherPriority.Input);
        }

        private void SyncFocusDim(FlyoutRequest request)
        {
            if (!request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase)
                || !TesseraFocusDimPolicy.EnabledFromPayload(request.Payload))
            {
                CloseFocusDim();
                return;
            }

            if (_focusDim is null)
            {
                _focusDim = new FocusDimWindow(request.MonitorIndex);
                // Show dim BEFORE the flyout (caller order) so the flyout is the later topmost peer.
                _focusDim.Show();
                _focusDim.FadeIn();
                Log($"focusDim shown mon={request.MonitorIndex}");
            }
            else
            {
                _focusDim.PlaceOnMonitor(request.MonitorIndex);
            }
        }

        private void CloseFocusDim()
        {
            FocusDimWindow? dim = _focusDim;
            _focusDim = null;
            if (dim is null)
            {
                return;
            }

            try { dim.InstantClose(); }
            catch
            {
                try { dim.Close(); } catch { /* ignore */ }
            }
        }

        private void StartOutsideClickWatcher(FlyoutWindow flyout)
        {
            StopOutsideClickWatcher();
            _outsideClick = new TesseraOutsideClickWatcher(flyout, DismissTesseraImmediate);
            _outsideClick.Start();
        }

        private void StopOutsideClickWatcher()
        {
            _outsideClickArm?.Stop();
            _outsideClickArm = null;
            _outsideClick?.Dispose();
            _outsideClick = null;
        }

        private void DismissTesseraImmediate()
        {
            Log("outside-click dismiss");
            if (_stackedSession is not null)
            {
                DismissStackedTesseraImmediate();
                return;
            }

            DismissTesseraSingleImmediate();
        }

        private void WireTesseraSession(FlyoutWindow window)
        {
            window.TransientDismissed -= OnFlyoutTransientDismissed;
            window.TransientDismissed += OnFlyoutTransientDismissed;
        }

        private void OnFlyoutTransientDismissed(string moduleId)
        {
            if (_stackedSession is not null
                && TesseraOsAcrylicStackedPolicy.ShouldCascadeTransientDismissToStackedSession(
                    moduleId, _stackedSession.SlotKeys))
            {
                lock (_gate)
                {
                    foreach (string slotKey in _stackedSession.SlotKeys)
                    {
                        if (_windows.TryGetValue(slotKey, out FlyoutWindow? window)
                            && window.IsFlyoutSessionShowing)
                        {
                            window.TransientDismissWithoutNotify();
                        }
                    }
                }

                StopStackedAutoDismiss();
            }
            else if (_stackedSession is not null
                     && TesseraOsAcrylicStackedPolicy.TransientDismissMustHideAllSlots)
            {
                // Superseded single-shell CapsLock Tick: ignore; do not touch live volume/media.
                Log($"ignore superseded transient-dismiss key={moduleId}");
                return;
            }

            try
            {
                TransientDismissed?.Invoke(
                    TesseraOsAcrylicStackedPolicy.ResolveTransientDismissConsumerKey(moduleId));
            }
            catch { /* ignore */ }

            if (!TesseraFocusDimPolicy.ShouldCloseFocusDimOnTransientDismiss())
            {
                return;
            }

            Log("transient-dismiss, closing focusDim");
            StopOutsideClickWatcher();
            CloseFocusDim();
        }

        private Control BuildContent(FlyoutRequest request, bool sessionAlreadyShowing = false)
        {
            if (request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
            {
                TesseraFlyoutMaterial material = TesseraFlyoutMaterialFactory.FromPayload(
                    request.Payload, request.StyleId, request.Kind);
                TesseraPalette.ApplyMaterial(material);

                bool osAcrylicEligible = TesseraFlyoutMaterialFactory.OsAcrylicEligibleFromPayload(
                    request.Payload, request.StyleId, request.Kind);
                bool settingsWantBlur = TesseraFlyoutRequestBuilder.BackdropBlurFromPayload(request.Payload);
                TesseraFlyoutGlassBinder.Binding glass = TesseraFlyoutGlassBinder.ApplyForLiveFlyout(settingsWantBlur, osAcrylicEligible);
                Log(
                    $"glass mode={glass.Mode} backdrop={glass.UseBackdropBlur} " +
                    $"embeddedPreview={glass.UseEmbeddedPreview} softFrostHwnd={glass.SoftFrostHwndReady} " +
                    $"osAcrylic={glass.OsAcrylicEligible}");

                TesseraFlyoutViewModel vm = TesseraFlyoutViewModel.FromRequest(_services, request, _hostUi);
                Control root = TesseraStyleFactory.Create(
                    request.StyleId ?? "Fluent",
                    vm,
                    accentColor: TesseraFlyoutRequestBuilder.AccentFromPayload(request.Payload),
                    embeddedPreview: glass.UseEmbeddedPreview,
                    sessionAlreadyShowing: sessionAlreadyShowing);
                double scale = TesseraFlyoutRequestBuilder.FlyoutScaleFromPayload(request.Payload);
                if (Math.Abs(scale - 1.0) > 0.01)
                {
                    root = new LayoutTransformControl
                    {
                        LayoutTransform = new ScaleTransform(scale, scale),
                        Child = root
                    };
                }

                return TesseraChrome.WrapFlyoutContent(root);
            }

            return BuildFallbackContent(request, null);
        }

        private static Control BuildFallbackContent(FlyoutRequest request, string? error)
        {
            return new Border
            {
                MinWidth = 220,
                MinHeight = 80,
                Background = new SolidColorBrush(Color.FromArgb(250, 0x11, 0x11, 0x1b)),
                BorderBrush = new SolidColorBrush(Color.Parse("#89dceb")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20),
                Child = new TextBlock
                {
                    Text = error is null
                        ? $"{request.ModuleId} · {request.Kind}"
                        : $"Flyout error\n{error}",
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap
                }
            };
        }

        private static void Log(string message)
        {
            TesseraFlyoutDiagnostics.Log(message);
        }
    }
}
