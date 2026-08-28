using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Host.Tiles.Tessera;

namespace MosaicShell.Host.Capabilities
{
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
        private FlyoutRequest? _stackedDismissRequest;
        private DispatcherTimer? _stackedAutoDismiss;
        private bool _stackedClusterHovered;

        private bool ShouldUseStackedOsAcrylic(FlyoutRequest request)
        {
            return request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase)
            && TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(
                request.Payload, request.StyleId, request.Kind);
        }

        private void ShowOrUpdateStacked(FlyoutRequest request, bool resetDismiss, bool allowLivePatch = true)
        {
            Log($"ShowOrUpdateStacked enter kind={request.Kind} style={request.StyleId}");
            CloseSingleTesseraWindowIfAny();
            _stackedSession ??= new TesseraStackedSession();

            if (allowLivePatch && TryPatchStacked(request, resetDismiss))
            {
                _stackedSession.Kind = request.Kind;
                _stackedSession.StyleId = request.StyleId;
                return;
            }

            if (TryReviveStacked(request, resetDismiss))
            {
                _stackedSession.Kind = request.Kind;
                _stackedSession.StyleId = request.StyleId;
                EnsureTesseraSession(request, TesseraFlyoutSessionMode.Stacked);
                return;
            }

            IReadOnlyList<TesseraStackedPanelRole> panels = TesseraOsAcrylicStackedPolicy.ResolvePanels(request.Payload, request.StyleId);
            if (panels.Count == 0)
            {
                Log("stacked panels empty, falling back to single HWND");
                ShowOrUpdateSingleTessera(request, resetDismiss, allowLivePatch);
                return;
            }

            _stackedSession.Kind = request.Kind;
            _stackedSession.StyleId = request.StyleId;
            EnsureTesseraSession(request, TesseraFlyoutSessionMode.Stacked);
            RebuildStackedSlots(request, panels, resetDismiss);
        }

        private bool TryPatchStacked(FlyoutRequest request, bool resetDismiss)
        {
            if (_stackedSession is null || _stackedSession.SlotKeys.Count == 0)
            {
                return false;
            }

            string volumeKey = TesseraOsAcrylicStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Volume);
            lock (_gate)
            {
                if (!_windows.TryGetValue(volumeKey, out FlyoutWindow? volumeWin)
                    || !volumeWin.IsFlyoutSessionShowing)
                {
                    return false;
                }

                if (!string.Equals(_stackedSession.Kind, request.Kind, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(_stackedSession.StyleId ?? "", request.StyleId ?? "", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!volumeWin.TryApplyLive(request, _services, resetDismiss))
                {
                    return false;
                }

                Log($"stacked patch kind={request.Kind} style={request.StyleId}");
                volumeWin.EnsureLivePump();
                ApplyStackedPlacement(request);
                if (resetDismiss)
                {
                    BumpStackedAutoDismiss();
                }
            }

            RefreshStackedOutsideClickBoundsAfterPatch();
            return true;
        }

        private void RefreshStackedOutsideClickBoundsAfterPatch()
        {
            if (_outsideClick is not { IsActive: true })
            {
                return;
            }

            _outsideClick.RefreshBounds(GetStackedWindowsFromSession());
        }

        private bool TryReviveStacked(FlyoutRequest request, bool resetDismiss)
        {
            if (_stackedSession is null || _stackedSession.SlotKeys.Count == 0)
            {
                return false;
            }

            if (!string.Equals(_stackedSession.Kind, request.Kind, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(_stackedSession.StyleId ?? "", request.StyleId ?? "", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            List<FlyoutWindow> windows = GetStackedWindowsFromSession();
            if (windows.Count == 0)
            {
                return false;
            }

            string volumeKey = TesseraOsAcrylicStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Volume);
            FlyoutWindow? volumeWin;
            lock (_gate)
            {
                if (!_windows.TryGetValue(volumeKey, out volumeWin))
                {
                    return false;
                }
            }

            if (volumeWin.IsFlyoutSessionShowing)
            {
                return false;
            }

            if (!volumeWin.TryApplyLive(request, _services, resetDismiss))
            {
                return false;
            }

            Log($"stacked revive kind={request.Kind} style={request.StyleId}");
            ApplyStackedPlacement(request);
            SyncFocusDim(request);

            foreach (FlyoutWindow window in windows)
            {
                if (!window.IsVisible)
                {
                    window.Show();
                }

                window.EnsureLivePump();
            }

            PresentStackedFlyout(request, windows);
            PlayStackedShowAnimation(request, windows, play: true);

            ScheduleStackedPlacementRefresh();
            WireStackedDismissCoordinator(request, windows);
            return true;
        }

        private void RebuildStackedSlots(
            FlyoutRequest request,
            IReadOnlyList<TesseraStackedPanelRole> panels,
            bool resetDismiss)
        {
            bool reuseWasVisible = GetStackedWindowsFromSession().Any(w => w.IsFlyoutSessionShowing);
            TesseraLiveBindings bindings = _stackedSession!.Bindings;
            List<string> newKeys = [];
            List<FlyoutWindow> newWindows = [];

            foreach (TesseraStackedPanelRole role in panels)
            {
                string slotKey = TesseraOsAcrylicStackedPolicy.WindowSlotKey("Tessera", role);
                newKeys.Add(slotKey);

                Control content;
                try
                {
                    content = BuildStackedPanelContent(request, role, bindings, reuseWasVisible);
                }
                catch (Exception ex)
                {
                    Log($"BuildStackedPanelContent failed role={role}: {ex}");
                    content = BuildFallbackContent(request, ex.Message);
                }

                FlyoutWindow window;
                lock (_gate)
                {
                    if (_windows.TryGetValue(slotKey, out FlyoutWindow? existing)
                        && TesseraOsAcrylicStackedPolicy.MustReuseRegisteredFlyoutHwndPerSlot)
                    {
                        window = existing;
                        window.StackedRole = role;
                        window.ApplyRequest(request, content);
                    }
                    else
                    {
                        if (_windows.TryGetValue(slotKey, out FlyoutWindow? old))
                        {
                            try { old.Close(); } catch { /* ignore */ }
                            _ = _windows.Remove(slotKey);
                        }

                        window = new FlyoutWindow(request, content, _services)
                        {
                            StackedRole = role
                        };
                        window.Closed += (_, _) => OnStackedSlotClosed(slotKey, window);
                        _windows[slotKey] = window;
                    }
                }

                WireTesseraSession(window);
                newWindows.Add(window);
            }

            WireStackedDismissCoordinator(request, newWindows);

            List<string> staleKeys = [.. _stackedSession.SlotKeys.Except(newKeys)];
            foreach (string staleKey in staleKeys)
            {
                lock (_gate)
                {
                    if (_windows.Remove(staleKey, out FlyoutWindow? stale))
                    {
                        try { stale.Close(); } catch { /* ignore */ }
                    }
                }
            }

            _stackedSession.SlotKeys.Clear();
            _stackedSession.SlotKeys.AddRange(newKeys);

            ApplyStackedPlacement(request);
            SyncFocusDim(request);

            foreach (FlyoutWindow window in newWindows)
            {
                if (!window.IsVisible)
                {
                    window.Show();
                }

                window.EnsureLivePump();
            }

            PresentStackedFlyout(request, newWindows);
            bool play = TesseraFlyoutLiveSyncPolicy.ShouldPlayShowAnimationAfterApplyRequest(
                reuseWasVisible, TesseraFlyoutWindowPolicy.HideUntilCompositionReady);
            PlayStackedShowAnimation(request, newWindows, play);

            ScheduleStackedPlacementRefresh();
        }

        private List<FlyoutWindow> GetStackedWindowsFromSession()
        {
            List<FlyoutWindow> list = [];
            if (_stackedSession is null)
            {
                return list;
            }

            lock (_gate)
            {
                foreach (string slotKey in _stackedSession.SlotKeys)
                {
                    if (_windows.TryGetValue(slotKey, out FlyoutWindow? window))
                    {
                        list.Add(window);
                    }
                }
            }

            return list;
        }

        private void ScheduleStackedPlacementRefresh()
        {
            // RebuildStackedSlots / TryReviveStacked already call ApplyStackedPlacement.
            // A Loaded repass reads inflated HWND Bounds and blew up cluster geometry (see flyout.log).
        }

        private static Screen? ResolveMonitorScreen(IReadOnlyList<Screen> screens, int monitorIndexOneBased)
        {
            if (screens.Count == 0)
            {
                return null;
            }

            if (monitorIndexOneBased <= 1)
            {
                return screens.FirstOrDefault(s => s.IsPrimary) ?? screens[0];
            }

            int idx = Math.Clamp(monitorIndexOneBased - 1, 0, screens.Count - 1);
            return screens[idx];
        }

        private void WireStackedDismissCoordinator(FlyoutRequest request, IReadOnlyList<FlyoutWindow> windows)
        {
            StopStackedAutoDismiss();
            _stackedDismissRequest = request;

            foreach (FlyoutWindow window in windows)
            {
                if (TesseraFlyoutDismissCoordinator.WindowMustSuppressAutoDismiss(
                        TesseraFlyoutDismissCoordinator.SessionOwnsAutoDismissClock))
                {
                    window.SuppressAutoDismiss();
                }

                window.FlyoutUserActivity -= OnStackedFlyoutUserActivity;
                window.FlyoutUserActivity += OnStackedFlyoutUserActivity;
                window.PointerHoverChanged -= OnStackedPointerHoverChanged;
                window.PointerHoverChanged += OnStackedPointerHoverChanged;
            }

            UpdateStackedClusterHover();
            StartStackedAutoDismiss();
        }

        private void OnStackedFlyoutUserActivity()
        {
            BumpStackedAutoDismiss();
        }

        private void OnStackedPointerHoverChanged()
        {
            UpdateStackedClusterHover();
        }

        private void UpdateStackedClusterHover()
        {
            _stackedClusterHovered = GetStackedWindowsFromSession().Any(w => w.IsFlyoutHovered);
        }

        private void StartStackedAutoDismiss()
        {
            StopStackedAutoDismiss();
            int ms = _stackedDismissRequest?.AutoDismissMs ?? 0;
            if (!TesseraFlyoutDismissCoordinator.ShouldArmSessionAutoDismiss(ms, _stackedSession is not null))
            {
                return;
            }

            _stackedAutoDismiss = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ms) };
            _stackedAutoDismiss.Tick += (_, _) =>
            {
                UpdateStackedClusterHover();
                if (_stackedClusterHovered)
                {
                    return;
                }

                _stackedAutoDismiss?.Stop();
                DismissStackedTesseraImmediate(notify: true);
            };
            _stackedAutoDismiss.Start();
        }

        private void BumpStackedAutoDismiss()
        {
            if (_stackedSession is null)
            {
                return;
            }

            StartStackedAutoDismiss();
        }

        private void StopStackedAutoDismiss()
        {
            _stackedAutoDismiss?.Stop();
            _stackedAutoDismiss = null;
        }

        private void PlayStackedShowAnimation(FlyoutRequest request, IReadOnlyList<FlyoutWindow> windows, bool play)
        {
            _ = request;
            if (windows.Count == 0)
            {
                return;
            }

            foreach (FlyoutWindow window in windows)
            {
                window.FinishLayout();
            }

            if (!play)
            {
                return;
            }

            foreach (FlyoutWindow window in windows)
            {
                window.PresenterDrivesMotion = true;
            }

            if (!TesseraFlyoutWindowPolicy.HideUntilCompositionReady)
            {
                _ = RunStackedShowAnimationAsync(windows);
                return;
            }

            List<FlyoutWindow> captured = [.. windows];
            Dispatcher.UIThread.Post(() =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _ = RunStackedShowAnimationAsync(captured);
                }, DispatcherPriority.Render);
            }, DispatcherPriority.Loaded);
        }

        private static async Task RunStackedShowAnimationAsync(IReadOnlyList<FlyoutWindow> windows)
        {
            await FlyoutMotionSession.RunShowAsync(windows).ConfigureAwait(true);
        }

        private async Task RunStackedExitAnimationAsync(FlyoutRequest request, IReadOnlyList<FlyoutWindow> windows)
        {
            _ = request;
            await FlyoutMotionSession.RunHideAsync(windows).ConfigureAwait(true);
        }

        private void TransientDismissAllStackedSlots(bool notify)
        {
            if (_stackedSession is null)
            {
                return;
            }

            List<FlyoutWindow> windows = [.. GetStackedWindowsFromSession().Where(w => w.IsFlyoutSessionShowing)];
            if (windows.Count == 0)
            {
                StopStackedAutoDismiss();
                return;
            }

            if (_stackedDismissRequest is not null
                && TesseraFlyoutAnimationPolicy.ShouldAnimateOpacity(_stackedDismissRequest.Ani)
                && TesseraFlyoutAnimationPolicy.ExitMustMirrorEntrance)
            {
                _ = DismissStackedAnimatedAsync(notify, windows, _stackedDismissRequest);
                return;
            }

            DismissStackedImmediate(notify, windows);
        }

        private async Task DismissStackedAnimatedAsync(
            bool notify,
            IReadOnlyList<FlyoutWindow> windows,
            FlyoutRequest request)
        {
            await RunStackedExitAnimationAsync(request, windows).ConfigureAwait(true);
            DismissStackedImmediate(notify, windows);
        }

        private void DismissStackedImmediate(bool notify, IReadOnlyList<FlyoutWindow> windows)
        {
            bool anyVisible = false;
            foreach (FlyoutWindow window in windows)
            {
                if (!window.IsFlyoutSessionShowing)
                {
                    continue;
                }

                anyVisible = true;
                window.TransientDismissWithoutNotify();
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
            return slotKey.EndsWith(":media", StringComparison.OrdinalIgnoreCase)
                ? TesseraStackedPanelRole.Media
                : slotKey.EndsWith(":device", StringComparison.OrdinalIgnoreCase)
                ? TesseraStackedPanelRole.Device
                : TesseraStackedPanelRole.Volume;
        }

        private void ApplyStackedPlacement(FlyoutRequest request)
        {
            if (_stackedSession is null || _stackedSession.SlotKeys.Count == 0)
            {
                return;
            }

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
            TesseraStackedSession? session = _stackedSession;
            if (session is null || session.SlotKeys.Count == 0)
            {
                return;
            }

            Dictionary<TesseraStackedPanelRole, FlyoutWindow> windowByRole = [];
            lock (_gate)
            {
                foreach (string slotKey in session.SlotKeys)
                {
                    if (!_windows.TryGetValue(slotKey, out FlyoutWindow? window))
                    {
                        continue;
                    }

                    windowByRole[RoleForSlotKey(slotKey)] = window;
                }
            }

            if (windowByRole.Count == 0)
            {
                return;
            }

            double flyoutScale = TesseraFlyoutRequestBuilder.FlyoutScaleFromPayload(request.Payload);
            Dictionary<TesseraStackedPanelRole, TesseraStackedPanelPlacement> estimates = TesseraStackedPlacementPolicy.ScalePlacements(
                    TesseraStackedPlacementPolicy.EstimatePlacements(request.StyleId),
                    flyoutScale)
                .ToDictionary(p => p.Role);

            double PanelWidth(FlyoutWindow w, TesseraStackedPanelRole role)
            {
                if (!estimates.TryGetValue(role, out TesseraStackedPanelPlacement est))
                {
                    return Math.Max(w.DesiredSize.Width, 1);
                }

                double raw = Math.Max(w.DesiredSize.Width, 0);
                double safe = TesseraStackedPlacementPolicy.SanitizeStackedPanelMeasure(est.WidthDip, raw);
                return safe > 0 ? Math.Max(est.WidthDip, safe) : est.WidthDip;
            }

            double PanelHeight(FlyoutWindow w, TesseraStackedPanelRole role)
            {
                if (!estimates.TryGetValue(role, out TesseraStackedPanelPlacement est))
                {
                    return Math.Max(w.DesiredSize.Height, 1);
                }

                double raw = Math.Max(w.DesiredSize.Height, 0);
                double safe = TesseraStackedPlacementPolicy.SanitizeStackedPanelMeasure(est.HeightDip, raw);
                return safe > 0 ? Math.Max(est.HeightDip, safe) : est.HeightDip;
            }

            _ = windowByRole.TryGetValue(TesseraStackedPanelRole.Volume, out FlyoutWindow? volumeWin);
            _ = windowByRole.TryGetValue(TesseraStackedPanelRole.Media, out FlyoutWindow? mediaWin);
            volumeWin ??= windowByRole.Values.First();
            mediaWin ??= windowByRole.Values.Last();

            IReadOnlyList<TesseraStackedPanelPlacement> placements = TesseraStackedPlacementPolicy.ScalePlacements(
                TesseraStackedPlacementPolicy.ComputePlacements(
                    request.StyleId,
                    PanelWidth(volumeWin, TesseraStackedPanelRole.Volume) / flyoutScale,
                    PanelHeight(volumeWin, TesseraStackedPanelRole.Volume) / flyoutScale,
                    PanelWidth(mediaWin, TesseraStackedPanelRole.Media) / flyoutScale,
                    PanelHeight(mediaWin, TesseraStackedPanelRole.Media) / flyoutScale),
                flyoutScale);

            (double combinedW, double combinedH) = TesseraStackedPlacementPolicy.ClusterSizeFromPlacements(placements);
            (double estimateW, double estimateH) = TesseraStackedPlacementPolicy.EstimateClusterSize(request.StyleId);
            estimateW *= flyoutScale;
            estimateH *= flyoutScale;
            combinedW = Math.Max(combinedW, estimateW);
            combinedH = Math.Max(combinedH, estimateH);
            List<Screen> screens = volumeWin.Screens?.All?.ToList() ?? [];
            Screen? screen = ResolveMonitorScreen(screens, request.MonitorIndex)
                         ?? volumeWin.Screens?.Primary;
            PixelRect area = screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);
            double scale = screen?.Scaling > 0.1 ? screen.Scaling : 1.0;
            int wPx = Math.Max(1, (int)Math.Ceiling(combinedW * scale));
            int hPx = Math.Max(1, (int)Math.Ceiling(combinedH * scale));
            (int clusterX, int clusterY) = FlyoutAnchor.Compute(
                area.X, area.Y, area.Width, area.Height,
                wPx, hPx,
                request.Anchor ?? "TL",
                request.XPad,
                request.YPad);

            TesseraFlyoutDiagnostics.Log(
                $"stacked cluster style={request.StyleId} combined={combinedW:F0}x{combinedH:F0} " +
                $"estimate={estimateW:F0}x{estimateH:F0} anchor={clusterX},{clusterY} scale={scale:F2}");

            foreach (string slotKey in session.SlotKeys)
            {
                FlyoutWindow? window;
                lock (_gate)
                {
                    if (!_windows.TryGetValue(slotKey, out window))
                    {
                        continue;
                    }
                }

                TesseraStackedPanelRole role = RoleForSlotKey(slotKey);
                TesseraStackedPanelPlacement placement = placements.First(p => p.Role == role);
                _ = estimates.TryGetValue(role, out TesseraStackedPanelPlacement estimate);
                double estW = estimate.Role == role ? estimate.WidthDip : placement.WidthDip;
                double estH = estimate.Role == role ? estimate.HeightDip : placement.HeightDip;
                double measuredW = PanelWidth(window, role);
                double measuredH = PanelHeight(window, role);
                window.ClusterOriginX = clusterX;
                window.ClusterOriginY = clusterY;
                window.PanelOffsetXDip = (int)Math.Round(placement.OffsetXDip);
                window.PanelOffsetYDip = (int)Math.Round(placement.OffsetYDip);
                double? priorW = window.StackedPanelWidthDip;
                double? priorH = window.StackedPanelHeightDip;
                if (priorW != placement.WidthDip || priorH != placement.HeightDip)
                {
                    window.UnlockStackedClientSize();
                }

                window.StackedPanelWidthDip = placement.WidthDip;
                window.StackedPanelHeightDip = placement.HeightDip;
                window.BackdropCornerRadiusDip = TesseraOsAcrylicStackedPolicy.ResolvePanelCornerRadiusDip(
                    request.StyleId,
                    role,
                    placement.WidthDip,
                    placement.HeightDip);

                window.FinishLayout();

                double clientW = double.IsNaN(window.Width) ? 0 : window.Width;
                double clientH = double.IsNaN(window.Height) ? 0 : window.Height;
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
            TesseraLiveBindings bindings,
            bool sessionAlreadyShowing = false)
        {
            TesseraFlyoutMaterial material = TesseraFlyoutMaterialFactory.FromPayload(
                request.Payload, request.StyleId, request.Kind);
            TesseraPalette.ApplyMaterial(material);

            bool osAcrylicEligible = true;
            bool settingsWantBlur = TesseraFlyoutRequestBuilder.BackdropBlurFromPayload(request.Payload);
            TesseraFlyoutGlassBinder.Binding glass = TesseraFlyoutGlassBinder.ApplyForLiveFlyout(settingsWantBlur, osAcrylicEligible);
            Log(
                $"stacked glass role={role} mode={glass.Mode} osAcrylic={glass.OsAcrylicEligible}");

            TesseraFlyoutViewModel vm = TesseraFlyoutViewModel.FromRequest(_services, request, _hostUi);
            string? accent = TesseraFlyoutRequestBuilder.AccentFromPayload(request.Payload);
            Control root = TesseraStackedPanelFactory.CreatePanel(
                request.StyleId ?? "Fluent",
                vm,
                role,
                bindings,
                accent,
                glass.UseEmbeddedPreview,
                sessionAlreadyShowing);
            double scale = FlyoutScaleFromPayload(request.Payload);
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
            {
                return;
            }

            SyncFocusDim(request);

            foreach (string? slotKey in _stackedSession!.SlotKeys
                         .OrderBy(k => TesseraOsAcrylicStackedPolicy.ZOrderRank(RoleForSlotKey(k))))
            {
                lock (_gate)
                {
                    if (!_windows.TryGetValue(slotKey, out FlyoutWindow? window))
                    {
                        continue;
                    }

                    window.FinishLayout();
                    RestackAboveDim(window);
                }
            }

            Log($"stacked presented slots={windows.Count} focusDim={TesseraFocusDimPolicy.EnabledFromPayload(request.Payload)}");
            ScheduleStackedOutsideClickArm(windows);
        }

        private void ScheduleStackedOutsideClickArm(IReadOnlyList<FlyoutWindow> windows)
        {
            if (TesseraFlyoutOutsideClickPolicy.PresentMustStopPriorWatcherBeforeRearm)
            {
                StopOutsideClickWatcher();
            }
            else
            {
                _outsideClickArm?.Stop();
            }

            _outsideClickArm = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
            List<FlyoutWindow> captured = [.. windows];
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
                if (_windows.TryGetValue(slotKey, out FlyoutWindow? current) && ReferenceEquals(current, window))
                {
                    _ = _windows.Remove(slotKey);
                }
            }

            if (_stackedSession is null)
            {
                return;
            }

            _ = _stackedSession.SlotKeys.Remove(slotKey);
            if (_stackedSession.SlotKeys.Count == 0)
            {
                _stackedSession = null;
                StopOutsideClickWatcher();
                CloseFocusDim();
                CancelDeferredPatch();
                _ = _session.Clear();
            }
        }

        private void CloseStackedSession()
        {
            if (_stackedSession is null)
            {
                return;
            }

            StopStackedAutoDismiss();
            _stackedDismissRequest = null;

            List<string> keys = [.. _stackedSession.SlotKeys];
            _stackedSession = null;
            bool hideFirst = TesseraStatusFlyoutPolicy.StackedToStatusMustHideSlotsBeforeStatusReveal;
            foreach (string key in keys)
            {
                lock (_gate)
                {
                    if (!_windows.Remove(key, out FlyoutWindow? w))
                    {
                        continue;
                    }

                    w.TransientDismissed -= OnFlyoutTransientDismissed;
                    w.SuppressAutoDismiss();
                    if (hideFirst)
                    {
                        try { w.TransientDismissWithoutNotify(); }
                        catch { /* ignore */ }
                    }

                    try { w.Close(); } catch { /* ignore */ }
                }
            }
        }

        private void CloseSingleTesseraWindowIfAny()
        {
            lock (_gate)
            {
                if (!_windows.Remove("Tessera", out FlyoutWindow? single))
                {
                    return;
                }

                // CapsLock→volume: cancel timer + detach before Close so a late Tick cannot
                // cascade into the stacked session via OnFlyoutTransientDismissed.
                single.TransientDismissed -= OnFlyoutTransientDismissed;
                if (TesseraOsAcrylicStackedPolicy.SupersededSingleHwndMustCancelDismissBeforeClose
                    || TesseraStatusFlyoutPolicy.SupersededStatusMustCancelDismissBeforeClose)
                {
                    single.SuppressAutoDismiss();
                }

                try { single.Close(); } catch { /* ignore */ }
            }
        }

        private void ShowOrUpdateSingleTessera(FlyoutRequest request, bool resetDismiss, bool allowLivePatch = true)
        {
            CloseStackedSession();
            ShowOrUpdateCoreSinglePath(request, resetDismiss, allowLivePatch);
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
            lock (_gate)
            {
                _ = _windows.TryGetValue("Tessera", out flyout);
            }

            if (flyout is null)
            {
                return;
            }

            flyout.TransientDismiss();
        }

        private bool IsStackedTesseraVisible()
        {
            if (_stackedSession is null)
            {
                return false;
            }

            lock (_gate)
            {
                return _stackedSession.SlotKeys.Any(k =>
                    _windows.TryGetValue(k, out FlyoutWindow? w) && w.IsFlyoutSessionShowing);
            }
        }
    }
}
