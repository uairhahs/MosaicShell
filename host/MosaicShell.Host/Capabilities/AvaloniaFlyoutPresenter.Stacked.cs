using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Host.Tiles.Tessera;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Services;

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

        /// <summary>Slot motion generations of the stacked exit in flight, read right after it began; null when none runs.</summary>
        private int[]? _stackedExitGenerations;

        private bool ShouldUseStackedNativeTranslucency(FlyoutRequest request)
        {
            return ModuleIds.IsTessera(request.ModuleId)
            && TesseraNativeTranslucencyStackedPolicy.UseMultiWindowFromPayload(
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

            IReadOnlyList<TesseraStackedPanelRole> panels = TesseraNativeTranslucencyStackedPolicy.ResolvePanels(request.Payload, request.StyleId);
            if (panels.Count == 0)
            {
                Log("stacked panels empty, falling back to single HWND");
                ShowOrUpdateSingleTessera(request, resetDismiss, allowLivePatch);
                return;
            }

            _stackedSession.Kind = request.Kind;
            _stackedSession.StyleId = request.StyleId;
            EnsureTesseraSession(request, TesseraFlyoutSessionMode.Stacked);
            RebuildStackedSlots(request, panels);
        }

        private bool TryPatchStacked(FlyoutRequest request, bool resetDismiss)
        {
            if (_stackedSession is null || _stackedSession.SlotKeys.Count == 0)
            {
                return false;
            }

            string volumeKey = TesseraNativeTranslucencyStackedPolicy.WindowSlotKey(ModuleIds.Tessera, TesseraStackedPanelRole.Volume);
            lock (_gate)
            {
                if (!_windows.TryGetValue(volumeKey, out FlyoutWindow? volumeWin)
                    || !(volumeWin.IsFlyoutSessionShowing || volumeWin.IsEntranceMotionInFlight))
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
            return true;
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

            string volumeKey = TesseraNativeTranslucencyStackedPolicy.WindowSlotKey(ModuleIds.Tessera, TesseraStackedPanelRole.Volume);
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

            if (volumeWin.IsEntranceMotionInFlight)
            {
                // Kind/style already matched above; a request landing mid-entrance must only
                // patch content, not replay PlayStackedShowAnimation - that restarts BeginMotion
                // on every stacked window (resets Position/Opacity) before the previous run
                // finishes, so a fast run of requests can perpetually restart and never complete.
                return volumeWin.TryApplyLive(request, _services, resetDismiss);
            }

            if (!volumeWin.TryApplyLive(request, _services, resetDismiss))
            {
                return false;
            }

            Log($"stacked revive kind={request.Kind} style={request.StyleId}");
            ApplyStackedPlacement(request);
            SyncFocusDim(request);
            FlyoutMotionSession.ArmEntranceStartPose(windows);

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
            IReadOnlyList<TesseraStackedPanelRole> panels)
        {
            // No resetDismiss param: WireStackedDismissCoordinator below unconditionally
            // stops+restarts the session dismiss clock on every rebuild, same as a fresh
            // single-window FlyoutWindow always arms its own timer on construction.
            bool reuseWasVisible = GetStackedWindowsFromSession().Any(w => w.IsFlyoutSessionShowing);
            TesseraLiveBindings bindings = _stackedSession!.Bindings;
            List<string> newKeys = [];
            List<FlyoutWindow> newWindows = [];

            foreach (TesseraStackedPanelRole role in panels)
            {
                string slotKey = TesseraNativeTranslucencyStackedPolicy.WindowSlotKey(ModuleIds.Tessera, role);
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
                        && TesseraNativeTranslucencyStackedPolicy.MustReuseRegisteredFlyoutHwndPerSlot)
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
            bool play = TesseraFlyoutLiveSyncPolicy.ShouldPlayShowAnimationAfterApplyRequest(
                reuseWasVisible, TesseraFlyoutWindowPolicy.HideUntilCompositionReady);
            if (play)
            {
                FlyoutMotionSession.ArmEntranceStartPose(newWindows);
            }

            foreach (FlyoutWindow window in newWindows)
            {
                if (!window.IsVisible)
                {
                    window.Show();
                }

                window.EnsureLivePump();
            }

            PresentStackedFlyout(request, newWindows);
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
            bool wasHovered = _stackedClusterHovered;
            UpdateStackedClusterHover();
            if (wasHovered && !_stackedClusterHovered && GetStackedWindowsFromSession().Any(static w => w.IsFlyoutSessionShowing))
            {
                // The pointer left the whole cluster: it goes soon after, not a full timer later. Crossing the gap
                // between two panels restarts the clock too, but the tick sees the cluster hovered again and waits.
                StartStackedAutoDismiss(TesseraFlyoutDismissPolicy.ResolvePointerLeaveDismissMs(_stackedDismissRequest?.AutoDismissMs ?? 0));
            }
        }

        private void UpdateStackedClusterHover()
        {
            _stackedClusterHovered = GetStackedWindowsFromSession().Any(w => w.IsFlyoutHovered);
        }

        private void StartStackedAutoDismiss()
        {
            StartStackedAutoDismiss(_stackedDismissRequest?.AutoDismissMs ?? 0);
        }

        private void StartStackedAutoDismiss(int ms)
        {
            StopStackedAutoDismiss();
            if (!TesseraFlyoutDismissCoordinator.ShouldArmSessionAutoDismiss(ms, _stackedSession is not null))
            {
                return;
            }

            _stackedAutoDismiss = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ms) };
            _stackedAutoDismiss.Tick += (_, _) =>
            {
                UpdateStackedClusterHover();
                if (_stackedClusterHovered || (_dismissLink?.IsHovered ?? false))
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
                // Before the play check: ApplyRequest cleared it, and a slot rebuilt while visible does not replay.
                window.PresenterDrivesMotion = TesseraNativeTranslucencyStackedPolicy.SlotMotionIsPresenterDriven(play);
            }

            if (!play)
            {
                return;
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

            if (!TesseraFlyoutDismissPolicy.ShouldBeginStackedDismiss(
                    _stackedExitGenerations, [.. windows.Select(static window => window.MotionGeneration)]))
            {
                Log("stacked dismiss ignored, its exit is already running");
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
            int gen = _session.Generation;
            _session.SetPhase(TesseraFlyoutPhase.Exiting);
            Task exit = RunStackedExitAnimationAsync(request, windows);
            // Read after the exit began (BeginMotion runs before its first await): a re-present supersedes these.
            int[] exitGenerations = [.. windows.Select(static window => window.MotionGeneration)];
            _stackedExitGenerations = exitGenerations;
            try
            {
                await exit.ConfigureAwait(true);
            }
            finally
            {
                if (ReferenceEquals(_stackedExitGenerations, exitGenerations))
                {
                    _stackedExitGenerations = null;
                }
            }

            int[] nowGenerations = [.. windows.Select(static window => window.MotionGeneration)];
            if (TesseraFlyoutDismissPolicy.ShouldFinishStackedTransientDismiss(
                    gen,
                    _session.Generation,
                    exitGenerations,
                    nowGenerations,
                    _session.Phase,
                    windows.Any(static window => window.IsVisible)))
            {
                _session.SetPhase(TesseraFlyoutPhase.Hidden);
                DismissStackedImmediate(notify, windows);
                return;
            }

            if (!exitGenerations.SequenceEqual(nowGenerations))
            {
                Log("stacked dismiss superseded by a present during the exit, slots stay");
            }
        }

        private void DismissStackedImmediate(bool notify, IReadOnlyList<FlyoutWindow> windows)
        {
            bool anyVisible = false;
            foreach (FlyoutWindow window in windows)
            {
                if (!window.IsVisible)
                {
                    continue;
                }

                anyVisible = true;
                window.TransientDismissWithoutNotify();
            }

            StopStackedAutoDismiss();

            if (notify && anyVisible)
            {
                try { TransientDismissed?.Invoke(ModuleIds.Tessera); }
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
                window.BackdropCornerRadiusDip = TesseraNativeTranslucencyStackedPolicy.ResolvePanelCornerRadiusDip(
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

            bool nativeTranslucencyEligible = true;
            bool settingsWantBlur = TesseraFlyoutRequestBuilder.BackdropBlurFromPayload(request.Payload);
            TesseraFlyoutGlassBinder.Binding glass = TesseraFlyoutGlassBinder.ApplyForLiveFlyout(settingsWantBlur, nativeTranslucencyEligible);
            Log(
                $"stacked glass role={role} mode={glass.Mode} nativeTranslucency={glass.NativeTranslucencyEligible}");

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

        private void PresentStackedFlyout(FlyoutRequest request, IReadOnlyList<FlyoutWindow> windows)
        {
            if (windows.Count == 0)
            {
                return;
            }

            SyncFocusDim(request);

            foreach (string? slotKey in _stackedSession!.SlotKeys
                         .OrderBy(k => TesseraNativeTranslucencyStackedPolicy.ZOrderRank(RoleForSlotKey(k))))
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

            Log($"stacked presented slots={windows.Count} focusDim={_focusDim is not null}");
            if (TesseraFlyoutDiagnostics.IsEnabled(DiagnosticLogLevel.Debug))
            {
                TesseraFlyoutDiagnostics.Log(
                    DiagnosticLogLevel.Debug, $"visible windows {Win32WindowChrome.DescribeVisibleProcessWindows()}");
            }
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
            _stackedExitGenerations = null;

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
                if (!_windows.Remove(ModuleIds.Tessera, out FlyoutWindow? single))
                {
                    return;
                }

                // CapsLock→volume: cancel timer + detach before Close so a late Tick cannot
                // cascade into the stacked session via OnFlyoutTransientDismissed.
                single.TransientDismissed -= OnFlyoutTransientDismissed;
                if (TesseraNativeTranslucencyStackedPolicy.SupersededSingleHwndMustCancelDismissBeforeClose
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
            // As FlyoutWindow.TransientDismissCore does: the clock stops when the dismiss begins, not when the exit ends,
            // or a timer due within the exit fires into it.
            StopStackedAutoDismiss();
            TransientDismissAllStackedSlots(notify);
            CloseFocusDim();
        }

        private void DismissTesseraSingleImmediate()
        {
            FlyoutWindow? flyout;
            lock (_gate)
            {
                _ = _windows.TryGetValue(ModuleIds.Tessera, out flyout);
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
