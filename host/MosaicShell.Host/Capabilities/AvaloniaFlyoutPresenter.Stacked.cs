using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Host.Tiles.Tessera;

namespace MosaicShell.Host.Capabilities;

public sealed partial class AvaloniaFlyoutPresenter
{
    private sealed class TesseraStackedSession
    {
        public TesseraLiveBindings Bindings { get; } = new();
        public List<string> SlotKeys { get; } = [];
        public string Kind { get; set; } = "";
        public string? StyleId { get; set; }
    }

    private TesseraStackedSession? _stackedSession;
    private List<FlyoutWindow>? _stackedWindows;
    private FlyoutRequest? _stackedDismissRequest;
    private DispatcherTimer? _stackedAutoDismiss;
    private bool _stackedClusterHovered;

    private bool ShouldUseStackedOsAcrylic(FlyoutRequest request) =>
        request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase)
        && TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(request.Payload, request.StyleId);

    private void ShowOrUpdateStacked(FlyoutRequest request, bool resetDismiss)
    {
        Log($"ShowOrUpdateStacked enter kind={request.Kind} style={request.StyleId}");
        CloseSingleTesseraWindowIfAny();
        EnsureStackedSession(request);

        if (TryPatchStacked(request, resetDismiss))
            return;

        if (TryReviveStacked(request, resetDismiss))
            return;

        var panels = TesseraOsAcrylicStackedPolicy.ResolvePanels(request.Payload, request.StyleId);
        if (panels.Count == 0)
        {
            Log("stacked panels empty, falling back to single HWND");
            ShowOrUpdateSingleTessera(request, resetDismiss);
            return;
        }

        RebuildStackedSlots(request, panels, resetDismiss);
    }

    private void EnsureStackedSession(FlyoutRequest request)
    {
        _stackedSession ??= new TesseraStackedSession();
        _stackedSession.Kind = request.Kind;
        _stackedSession.StyleId = request.StyleId;
    }

    private bool TryPatchStacked(FlyoutRequest request, bool resetDismiss)
    {
        if (_stackedSession is null || _stackedSession.SlotKeys.Count == 0)
            return false;

        var volumeKey = TesseraOsAcrylicStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Volume);
        lock (_gate)
        {
            if (!_windows.TryGetValue(volumeKey, out var volumeWin)
                || !volumeWin.IsFlyoutSessionShowing)
                return false;

            if (!string.Equals(_stackedSession.Kind, request.Kind, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(_stackedSession.StyleId ?? "", request.StyleId ?? "", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!volumeWin.TryApplyLive(request, _services, resetDismiss))
                return false;

            Log($"stacked patch kind={request.Kind} style={request.StyleId}");
            volumeWin.EnsureLivePump();
            ApplyStackedPlacement(request);
            if (resetDismiss)
                BumpStackedAutoDismiss();
            return true;
        }
    }

    private bool TryReviveStacked(FlyoutRequest request, bool resetDismiss)
    {
        if (_stackedSession is null || _stackedSession.SlotKeys.Count == 0)
            return false;

        if (!string.Equals(_stackedSession.Kind, request.Kind, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(_stackedSession.StyleId ?? "", request.StyleId ?? "", StringComparison.OrdinalIgnoreCase))
            return false;

        var windows = GetStackedWindowsFromSession();
        if (windows.Count == 0)
            return false;

        var volumeKey = TesseraOsAcrylicStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Volume);
        FlyoutWindow? volumeWin;
        lock (_gate)
        {
            if (!_windows.TryGetValue(volumeKey, out volumeWin))
                return false;
        }

        if (volumeWin.IsFlyoutSessionShowing)
            return false;

        if (!volumeWin.TryApplyLive(request, _services, resetDismiss))
            return false;

        Log($"stacked revive kind={request.Kind} style={request.StyleId}");
        ApplyStackedPlacement(request);
        SyncFocusDim(request);

        foreach (var window in windows)
        {
            if (!window.IsVisible)
                window.Show();
            window.EnsureLivePump();
        }

        PresentStackedFlyout(request, windows);
        foreach (var window in windows)
        {
            window.PlayShowAnimation();
            window.RevealAfterLayout();
        }

        ScheduleStackedPlacementRefresh(request);
        WireStackedDismissCoordinator(request, windows);
        return true;
    }

    private void RebuildStackedSlots(
        FlyoutRequest request,
        IReadOnlyList<TesseraStackedPanelRole> panels,
        bool resetDismiss)
    {
        var bindings = _stackedSession!.Bindings;
        var newKeys = new List<string>();
        var newWindows = new List<FlyoutWindow>();

        foreach (var role in panels)
        {
            var slotKey = TesseraOsAcrylicStackedPolicy.WindowSlotKey("Tessera", role);
            newKeys.Add(slotKey);

            Control content;
            try
            {
                content = BuildStackedPanelContent(request, role, bindings);
            }
            catch (Exception ex)
            {
                Log($"BuildStackedPanelContent failed role={role}: {ex}");
                content = BuildFallbackContent(request, ex.Message);
            }

            FlyoutWindow window;
            lock (_gate)
            {
                if (_windows.TryGetValue(slotKey, out var existing)
                    && TesseraOsAcrylicStackedPolicy.MustReuseRegisteredFlyoutHwndPerSlot)
                {
                    window = existing;
                    window.ApplyRequest(request, content);
                }
                else
                {
                    if (_windows.TryGetValue(slotKey, out var old))
                    {
                        try { old.Close(); } catch { /* ignore */ }
                        _windows.Remove(slotKey);
                    }

                    window = new FlyoutWindow(request, content, _services);
                    window.Closed += (_, _) => OnStackedSlotClosed(slotKey, window);
                    _windows[slotKey] = window;
                }
            }

            WireTesseraSession(window);
            newWindows.Add(window);
        }

        WireStackedDismissCoordinator(request, newWindows);

        var staleKeys = _stackedSession.SlotKeys.Except(newKeys).ToList();
        foreach (var staleKey in staleKeys)
        {
            lock (_gate)
            {
                if (_windows.Remove(staleKey, out var stale))
                {
                    try { stale.Close(); } catch { /* ignore */ }
                }
            }
        }

        _stackedSession.SlotKeys.Clear();
        _stackedSession.SlotKeys.AddRange(newKeys);
        _stackedWindows = newWindows;

        ApplyStackedPlacement(request);
        SyncFocusDim(request);

        foreach (var window in newWindows)
        {
            if (!window.IsVisible)
                window.Show();
            window.EnsureLivePump();
        }

        PresentStackedFlyout(request, newWindows);
        foreach (var window in newWindows)
        {
            window.PlayShowAnimation();
            window.RevealAfterLayout();
        }

        ScheduleStackedPlacementRefresh(request);
    }

    private List<FlyoutWindow> GetStackedWindowsFromSession()
    {
        var list = new List<FlyoutWindow>();
        if (_stackedSession is null)
            return list;

        lock (_gate)
        {
            foreach (var slotKey in _stackedSession.SlotKeys)
            {
                if (_windows.TryGetValue(slotKey, out var window))
                    list.Add(window);
            }
        }

        return list;
    }

    private void ScheduleStackedPlacementRefresh(FlyoutRequest request)
    {
        // RebuildStackedSlots / TryReviveStacked already call ApplyStackedPlacement.
        // A Loaded repass reads inflated HWND Bounds and blew up cluster geometry (see flyout.log).
    }

    private static Screen? ResolveMonitorScreen(IReadOnlyList<Screen> screens, int monitorIndexOneBased)
    {
        if (screens.Count == 0)
            return null;
        if (monitorIndexOneBased <= 1)
            return screens.FirstOrDefault(s => s.IsPrimary) ?? screens[0];

        var idx = Math.Clamp(monitorIndexOneBased - 1, 0, screens.Count - 1);
        return screens[idx];
    }

    private void WireStackedDismissCoordinator(FlyoutRequest request, IReadOnlyList<FlyoutWindow> windows)
    {
        StopStackedAutoDismiss();
        _stackedDismissRequest = request;

        foreach (var window in windows)
        {
            window.SuppressAutoDismiss();
            window.FlyoutUserActivity -= OnStackedFlyoutUserActivity;
            window.FlyoutUserActivity += OnStackedFlyoutUserActivity;
            window.PointerHoverChanged -= OnStackedPointerHoverChanged;
            window.PointerHoverChanged += OnStackedPointerHoverChanged;
        }

        UpdateStackedClusterHover();
        StartStackedAutoDismiss();
    }

    private void OnStackedFlyoutUserActivity() => BumpStackedAutoDismiss();

    private void OnStackedPointerHoverChanged() => UpdateStackedClusterHover();

    private void UpdateStackedClusterHover()
    {
        _stackedClusterHovered = GetStackedWindowsFromSession().Any(w => w.IsFlyoutHovered);
    }

    private void StartStackedAutoDismiss()
    {
        StopStackedAutoDismiss();
        var ms = _stackedDismissRequest?.AutoDismissMs ?? 0;
        if (ms <= 0 || _stackedSession is null)
            return;

        _stackedAutoDismiss = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ms) };
        _stackedAutoDismiss.Tick += (_, _) =>
        {
            UpdateStackedClusterHover();
            if (_stackedClusterHovered)
                return;

            _stackedAutoDismiss?.Stop();
            DismissStackedTesseraImmediate(notify: true);
        };
        _stackedAutoDismiss.Start();
    }

    private void BumpStackedAutoDismiss()
    {
        if (_stackedSession is null)
            return;

        StartStackedAutoDismiss();
    }

    private void StopStackedAutoDismiss()
    {
        _stackedAutoDismiss?.Stop();
        _stackedAutoDismiss = null;
    }

    private void TransientDismissAllStackedSlots(bool notify)
    {
        if (_stackedSession is null)
            return;

        var anyVisible = false;
        lock (_gate)
        {
            foreach (var slotKey in _stackedSession.SlotKeys)
            {
                if (!_windows.TryGetValue(slotKey, out var window))
                    continue;
                if (!window.IsFlyoutSessionShowing)
                    continue;

                anyVisible = true;
                window.TransientDismissWithoutNotify();
            }
        }

        StopStackedAutoDismiss();

        if (notify && anyVisible)
        {
            try { TransientDismissed?.Invoke("Tessera"); }
            catch { /* ignore */ }
        }
    }

    private TesseraStackedPanelRole RoleForSlotKey(string slotKey)
    {
        if (slotKey.EndsWith(":media", StringComparison.OrdinalIgnoreCase))
            return TesseraStackedPanelRole.Media;
        if (slotKey.EndsWith(":device", StringComparison.OrdinalIgnoreCase))
            return TesseraStackedPanelRole.Device;
        return TesseraStackedPanelRole.Volume;
    }

    private void ApplyStackedPlacement(FlyoutRequest request)
    {
        if (_stackedSession is null || _stackedSession.SlotKeys.Count == 0)
            return;

        try
        {
            ApplyStackedPlacementCore(request);
        }
        catch (Exception ex)
        {
            TesseraFlyoutDiagnostics.LogException($"stacked placement style={request.StyleId}", ex);
        }
    }

    private void ApplyStackedPlacementCore(FlyoutRequest request)
    {
        var session = _stackedSession;
        if (session is null || session.SlotKeys.Count == 0)
            return;

        var windowByRole = new Dictionary<TesseraStackedPanelRole, FlyoutWindow>();
        lock (_gate)
        {
            foreach (var slotKey in session.SlotKeys)
            {
                if (!_windows.TryGetValue(slotKey, out var window))
                    continue;
                windowByRole[RoleForSlotKey(slotKey)] = window;
            }
        }

        if (windowByRole.Count == 0)
            return;

        var flyoutScale = TesseraFlyoutRequestBuilder.FlyoutScaleFromPayload(request.Payload);
        var estimates = TesseraStackedPlacementPolicy.ScalePlacements(
                TesseraStackedPlacementPolicy.EstimatePlacements(request.StyleId),
                flyoutScale)
            .ToDictionary(p => p.Role);

        double PanelWidth(FlyoutWindow w, TesseraStackedPanelRole role)
        {
            if (!estimates.TryGetValue(role, out var est))
                return Math.Max(w.DesiredSize.Width, 1);

            var raw = Math.Max(w.DesiredSize.Width, 0);
            var safe = TesseraStackedPlacementPolicy.SanitizeStackedPanelMeasure(est.WidthDip, raw);
            return safe > 0 ? Math.Max(est.WidthDip, safe) : est.WidthDip;
        }

        double PanelHeight(FlyoutWindow w, TesseraStackedPanelRole role)
        {
            if (!estimates.TryGetValue(role, out var est))
                return Math.Max(w.DesiredSize.Height, 1);

            var raw = Math.Max(w.DesiredSize.Height, 0);
            var safe = TesseraStackedPlacementPolicy.SanitizeStackedPanelMeasure(est.HeightDip, raw);
            return safe > 0 ? Math.Max(est.HeightDip, safe) : est.HeightDip;
        }

        windowByRole.TryGetValue(TesseraStackedPanelRole.Volume, out var volumeWin);
        windowByRole.TryGetValue(TesseraStackedPanelRole.Media, out var mediaWin);
        volumeWin ??= windowByRole.Values.First();
        mediaWin ??= windowByRole.Values.Last();

        var placements = TesseraStackedPlacementPolicy.ScalePlacements(
            TesseraStackedPlacementPolicy.ComputePlacements(
                request.StyleId,
                PanelWidth(volumeWin, TesseraStackedPanelRole.Volume) / flyoutScale,
                PanelHeight(volumeWin, TesseraStackedPanelRole.Volume) / flyoutScale,
                PanelWidth(mediaWin, TesseraStackedPanelRole.Media) / flyoutScale,
                PanelHeight(mediaWin, TesseraStackedPanelRole.Media) / flyoutScale),
            flyoutScale);

        var (combinedW, combinedH) = TesseraStackedPlacementPolicy.ClusterSizeFromPlacements(placements);
        var (estimateW, estimateH) = TesseraStackedPlacementPolicy.EstimateClusterSize(request.StyleId);
        estimateW *= flyoutScale;
        estimateH *= flyoutScale;
        combinedW = Math.Max(combinedW, estimateW);
        combinedH = Math.Max(combinedH, estimateH);
        var screens = volumeWin.Screens?.All?.ToList() ?? [];
        var screen = ResolveMonitorScreen(screens, request.MonitorIndex)
                     ?? volumeWin.Screens?.Primary;
        var area = screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);
        var scale = screen?.Scaling > 0.1 ? screen.Scaling : 1.0;
        var wPx = Math.Max(1, (int)Math.Ceiling(combinedW * scale));
        var hPx = Math.Max(1, (int)Math.Ceiling(combinedH * scale));
        var (clusterX, clusterY) = FlyoutAnchor.Compute(
            area.X, area.Y, area.Width, area.Height,
            wPx, hPx,
            request.Anchor ?? "TL",
            request.XPad,
            request.YPad);

        TesseraFlyoutDiagnostics.Log(
            $"stacked cluster style={request.StyleId} combined={combinedW:F0}x{combinedH:F0} " +
            $"estimate={estimateW:F0}x{estimateH:F0} anchor={clusterX},{clusterY} scale={scale:F2}");

        foreach (var slotKey in session.SlotKeys)
        {
            FlyoutWindow? window;
            lock (_gate)
            {
                if (!_windows.TryGetValue(slotKey, out window))
                    continue;
            }

            var role = RoleForSlotKey(slotKey);
            var placement = placements.First(p => p.Role == role);
            _ = estimates.TryGetValue(role, out var estimate);
            var estW = estimate.Role == role ? estimate.WidthDip : placement.WidthDip;
            var estH = estimate.Role == role ? estimate.HeightDip : placement.HeightDip;
            var measuredW = PanelWidth(window, role);
            var measuredH = PanelHeight(window, role);
            window.ClusterOriginX = clusterX;
            window.ClusterOriginY = clusterY;
            window.PanelOffsetXDip = (int)Math.Round(placement.OffsetXDip);
            window.PanelOffsetYDip = (int)Math.Round(placement.OffsetYDip);
            var priorW = window.StackedPanelWidthDip;
            var priorH = window.StackedPanelHeightDip;
            if (priorW != placement.WidthDip || priorH != placement.HeightDip)
                window.UnlockStackedClientSize();
            window.StackedPanelWidthDip = placement.WidthDip;
            window.StackedPanelHeightDip = placement.HeightDip;
            window.BackdropCornerRadiusDip = TesseraOsAcrylicStackedPolicy.ResolvePanelCornerRadiusDip(
                request.StyleId,
                role,
                placement.WidthDip,
                placement.HeightDip);

            window.FinishLayout();

            var clientW = double.IsNaN(window.Width) ? 0 : window.Width;
            var clientH = double.IsNaN(window.Height) ? 0 : window.Height;
            TesseraFlyoutDiagnostics.LogStackedPlacement(
                request.StyleId,
                role,
                estW,
                estH,
                measuredW,
                measuredH,
                placement.WidthDip,
                placement.HeightDip,
                placement.OffsetXDip,
                placement.OffsetYDip,
                clientW,
                clientH,
                window.Position.X,
                window.Position.Y);
        }
    }

    private Control BuildStackedPanelContent(
        FlyoutRequest request,
        TesseraStackedPanelRole role,
        TesseraLiveBindings bindings)
    {
        var material = TesseraFlyoutMaterialFactory.FromPayload(request.Payload, request.StyleId);
        TesseraPalette.ApplyMaterial(material);

        var osAcrylicEligible = true;
        var settingsWantBlur = TesseraFlyoutRequestBuilder.BackdropBlurFromPayload(request.Payload);
        var glass = TesseraFlyoutGlassBinder.ApplyForLiveFlyout(settingsWantBlur, osAcrylicEligible);
        Log(
            $"stacked glass role={role} mode={glass.Mode} osAcrylic={glass.OsAcrylicEligible}");

        var vm = TesseraFlyoutViewModel.FromRequest(_services, request, _hostUi);
        var accent = TesseraFlyoutRequestBuilder.AccentFromPayload(request.Payload);
        var root = TesseraStackedPanelFactory.CreatePanel(
            request.StyleId ?? "Fluent",
            vm,
            role,
            bindings,
            accent,
            glass.UseEmbeddedPreview);
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

    private void PresentStackedFlyout(FlyoutRequest request, IReadOnlyList<FlyoutWindow> windows)
    {
        if (windows.Count == 0)
            return;

        SyncFocusDim(request);

        foreach (var slotKey in _stackedSession!.SlotKeys
                     .OrderBy(k => TesseraOsAcrylicStackedPolicy.ZOrderRank(RoleForSlotKey(k))))
        {
            lock (_gate)
            {
                if (!_windows.TryGetValue(slotKey, out var window))
                    continue;
                window.FinishLayout();
                RestackAboveDim(window);
            }
        }

        Log($"stacked presented slots={windows.Count} focusDim={TesseraFocusDimPolicy.EnabledFromPayload(request.Payload)}");
        ScheduleStackedOutsideClickArm(windows);
    }

    private void ScheduleStackedOutsideClickArm(IReadOnlyList<FlyoutWindow> windows)
    {
        _outsideClickArm?.Stop();
        _outsideClickArm = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        var captured = windows.ToList();
        _outsideClickArm.Tick += (_, _) =>
        {
            _outsideClickArm?.Stop();
            _outsideClickArm = null;
            if (captured.Any(w => w.IsVisible))
            {
                StartStackedOutsideClickWatcher(captured);
                Log("stacked outside-click armed");
            }
        };
        _outsideClickArm.Start();
    }

    private void StartStackedOutsideClickWatcher(IReadOnlyList<FlyoutWindow> windows)
    {
        StopOutsideClickWatcher();
        _outsideClick = new TesseraOutsideClickWatcher(windows, DismissTesseraImmediate);
        _outsideClick.Start();
    }

    private void OnStackedSlotClosed(string slotKey, FlyoutWindow window)
    {
        lock (_gate)
        {
            if (_windows.TryGetValue(slotKey, out var current) && ReferenceEquals(current, window))
                _windows.Remove(slotKey);
        }

        if (_stackedSession is null)
            return;

        _stackedSession.SlotKeys.Remove(slotKey);
        if (_stackedSession.SlotKeys.Count == 0)
        {
            _stackedSession = null;
            _stackedWindows = null;
            StopOutsideClickWatcher();
            CloseFocusDim();
            CancelDeferredPatch();
        }
    }

    private void CloseStackedSession()
    {
        if (_stackedSession is null)
            return;

        StopStackedAutoDismiss();
        _stackedDismissRequest = null;

        var keys = _stackedSession.SlotKeys.ToList();
        _stackedSession = null;
        _stackedWindows = null;
        foreach (var key in keys)
        {
            lock (_gate)
            {
                if (_windows.Remove(key, out var w))
                {
                    try { w.Close(); } catch { /* ignore */ }
                }
            }
        }
    }

    private void CloseSingleTesseraWindowIfAny()
    {
        lock (_gate)
        {
            if (_windows.Remove("Tessera", out var single))
            {
                try { single.Close(); } catch { /* ignore */ }
            }
        }
    }

    private void ShowOrUpdateSingleTessera(FlyoutRequest request, bool resetDismiss)
    {
        CloseStackedSession();
        ShowOrUpdateCoreSinglePath(request, resetDismiss);
    }

    private void DismissStackedTesseraImmediate(bool notify = true)
    {
        if (_stackedSession is null)
        {
            DismissTesseraSingleImmediate();
            return;
        }

        Log("stacked dismiss");
        TransientDismissAllStackedSlots(notify);
        StopOutsideClickWatcher();
        CloseFocusDim();
    }

    private void DismissTesseraSingleImmediate()
    {
        FlyoutWindow? flyout;
        lock (_gate) _windows.TryGetValue("Tessera", out flyout);
        if (flyout is null) return;
        flyout.TransientDismiss();
    }

    private bool IsStackedTesseraVisible()
    {
        if (_stackedSession is null)
            return false;

        lock (_gate)
        {
            return _stackedSession.SlotKeys.Any(k =>
                _windows.TryGetValue(k, out var w) && w.IsFlyoutSessionShowing);
        }
    }
}
