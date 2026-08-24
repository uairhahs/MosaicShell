using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;
using MosaicShell.Host.Tiles.Tessera;

namespace MosaicShell.Host.Capabilities;

public sealed class AvaloniaCapabilityUiBridge : ICapabilityUiBridge
{
    public AvaloniaCapabilityUiBridge(IFlyoutPresenter flyouts, IHostUiBridge hostUi)
    {
        Flyouts = flyouts;
        HostUi = hostUi;
    }

    public IFlyoutPresenter Flyouts { get; }
    public IHostUiBridge HostUi { get; }

    public void RunOnHostThread(Action action) =>
        Dispatcher.UIThread.Invoke(action);
}

public sealed partial class AvaloniaFlyoutPresenter : IFlyoutPresenter
{
    public event Action<string>? TransientDismissed;

    private readonly HostServices _services;
    private IHostUiBridge? _hostUi;
    private readonly object _gate = new();
    private readonly Dictionary<string, FlyoutWindow> _windows = new(StringComparer.OrdinalIgnoreCase);
    private FocusDimWindow? _focusDim;
    private TesseraOutsideClickWatcher? _outsideClick;
    private DispatcherTimer? _outsideClickArm;
    private readonly TesseraFlyoutLiveSyncCoalescer _patchCoalesce = new();
    private readonly TesseraFlyoutUpdateDispatchGate _updateDispatch = new();
    private DispatcherTimer? _deferredPatch;
    private FlyoutRequest? _pendingPatch;
    private FlyoutRequest? _pendingUpdate;
    private FlyoutRequest? _pendingSoftRefresh;
    private bool _pendingResetDismiss;
    private DispatcherTimer? _deferredSoftRefresh;

    public AvaloniaFlyoutPresenter(HostServices services, IHostUiBridge? hostUi = null)
    {
        _services = services;
        _hostUi = hostUi;
        Log($"presenter ctor build={typeof(AvaloniaFlyoutPresenter).Assembly.GetName().Version}");
    }

    public void AttachHostUi(IHostUiBridge hostUi) => _hostUi = hostUi;

    public void Show(FlyoutRequest request)
    {
        Log($"Show queued kind={request.Kind} style={request.StyleId} thread={Environment.CurrentManagedThreadId}");
        if (IsImmediateStatusKind(request))
            Dispatcher.UIThread.Invoke(() => SafeShowOrUpdate(request, resetDismiss: true));
        else
            Dispatcher.UIThread.Post(() => SafeShowOrUpdate(request, resetDismiss: true));
    }

    private static bool IsImmediateStatusKind(FlyoutRequest request) =>
        request.Kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
        || request.Kind.Equals("flight", StringComparison.OrdinalIgnoreCase);

    public void Update(FlyoutRequest request)
    {
        Log($"Update queued kind={request.Kind} style={request.StyleId} thread={Environment.CurrentManagedThreadId}");
        if (IsImmediateStatusKind(request))
        {
            Dispatcher.UIThread.Invoke(() => SafeShowOrUpdate(request, resetDismiss: true));
            return;
        }

        _pendingUpdate = request;
        _pendingResetDismiss = true;
        if (_updateDispatch.TryEnqueue() != TesseraFlyoutUpdateDispatchKind.PostNow)
            return;

        Dispatcher.UIThread.Post(FlushPendingUpdate);
    }

    private void FlushPendingUpdate()
    {
        if (_pendingUpdate is not { } pending)
        {
            _updateDispatch.CompleteDispatch();
            return;
        }

        var reset = _pendingResetDismiss;
        _pendingUpdate = null;
        try
        {
            SafeShowOrUpdate(pending, reset);
        }
        finally
        {
            _updateDispatch.CompleteDispatch();
        }

        if (_pendingUpdate is not null
            && _updateDispatch.TryEnqueue() == TesseraFlyoutUpdateDispatchKind.PostNow)
            Dispatcher.UIThread.Post(FlushPendingUpdate);
    }

    public void SoftRefresh(FlyoutRequest request) =>
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                lock (_gate)
                {
                    if (ShouldUseStackedOsAcrylic(request))
                    {
                        var volumeKey = TesseraOsAcrylicStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Volume);
                        if (!_windows.TryGetValue(volumeKey, out var volumeWin)
                            || !volumeWin.IsFlyoutSessionShowing)
                            return;

                        if (!_patchCoalesce.TryBeginFlush(MonoNow(), out var retryAfter))
                        {
                            if (TesseraFlyoutLiveSyncPolicy.SoftRefreshMustScheduleDeferredLastValue)
                            {
                                _pendingSoftRefresh = request;
                                ScheduleDeferredSoftRefresh(retryAfter);
                            }
                            return;
                        }

                        volumeWin.ApplyLiveOnly(request, _services);
                        return;
                    }

                    if (!_windows.TryGetValue(request.ModuleId, out var existing)
                        || !existing.IsFlyoutSessionShowing)
                        return;

                    if (!_patchCoalesce.TryBeginFlush(MonoNow(), out var retryAfterSingle))
                    {
                        if (TesseraFlyoutLiveSyncPolicy.SoftRefreshMustScheduleDeferredLastValue)
                        {
                            _pendingSoftRefresh = request;
                            ScheduleDeferredSoftRefresh(retryAfterSingle);
                        }
                        return;
                    }

                    existing.ApplyLiveOnly(request, _services);
                }
            }
            catch (Exception ex) { Log($"soft refresh {ex}"); }
        });

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
                _windows.Remove(moduleId, out single);
            }
            try { single?.Close(); } catch { /* ignore */ }
            StopOutsideClickWatcher();
            CloseFocusDim();
            CancelDeferredPatch();
            return;
        }

        FlyoutWindow? w;
        lock (_gate)
        {
            if (!_windows.Remove(moduleId, out w)) return;
        }
        try { w.Close(); } catch { /* ignore */ }
    }

    private void HideAllCore()
    {
        CloseStackedSession();
        List<string> ids;
        lock (_gate) ids = _windows.Keys.ToList();
        foreach (var id in ids)
            HideCore(id);
        StopOutsideClickWatcher();
        CloseFocusDim();
        CancelDeferredPatch();
    }

    public bool IsVisible(string moduleId)
    {
        if (moduleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase) && IsStackedTesseraVisible())
            return true;

        lock (_gate)
            return _windows.TryGetValue(moduleId, out var w) && w.IsFlyoutSessionShowing;
    }

    private void SafeShowOrUpdate(FlyoutRequest request, bool resetDismiss = true)
    {
        try { ShowOrUpdateCore(request, resetDismiss); }
        catch (Exception ex)
        {
            Log($"EXCEPTION {ex}");
            CloseFocusDim();
        }
    }

    private static TimeSpan MonoNow() => TimeSpan.FromMilliseconds(Environment.TickCount64);

    private void ShowOrUpdateCore(FlyoutRequest request, bool resetDismiss = true)
    {
        InvalidateTesseraQueuesOnHandoff(request);

        if (ShouldUseStackedOsAcrylic(request))
        {
            ShowOrUpdateStacked(request, resetDismiss);
            return;
        }

        if (request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
            CloseStackedSession();

        ShowOrUpdateCoreSinglePath(request, resetDismiss);
    }

    private void InvalidateTesseraQueuesOnHandoff(FlyoutRequest request)
    {
        if (!request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
            return;

        var nextStacked = ShouldUseStackedOsAcrylic(request);
        string? openKind = null;
        string? openStyle = null;
        var openStacked = _stackedSession is not null;
        var hasOpen = openStacked;

        if (openStacked)
        {
            openKind = _stackedSession!.Kind;
            openStyle = _stackedSession.StyleId;
        }
        else
        {
            lock (_gate)
            {
                if (_windows.TryGetValue("Tessera", out var single))
                {
                    hasOpen = true;
                    openKind = single.Kind;
                    openStyle = single.StyleId;
                }
            }
        }

        if (!TesseraFlyoutPresentHandoffPolicy.MustInvalidatePendingWork(
                hasOpen, openStacked, nextStacked, openKind, request.Kind, openStyle, request.StyleId))
            return;

        Log($"handoff invalidate open={openKind}/{openStyle} stacked={openStacked} → {request.Kind}/{request.StyleId} stacked={nextStacked}");
        CancelDeferredPatch();
        if (TesseraFlyoutPresentHandoffPolicy.PresentMustStopOutsideClickBeforeRearm)
            StopOutsideClickWatcher();
    }

    private void ShowOrUpdateCoreSinglePath(FlyoutRequest request, bool resetDismiss = true)
    {
        Log($"ShowOrUpdateCore enter kind={request.Kind}");
        FlyoutWindow? reuse = null;
        var reuseWasVisible = false;

        lock (_gate)
        {
            if (_windows.TryGetValue(request.ModuleId, out var existing)
                && TesseraFlyoutLiveSyncPolicy.MustReuseRegisteredFlyoutHwnd)
            {
                reuse = existing;
                reuseWasVisible = existing.IsFlyoutSessionShowing;

                if (reuseWasVisible)
                {
                    var action = TesseraFlyoutLiveSyncPolicy.ResolveAction(
                        isVisible: true,
                        openKind: existing.Kind,
                        nextKind: request.Kind,
                        openStyle: existing.StyleId,
                        nextStyle: request.StyleId);

                    if (action == TesseraFlyoutSyncAction.Patch)
                    {
                        if (!_patchCoalesce.TryBeginFlush(MonoNow(), out var retryAfter))
                        {
                            _pendingPatch = request;
                            _pendingResetDismiss = resetDismiss;
                            ScheduleDeferredPatch(retryAfter);
                            return;
                        }

                        if (TryPatchLive(existing, request, resetDismiss))
                            return;

                        Log($"live-apply missed kind={request.Kind} style={request.StyleId}, rebuilding");
                    }
                }
            }
            else if (_windows.TryGetValue(request.ModuleId, out var old))
            {
                try { old.Close(); } catch { /* ignore */ }
                _windows.Remove(request.ModuleId);
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
                lock (_gate) _windows.Remove(request.ModuleId);
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
                        reuse.Show();
                    Log($"revive kind={request.Kind} style={request.StyleId}");
                    reuse.PlayShowAnimation();
                    return;
                }
            }

            Control reusedContent;
            try { reusedContent = BuildContent(request); }
            catch (Exception ex)
            {
                Log($"BuildContent failed on reuse, using fallback: {ex}");
                reusedContent = BuildFallbackContent(request, ex.Message);
            }

            reuse.ApplyRequest(request, reusedContent);
            WireTesseraSession(reuse);
            if (!reuse.IsVisible)
                reuse.Show();
            reuse.EnsureLivePump();
            PresentFlyout(reuse, request);
            if (TesseraFlyoutLiveSyncPolicy.ShouldPlayShowAnimationAfterApplyRequest(
                    reuseWasVisible, TesseraFlyoutWindowPolicy.HideUntilCompositionReady))
                reuse.PlayShowAnimation();
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
            var hasHost = content is Control c && TesseraLiveHost.FindIn(c) is not null;
            var isFallback = content is Border { Child: TextBlock };
            if (!TesseraFlyoutLiveSyncPolicy.IsSuccessfulTesseraContent(hasHost, isFallback))
                Log($"live session unsuccessful hasLiveHost={hasHost} fallback={isFallback}");
        }

        var window = new FlyoutWindow(request, content, _services);
        WireTesseraSession(window);
        window.Closed += (_, _) =>
        {
            lock (_gate)
            {
                // ClosedMustOnlyUnregisterSameInstance: a superseded HWND must not clear the live session.
                if (_windows.TryGetValue(request.ModuleId, out var current)
                    && ReferenceEquals(current, window))
                    _windows.Remove(request.ModuleId);
            }
            if (request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase)
                && !IsVisible("Tessera"))
            {
                StopOutsideClickWatcher();
                CloseFocusDim();
                CancelDeferredPatch();
            }
        };
        lock (_gate) _windows[request.ModuleId] = window;

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
            return false;

        Log($"patch kind={request.Kind} style={request.StyleId}");
        // (PatchImpliesPresent|Win32Restack|OutsideClickRearm are false, Core tests).
        existing.EnsureLivePump();
        if (TesseraFlyoutOutsideClickPolicy.PatchMustRefreshBoundsWithoutRearm)
            _outsideClick?.RefreshBounds(existing);
        return true;
    }

    private void ScheduleDeferredPatch(TimeSpan delay)
    {
        if (delay <= TimeSpan.Zero)
            delay = TesseraFlyoutLiveSyncPolicy.MinFlushInterval;

        _deferredPatch?.Stop();
        _deferredPatch = new DispatcherTimer { Interval = delay };
        _deferredPatch.Tick += (_, _) =>
        {
            _deferredPatch?.Stop();
            _deferredPatch = null;
            if (_pendingPatch is not { } pending)
                return;
            var reset = _pendingResetDismiss;
            _pendingPatch = null;
            SafeShowOrUpdate(pending, reset);
        };
        _deferredPatch.Start();
    }

    private void CancelDeferredPatch()
    {
        _deferredPatch?.Stop();
        _deferredPatch = null;
        _pendingPatch = null;
        _pendingUpdate = null;
        _pendingSoftRefresh = null;
        _deferredSoftRefresh?.Stop();
        _deferredSoftRefresh = null;
        _updateDispatch.CompleteDispatch();
    }

    private void ScheduleDeferredSoftRefresh(TimeSpan delay)
    {
        if (!TesseraFlyoutLiveSyncPolicy.SoftRefreshMustScheduleDeferredLastValue)
            return;

        if (delay <= TimeSpan.Zero)
            delay = TesseraFlyoutLiveSyncPolicy.MinFlushInterval;

        _deferredSoftRefresh?.Stop();
        _deferredSoftRefresh = new DispatcherTimer { Interval = delay };
        _deferredSoftRefresh.Tick += (_, _) =>
        {
            _deferredSoftRefresh?.Stop();
            _deferredSoftRefresh = null;
            if (_pendingSoftRefresh is not { } pending)
                return;
            _pendingSoftRefresh = null;
            SoftRefresh(pending);
        };
        _deferredSoftRefresh.Start();
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

        var focusDimOn = TesseraFocusDimPolicy.EnabledFromPayload(request.Payload);
        var layered = TesseraFlyoutWindowPolicy.MustApplyPresentableLayeredAlpha
            ? Win32WindowChrome.ForceOpaqueLayer(window)
            : "skipped";
        var screen = window.Screens?.ScreenFromWindow(window)
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
            ScheduleOutsideClickArm(window);
    }

    private void ScheduleOutsideClickArm(FlyoutWindow window)
    {
        // Present owns the hook: dispose prior watcher before the arm delay (stale bounds otherwise).
        if (TesseraFlyoutOutsideClickPolicy.PresentMustStopPriorWatcherBeforeRearm)
            StopOutsideClickWatcher();
        else
            _outsideClickArm?.Stop();

        _outsideClickArm = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        var captured = window;
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
                if (_focusDim is not null)
                    _focusDim.Topmost = true;

                var ok = Win32WindowChrome.TryStackAbove(flyout, _focusDim, out var detail);
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
        var dim = _focusDim;
        _focusDim = null;
        if (dim is null) return;
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
                foreach (var slotKey in _stackedSession.SlotKeys)
                {
                    if (_windows.TryGetValue(slotKey, out var window)
                        && window.IsFlyoutSessionShowing)
                        window.TransientDismissWithoutNotify();
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
            return;
        Log("transient-dismiss, closing focusDim");
        StopOutsideClickWatcher();
        CloseFocusDim();
    }

    private Control BuildContent(FlyoutRequest request)
    {
        if (request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
        {
            var material = TesseraFlyoutMaterialFactory.FromPayload(
                request.Payload, request.StyleId, request.Kind);
            TesseraPalette.ApplyMaterial(material);

            var osAcrylicEligible = TesseraFlyoutMaterialFactory.OsAcrylicEligibleFromPayload(
                request.Payload, request.StyleId, request.Kind);
            var settingsWantBlur = TesseraFlyoutRequestBuilder.BackdropBlurFromPayload(request.Payload);
            var glass = TesseraFlyoutGlassBinder.ApplyForLiveFlyout(settingsWantBlur, osAcrylicEligible);
            Log(
                $"glass mode={glass.Mode} backdrop={glass.UseBackdropBlur} " +
                $"embeddedPreview={glass.UseEmbeddedPreview} softFrostHwnd={glass.SoftFrostHwndReady} " +
                $"osAcrylic={glass.OsAcrylicEligible}");

            var vm = TesseraFlyoutViewModel.FromRequest(_services, request, _hostUi);
            var root = TesseraStyleFactory.Create(
                request.StyleId ?? "Fluent",
                vm,
                accentColor: TesseraFlyoutRequestBuilder.AccentFromPayload(request.Payload),
                embeddedPreview: glass.UseEmbeddedPreview);
            var scale = FlyoutScaleFromPayload(request.Payload);
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

    private static Control BuildFallbackContent(FlyoutRequest request, string? error) =>
        new Border
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

    private static double FlyoutScaleFromPayload(IReadOnlyDictionary<string, string>? payload)
    {
        if (payload is null || !payload.TryGetValue("flyoutScale", out var raw))
            return 1.0;
        if (!int.TryParse(raw, out var pct))
            return 1.0;
        return Math.Clamp(pct, 50, 150) / 100.0;
    }

    private static void Log(string message) => TesseraFlyoutDiagnostics.Log(message);
}
