using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;
using MosaicShell.Host.Tiles.Tessera;
using MosaicShell.Core.Modules;

namespace MosaicShell.Host.Capabilities
{
    /// <summary>
    /// Tessera flyout surface. Configured per Avalonia window docs:
    /// TransparencyLevelHint + Transparent Background + TransparencyBackgroundFallback,
    /// SizeToContent, unowned Show(), Topmost, Screens for placement.
    /// </summary>
    internal sealed class FlyoutWindow : Window
    {
        private static int _nextWindowId;
        private long _motionStartTicks;
        private readonly HostServices _services;
        private TesseraFlyoutMaterial _material = null!;
        private DispatcherTimer? _dismiss;
        private DispatcherTimer? _live;
        private int _revealGeneration;
        private bool _autoDismissSuppressed;
        private Size _lastSize;
        private bool _clientSizeLocked;
        private bool _relayouting;
        private PixelPoint _restPosition;
        private bool _motionAnimating;
        private bool _motionEntrance;
        private bool _syncingRevealRegion;
        private double? _motionRegionProgress;
        private bool _suppressRegionClientRedraw;
        private double _signedRevealRestWidthDip;
        private double _signedRevealRestHeightDip;
        private int _stackedShowGeneration;
        private CancellationTokenSource _motionCts = new();
        private const int MotionFrameSamplerTailMs = 300;
        private const int MotionFrameSamplerMaxMs = 5000;
        private long _frameSamplerStart;
        private long _frameSamplerTailUntil;
        private string? _lastFrameSample;
        private bool _entranceStartPoseArmed;
        private bool _hasRestPosition;

        /// <summary>Fades with flyout content; acrylic HWND stays opaque until Hide.</summary>
        internal Panel MotionSurface { get; } = new();

        /// <summary>H3 stacked acrylic: cluster anchor in physical pixels; panel offset in DIP.</summary>
        public int? ClusterOriginX { get; set; }
        public int? ClusterOriginY { get; set; }
        public int PanelOffsetXDip { get; set; }
        public int PanelOffsetYDip { get; set; }

        /// <summary>H3 stacked acrylic: Win32 region clip radius in DIP (pill/card geometry).</summary>
        public double? BackdropCornerRadiusDip { get; set; }

        /// <summary>Stacked acrylic panel role (show phase 1 skips media; phase 2 still hits Animated meters).</summary>
        public TesseraStackedPanelRole? StackedRole { get; set; }

        /// <summary>When true, stacked presenter runs entrance/exit via parallel motion tasks.</summary>
        internal bool PresenterDrivesMotion { get; set; }

        internal bool Phase2Animating { get; set; }

        internal FlyoutRequest FlyoutRequest { get; private set; }

        /// <summary>
        /// Media-strip presence of the request the current content was built from. A live patch
        /// replaces <see cref="FlyoutRequest"/> without rebuilding, so the two can disagree (audit
        /// F02); the relayout trace logs both.
        /// </summary>
        private bool _builtShowMedia;

        /// <summary>H3 stacked acrylic: lock HWND client area to signed placement DIP sizes.</summary>
        public double? StackedPanelWidthDip { get; set; }
        public double? StackedPanelHeightDip { get; set; }

        public FlyoutWindow(FlyoutRequest request, Control content, HostServices services)
        {
            FlyoutRequest = request;
            _builtShowMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(request.Payload);
            WindowId = Interlocked.Increment(ref _nextWindowId);
            _services = services;
            ApplyFlyoutMaterial(TesseraFlyoutMaterialFactory.FromPayload(request.Payload, request.StyleId, request.Kind));
            // Win32 title is for HWND identity only, WindowDecorations.None; never a visible chrome strip.
            Title = $"MosaicShell - {request.ModuleId}";

            // docs: SizeToContent for content-sized tool windows
            SizeToContent = SizeToContent.WidthAndHeight;
            CanResize = false;
            WindowDecorations = WindowDecorations.None;
            Topmost = true;
            ShowInTaskbar = false;
            ShowActivated = false;
            Focusable = true;
            IsHitTestVisible = true;

            Content = MotionSurface;
            SetFlyoutContent(content);
            // Keep HWND composited; fade the motion surface (glass + controls) instead of Window.Opacity alone.
            Opacity = 1;
            MotionSurface.Opacity = TesseraFlyoutWindowPolicy.HideUntilCompositionReady ? 0 : 1;
            PointerEntered += (_, _) =>
            {
                IsFlyoutHovered = true;
                PointerHoverChanged?.Invoke();
                FlyoutUserActivity?.Invoke();
            };
            PointerExited += (_, _) =>
            {
                IsFlyoutHovered = false;
                PointerHoverChanged?.Invoke();
            };
            PointerWheelChanged += OnWheel;
            Opened += (_, _) =>
            {
                StartMotionFrameSampler("opened");
                Relayout();
                EnsureLivePump();
            };
            LayoutUpdated += OnLayoutUpdated;
            Closed += (_, _) => StopLivePump();
            ResetDismissTimer();
        }

        public string Kind => FlyoutRequest.Kind;
        public string? StyleId => FlyoutRequest.StyleId;
        internal int MotionGeneration { get; private set; }

        /// <summary>
        /// Explicit session lifecycle phase.
        /// </summary>
        public TesseraFlyoutPhase Phase { get; private set; } = TesseraFlyoutPhase.Hidden;

        private void SetPhase(TesseraFlyoutPhase value)
        {
            if (Phase != value)
            {
                Phase = value;
                PhaseChanged?.Invoke(this, value);
            }
        }

        public event Action<FlyoutWindow, TesseraFlyoutPhase>? PhaseChanged;

        public bool IsSessionActive => Phase.IsSessionActive();

        /// <summary>
        /// True only when the SoftFrost session is user-visible (not pre-reveal / transient-dismissed).
        /// </summary>
        public bool IsFlyoutSessionShowing =>
            TesseraFlyoutLiveSyncPolicy.IsEffectivelyShowing(IsVisible, MotionSurface.Opacity);

        /// <summary>Stable per-HWND id so log lines can be correlated across a reused surface.</summary>
        internal int WindowId { get; }

        /// <summary>
        /// Diagnostics-only view of the state that <see cref="IsFlyoutSessionShowing"/> collapses
        /// into one bool. Routing reads only that bool, so a decision taken mid-entrance is
        /// indistinguishable in the log from one taken while hidden unless the inputs are recorded
        /// alongside it.
        /// </summary>
        internal string MotionStateTag
        {
            get
            {
                string motion = !_motionAnimating
                    ? "none"
                    : _motionEntrance ? "entering" : "exiting";
                return $"w{WindowId} role={StackedRole?.ToString() ?? "single"} "
                    + $"phase={Phase} vis={(IsVisible ? 1 : 0)} op={MotionSurface.Opacity:0.###} "
                    + $"motion={motion} p2={(Phase2Animating ? 1 : 0)} gen={MotionGeneration} "
                    + $"showing={(IsFlyoutSessionShowing ? 1 : 0)}";
            }
        }

        /// <summary>
        /// True while this window's own entrance sequence is still running. Opacity ramps from 0,
        /// so IsFlyoutSessionShowing reads false for the whole entrance even though the window is
        /// not "closed" and does not need reviving. A same-kind/style Present that arrives during
        /// this window must patch content in place and let the in-flight entrance finish - treating
        /// it as not-showing restarts BeginMotion (resets Position/Opacity) before the previous run
        /// completes, which is why a rapid string of track-change Present calls looked choppy and
        /// never finished.
        /// </summary>
        internal bool IsEntranceMotionInFlight => _motionAnimating && _motionEntrance;

        private void SetFlyoutContent(Control content)
        {
            MotionSurface.Children.Clear();
            MotionSurface.Children.Add(content);
        }

        private void ResetRevealProgress()
        {
            bool showMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(FlyoutRequest.Payload);
            bool willRunPhase2 = ShouldRunPhase2Reveal(showMedia);
            double progress = TesseraFlyoutRevealSpec.ResolveHideRevealProgress(willRunPhase2);
            bool engaged = TesseraFlyoutRevealSpec.ResolveHidePhase2Engaged(willRunPhase2);
            foreach (TesseraRevealHost host in this.GetVisualDescendants().OfType<TesseraRevealHost>())
            {
                host.Phase2Engaged = engaged;
                host.RevealProgress = progress;
            }
        }

        internal void SyncStrokeBRegion()
        {
            SyncRevealRegion();
        }

        internal void SyncRevealRegion()
        {
            if (_syncingRevealRegion)
            {
                return;
            }

            bool showMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(FlyoutRequest.Payload);
            if (!TesseraFlyoutHwndRegionSpec.RevealOwnsWindowRegion(FlyoutRequest.StyleId, showMedia, StackedRole))
            {
                return;
            }

            _syncingRevealRegion = true;
            try
            {
                List<TesseraRevealHost> hosts = [.. this.GetVisualDescendants().OfType<TesseraRevealHost>()];
                bool forceRest = TesseraFlyoutHwndRegionSpec.ShouldForceRestRevealRegion(
                    _motionAnimating, Phase2Animating, IsFlyoutSessionShowing);
                if (forceRest)
                {
                    foreach (TesseraRevealHost host in hosts)
                    {
                        host.Phase2Engaged = true;
                        if (!TesseraFlyoutHwndRegionSpec.IsRestRevealProgress(host.RevealProgress))
                        {
                            host.RevealProgress = TesseraFlyoutRevealSpec.RestRevealProgress;
                        }
                    }
                }

                double progress = _motionRegionProgress
                               ?? TesseraFlyoutHwndRegionSpec.ResolveRegionProgress(
                                   hosts.ConvertAll(static h => h.RevealProgress),
                                   _motionAnimating,
                                   Phase2Animating,
                                   IsFlyoutSessionShowing);
                bool engaged = forceRest
                              || TesseraFlyoutHwndRegionSpec.IsRestRevealProgress(progress)
                              || hosts.Exists(static h => h.Phase2Engaged);

                double signedW = StackedPanelWidthDip ?? (_signedRevealRestWidthDip > 1 ? _signedRevealRestWidthDip : 0);
                double signedH = StackedPanelHeightDip ?? (_signedRevealRestHeightDip > 1 ? _signedRevealRestHeightDip : 0);
                double restW = TesseraFlyoutHwndRegionSpec.ResolveRestExtentDip(signedW, Bounds.Width);
                double restH = TesseraFlyoutHwndRegionSpec.ResolveRestExtentDip(signedH, Bounds.Height);
                double scale = ResolveMonitorScale();
                double radiusDip = BackdropCornerRadiusDip
                                ?? TesseraFlyoutHwndRegionSpec.ResolveSingleShellCornerRadiusDip(FlyoutRequest.StyleId);

                // Rest extents are measured (they include the flyout scale); the reveal math is in
                // layout units. The spec converts between the two so the region covers the card.
                (int WidthPx, int HeightPx, int CornerRadiusPx) = TesseraFlyoutHwndRegionSpec.ResolveWindowRegionPhysical(
                    FlyoutRequest.StyleId,
                    StackedRole,
                    progress,
                    engaged,
                    showMedia,
                    restW,
                    restH,
                    radiusDip,
                    TesseraFlyoutRequestBuilder.FlyoutScaleFromPayload(FlyoutRequest.Payload),
                    scale);

                if (StackedRole == TesseraStackedPanelRole.Media)
                {
                    Opacity = 1;
                }

                Win32Properties.SetWindowCornerPreference(
                    this,
                    Win32Properties.WindowCornerPreference.DoNotRound);
                if (TesseraFlyoutHwndRegionSpec.IsCollapsedWindowRegion(
                        FlyoutRequest.StyleId,
                        StackedRole,
                        progress,
                        engaged,
                        showMedia,
                        restW,
                        restH,
                        TesseraFlyoutRequestBuilder.FlyoutScaleFromPayload(FlyoutRequest.Payload)))
                {
                    Win32WindowChrome.ApplyEmptyRegion(this);
                    return;
                }

                Win32WindowChrome.ApplyRoundRectRegion(
                    this,
                    WidthPx,
                    HeightPx,
                    CornerRadiusPx,
                    applySynchronously: true,
                    redrawClient: !_suppressRegionClientRedraw);
            }
            finally
            {
                _syncingRevealRegion = false;
            }
        }

        /// <summary>Raised after auto-dismiss / TransientDismiss so Host can close FocusDim.</summary>
        public event Action<string>? TransientDismissed;

        /// <summary>Pointer enter or wheel on this flyout (stacked cluster dismiss coordination).</summary>
        public event Action? FlyoutUserActivity;

        /// <summary>Pointer entered or exited this flyout surface.</summary>
        public event Action? PointerHoverChanged;

        public bool IsFlyoutHovered { get; private set; }

        /// <summary>H3 stacked acrylic: only the presenter runs auto-dismiss for the cluster.</summary>
        public void SuppressAutoDismiss()
        {
            _autoDismissSuppressed = true;
            _dismiss?.Stop();
        }

        public void EnsureLivePump()
        {
            StartLivePump();
        }

        private void StartLivePump()
        {
            if (!ModuleIds.IsTessera(FlyoutRequest.ModuleId))
            {
                return;
            }

            if (_live is not null)
            {
                return;
            }

            _live = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _live.Tick += (_, _) =>
            {
                try
                {
                    if (FlyoutRequest.Kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
                        || FlyoutRequest.Kind.Equals("flight", StringComparison.OrdinalIgnoreCase))
                    {
                        RefreshStatusFromServices();
                        return;
                    }

                    // Timeline UI: ApplyLive only. PumpTimeline runs from armed poll / user events.
                    if (Content is Control root && TesseraLiveHost.FindIn(root) is { } host)
                    {
                        host.ApplyLive(_services, FlyoutRequest);
                    }

                    if (TesseraFlyoutHwndRegionSpec.ShouldForceRestRevealRegion(
                            _motionAnimating, Phase2Animating, IsFlyoutSessionShowing))
                    {
                        SyncRevealRegion();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Tessera live] {ex.Message}");
                }
            };
            _live.Start();
        }

        private void StopLivePump()
        {
            _live?.Stop();
            _live = null;
        }

        private void RefreshStatusFromServices()
        {
            Dictionary<string, string> payload = TesseraFlyoutRequestBuilder.RefreshStatusPayload(
                _services, FlyoutRequest.Kind, FlyoutRequest.Payload);
            string? prevOn = FlyoutRequest.Payload?.GetValueOrDefault("on");
            string? nextOn = payload.GetValueOrDefault("on");
            string? prevLock = FlyoutRequest.Payload?.GetValueOrDefault("lock");
            string? nextLock = payload.GetValueOrDefault("lock");
            if (prevOn == nextOn && prevLock == nextLock)
            {
                return;
            }

            FlyoutRequest = FlyoutRequest with { Payload = payload };
            if (Content is Control root && TesseraLiveHost.FindIn(root) is { } host)
            {
                host.ApplyLive(_services, FlyoutRequest);
            }
        }

        public void ApplyLiveOnly(FlyoutRequest request, HostServices services)
        {
            FlyoutRequest = request;
            if (FlyoutRequest.Kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
                || FlyoutRequest.Kind.Equals("flight", StringComparison.OrdinalIgnoreCase))
            {
                RefreshStatusFromServices();
                return;
            }
            if (Content is Control root && TesseraLiveHost.FindIn(root) is { } host)
            {
                host.ApplyLive(services, FlyoutRequest);
            }
        }

        public bool TryApplyLive(FlyoutRequest request, HostServices services, bool resetDismiss = true)
        {
            if (!ModuleIds.IsTessera(request.ModuleId))
            {
                return false;
            }

            if (!string.Equals(FlyoutRequest.Kind, request.Kind, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.Equals(FlyoutRequest.StyleId ?? "", request.StyleId ?? "", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // The scale wrapper (LayoutTransformControl) is baked in at content-build time and
            // isn't touched by a live patch, so a scale change must fall through to a full rebuild.
            if (TesseraFlyoutRequestBuilder.FlyoutScaleFromPayload(FlyoutRequest.Payload)
                != TesseraFlyoutRequestBuilder.FlyoutScaleFromPayload(request.Payload))
            {
                return false;
            }

            if (Content is not Control root)
            {
                return false;
            }

            if (TesseraLiveHost.FindIn(root) is not { } liveHost)
            {
                return false;
            }

            // Do NOT force a full rebuild when media-strip presence flips mid-volume.
            // Rebuild + PresentFlyout (Win32 restack ×3) on every Audio.Changed freezes the app.
            // Strip structure can catch up on the next cold Show.
            liveHost.ApplyLive(services, request);
            // TesseraPalette is read live at paint/render time (TesseraGlassPanel.DrawGlassChrome,
            // TesseraTrack.ApplyFillBrush), so re-applying it here (unlike the scale wrapper) keeps
            // acrylic/backdrop-blur/accent settings in sync on a patch without needing a rebuild.
            TesseraFlyoutMaterial freshMaterial = TesseraFlyoutMaterialFactory.FromPayload(
                request.Payload, request.StyleId, request.Kind);
            TesseraPalette.ApplyMaterial(freshMaterial);

            // Diagnostic (2026-09-07): TryApplyLive never calls ApplyFlyoutMaterial, so this
            // window's own TransparencyLevelHint/Background can go stale relative to what content
            // now assumes via TesseraPalette above. Logging whether the hints actually differ
            // pins down whether that gap is the cause of MaterialYou's reported inconsistent
            // background, rather than patching on a guess.
            if (!_material.TransparencyHints.SequenceEqual(freshMaterial.TransparencyHints, StringComparer.OrdinalIgnoreCase)
                || _material.ShellAlpha != freshMaterial.ShellAlpha)
            {
                TesseraFlyoutDiagnostics.Log(
                    $"TryApplyLive material drift w{WindowId} style={request.StyleId} kind={request.Kind} "
                    + $"windowHints=[{string.Join(',', _material.TransparencyHints)}] windowAlpha={_material.ShellAlpha} "
                    + $"freshHints=[{string.Join(',', freshMaterial.TransparencyHints)}] freshAlpha={freshMaterial.ShellAlpha} "
                    + $"liveHint={string.Join(',', TransparencyLevelHint)}");
            }
            string? priorAccent = TesseraFlyoutRequestBuilder.AccentFromPayload(FlyoutRequest.Payload);
            string? nextAccent = TesseraFlyoutRequestBuilder.AccentFromPayload(request.Payload);
            if (!string.Equals(priorAccent, nextAccent, StringComparison.OrdinalIgnoreCase))
            {
                // Gated on change: the fallback branch does a DwmGetColorizationColor syscall,
                // and TryApplyLive runs on every volume/media tick, not just settings changes.
                TesseraPalette.ApplyAccentFromSettings(nextAccent);
            }

            FlyoutRequest = request;
            if (resetDismiss)
            {
                ResetDismissTimer();
            }

            return true;
        }

        public void ApplyRequest(FlyoutRequest request, Control content)
        {
            bool wasShowing = IsFlyoutSessionShowing;
            if (_motionAnimating)
            {
                // Full rebuild over a live animation. This resets SizeToContent/Width/Height and
                // relayouts, so the surface can repaint at a stale or partial size mid-reveal.
                TesseraFlyoutDiagnostics.Log(
                    $"MOTION INTERRUPTED by ApplyRequest {MotionStateTag} wasShowing={wasShowing} "
                    + $"kind={FlyoutRequest.Kind}->{request.Kind} style={request.StyleId}");
            }

            FlyoutRequest = request;
            _builtShowMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(request.Payload);
            ApplyFlyoutMaterial(TesseraFlyoutMaterialFactory.FromPayload(request.Payload, request.StyleId, request.Kind));
            // Invalidate any posted SoftFrost reveal from the previous surface.
            _revealGeneration++;
            SupersedeMotion();
            _entranceStartPoseArmed = false;
            if (TesseraFlyoutAnimationPolicy.MotionAnimatingMustClearOnSupersede)
            {
                _motionAnimating = false;
            }

            Win32WindowChrome.ClearWindowRegion(this);
            PresenterDrivesMotion = false;
            RenderTransform = null;
            Opacity = 1;
            bool hideUntilReady = TesseraFlyoutWindowPolicy.HideUntilCompositionReady;
            if (TesseraFlyoutLiveSyncPolicy.ShouldZeroMotionSurfaceOnApplyRequest(wasShowing, hideUntilReady))
            {
                MotionSurface.Opacity = 0;
            }
            else if (wasShowing)
            {
                MotionSurface.Opacity = 1;
            }

            SetFlyoutContent(content);
            if (TesseraFlyoutLiveSyncPolicy.ShouldSnapRevealToRestAfterApplyRequest(wasShowing))
            {
                ApplyPhase2Reveal(TesseraFlyoutRevealSpec.RestRevealProgress);
            }

            _lastSize = default;
            // Media/stacked → CapsLock: drop cluster placement and stale Win32 region.
            ClusterOriginX = null;
            ClusterOriginY = null;
            PanelOffsetXDip = 0;
            PanelOffsetYDip = 0;
            StackedPanelWidthDip = null;
            StackedPanelHeightDip = null;
            _signedRevealRestWidthDip = 0;
            _signedRevealRestHeightDip = 0;
            _clientSizeLocked = false;
            SizeToContent = SizeToContent.WidthAndHeight;
            Width = double.NaN;
            Height = double.NaN;
            MaxWidth = double.PositiveInfinity;
            MaxHeight = double.PositiveInfinity;
            if (TesseraStatusFlyoutPolicy.IsStatusKind(request.Kind))
            {
                BackdropCornerRadiusDip =
                    TesseraStatusFlyoutPolicy.ResolveChipCornerRadiusDip(request.StyleId);
                MaxWidth = TesseraStatusFlyoutPolicy.ChipMaxWidthDip;
                MaxHeight = TesseraStatusFlyoutPolicy.ChipMaxHeightDip;
            }

            ResetDismissTimer();
            Relayout();
            EnsureLivePump();
        }

        /// <summary>Allow stacked relayout to remeasure when placement floors change.</summary>
        public void UnlockStackedClientSize()
        {
            _clientSizeLocked = false;
        }

        private bool _exitAnimating;

        /// <summary>
        /// SoftFrost: reveal after layout with optional slide + eased opacity (settings Motion page).
        /// Generation-gated so rapid ApplyRequest does not apply a stale entrance.
        /// </summary>
        public void RevealAfterLayout()
        {
            SetPhase(TesseraFlyoutPhase.Entering);
            void Run()
            {
                if (!IsVisible)
                {
                    return;
                }

                RunEntranceAnimation();
            }

            if (!TesseraFlyoutWindowPolicy.HideUntilCompositionReady)
            {
                Run();
                return;
            }

            int generation = _revealGeneration;
            // One Loaded + one Render frame so SoftFrost composition settles before motion starts.
            Dispatcher.UIThread.Post(() =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (!IsVisible)
                    {
                        return;
                    }

                    if (TesseraFlyoutWindowPolicy.RevealMustBeGenerationGated
                        && generation != _revealGeneration)
                    {
                        return;
                    }

                    RunEntranceAnimation();
                }, DispatcherPriority.Render);
            }, DispatcherPriority.Loaded);
        }

        /// <summary>Sync layout then reveal (stacked + single Present paths).</summary>
        public void PlayShowAnimation()
        {
            try
            {
                SetPhase(TesseraFlyoutPhase.Entering);
                FlyoutMotionSession.ArmEntranceStartPose([this]);
                FinishLayout();
                RevealAfterLayout();
            }
            catch
            {
                MotionSurface.Opacity = 1;
                RenderTransform = null;
                Position = _restPosition;
            }
        }

        private void EnsureStatusRoundClipBeforeReveal()
        {
            if (!TesseraStatusFlyoutPolicy.IsStatusKind(FlyoutRequest.Kind)
                || !TesseraStatusFlyoutPolicy.MustRoundClipHwndBeforeReveal)
            {
                return;
            }

            if (BackdropCornerRadiusDip is not { } radiusDip || radiusDip <= 0)
            {
                BackdropCornerRadiusDip = TesseraStatusFlyoutPolicy.ResolveChipCornerRadiusDip(FlyoutRequest.StyleId);
            }

            (double dipW, double dipH) = TesseraStatusFlyoutPolicy.ResolveStatusClientSizeDip(
                Bounds.Width, Bounds.Height, DesiredSize.Width, DesiredSize.Height);
            if (!TesseraFlyoutWindowPolicy.MeetsRelayoutSizeGate(dipW, dipH, stackedClusterPanel: false))
            {
                return;
            }

            double scale = Screens?.ScreenFromWindow(this)?.Scaling
                        ?? Screens?.Primary?.Scaling
                        ?? 1.0;
            if (scale < 0.1)
            {
                scale = 1.0;
            }

            int w = Math.Max(1, (int)Math.Ceiling(dipW * scale));
            int h = Math.Max(1, (int)Math.Ceiling(dipH * scale));
            ApplyStackedBackdropClip(w, h, scale);
        }

        private void OnLayoutUpdated(object? sender, EventArgs e)
        {
            if (_relayouting || TesseraFlyoutAnimationPolicy.ShouldDeferRelayoutDuringMotion(_motionAnimating, Phase2Animating))
            {
                return;
            }

            Size s = Bounds.Size;
            if (s.Width < 2 || s.Height < 2)
            {
                return;
            }

            if (ClusterOriginX is not null
                && _clientSizeLocked
                && !double.IsNaN(Width)
                && !double.IsNaN(Height)
                && Math.Abs(s.Width - Width) < 0.5
                && Math.Abs(s.Height - Height) < 0.5)
            {
                return;
            }

            if (Math.Abs(s.Width - _lastSize.Width) < 0.5 && Math.Abs(s.Height - _lastSize.Height) < 0.5)
            {
                return;
            }

            _lastSize = s;
            RelayoutImmediate();
        }

        public void Relayout()
        {
            Dispatcher.UIThread.Post(RelayoutImmediate, DispatcherPriority.Loaded);
        }

        public void FinishLayout()
        {
            RelayoutImmediate();
        }

        private void RelayoutImmediate()
        {
            if (_relayouting)
            {
                return;
            }

            if (TesseraFlyoutAnimationPolicy.ShouldDeferRelayoutDuringMotion(_motionAnimating, Phase2Animating))
            {
                return;
            }

            _relayouting = true;
            long relayoutSpan = TesseraFlyoutDiagnostics.BeginSpan();
            try
            {
                InvalidateMeasure();
                UpdateLayout();

                double dipW = Math.Max(Bounds.Width, DesiredSize.Width);
                double dipH = Math.Max(Bounds.Height, DesiredSize.Height);
                bool stackedCluster = ClusterOriginX is not null && ClusterOriginY is not null;
                bool statusChip = TesseraStatusFlyoutPolicy.IsStatusKind(FlyoutRequest.Kind)
                    && TesseraStatusFlyoutPolicy.ForbidFixedVolumeShellSize;
                if (statusChip)
                {
                    (dipW, dipH) = TesseraStatusFlyoutPolicy.ResolveStatusClientSizeDip(
                        Bounds.Width, Bounds.Height, DesiredSize.Width, DesiredSize.Height);
                }

                if (!TesseraFlyoutWindowPolicy.MeetsRelayoutSizeGate(dipW, dipH, stackedCluster))
                {
                    return;
                }

                if (stackedCluster
                    && StackedPanelWidthDip is { } panelW
                    && StackedPanelHeightDip is { } panelH)
                {
                    MaxWidth = double.PositiveInfinity;
                    MaxHeight = double.PositiveInfinity;

                    if (!_clientSizeLocked)
                    {
                        SizeToContent = SizeToContent.WidthAndHeight;
                        Width = double.NaN;
                        Height = double.NaN;
                        InvalidateMeasure();
                        UpdateLayout();
                    }

                    // Bounds can inflate after HWND resize (LayoutTransformControl feedback loop).
                    double measuredW = Math.Max(DesiredSize.Width, 0);
                    double measuredH = Math.Max(DesiredSize.Height, 0);
                    measuredW = TesseraStackedPlacementPolicy.SanitizeStackedPanelMeasure(panelW, measuredW);
                    measuredH = TesseraStackedPlacementPolicy.SanitizeStackedPanelMeasure(panelH, measuredH);
                    (double clientW, double clientH) = TesseraStackedPlacementPolicy.ResolveStackedClientSize(
                        panelW,
                        panelH,
                        measuredW,
                        measuredH);

                    if (!_clientSizeLocked
                        || Math.Abs(Width - clientW) > 0.5
                        || Math.Abs(Height - clientH) > 0.5)
                    {
                        Width = clientW;
                        Height = clientH;
                        SizeToContent = SizeToContent.Manual;
                        _clientSizeLocked = true;
                    }

                    dipW = clientW;
                    dipH = clientH;
                }
                else if (_material.ShouldLockClientSize && !_clientSizeLocked)
                {
                    Width = dipW;
                    Height = dipH;
                    SizeToContent = SizeToContent.Manual;
                    _clientSizeLocked = true;
                }

                // docs: Screens API for placement; Position is pixel coordinates
                List<Screen> screens = Screens?.All?.ToList() ?? [];
                Screen? screen = ResolveScreen(screens, FlyoutRequest.MonitorIndex) ?? Screens?.Primary;
                PixelRect area = screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);
                double scale = screen?.Scaling > 0.1 ? screen.Scaling : (Screens?.Primary?.Scaling ?? 1.0);
                int w = Math.Max(1, (int)Math.Ceiling(dipW * scale));
                int h = Math.Max(1, (int)Math.Ceiling(dipH * scale));
                if (TesseraFlyoutDiagnostics.IsEnabled(DiagnosticLogLevel.Debug))
                {
                    // A2 (audit F02, H4): sizes that feed the region, and whether the built content
                    // still matches the request after live patches.
                    bool requestedShowMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(FlyoutRequest.Payload);
                    TesseraFlyoutDiagnostics.Log(
                        DiagnosticLogLevel.Debug,
                        $"relayout w{WindowId} kind={FlyoutRequest.Kind} style={FlyoutRequest.StyleId} " +
                        $"bounds={Bounds.Width:0.#}x{Bounds.Height:0.#} desired={DesiredSize.Width:0.#}x{DesiredSize.Height:0.#} " +
                        $"client={dipW:0.#}x{dipH:0.#}dip {w}x{h}px scale={scale:0.##} " +
                        $"showMedia requested={requestedShowMedia} built={_builtShowMedia}" +
                        (requestedShowMedia != _builtShowMedia ? " DIVERGED" : ""));
                }

                int xPad = Math.Clamp(FlyoutRequest.XPad, 0, 200);
                int yPad = Math.Clamp(FlyoutRequest.YPad, 0, 200);

                if (ClusterOriginX is int clusterX && ClusterOriginY is int clusterY)
                {
                    PixelPoint pt = new(
                        clusterX + (int)Math.Round(PanelOffsetXDip * scale),
                        clusterY + (int)Math.Round(PanelOffsetYDip * scale));
                    CommitRestPosition(pt);
                    ApplyStackedBackdropClip(w, h, scale);
                    SyncStrokeBRegion();
                    _lastSize = Bounds.Size;
                    return;
                }

                (int x, int y) = FlyoutAnchor.Compute(
                    area.X, area.Y, area.Width, area.Height,
                    w, h,
                    FlyoutRequest.Anchor ?? "TL",
                    xPad,
                    yPad);
                CommitRestPosition(new PixelPoint(x, y));
                ApplyStackedBackdropClip(w, h, scale);
                SyncRevealRegion();
                _lastSize = Bounds.Size;
            }
            catch (Exception ex)
            {
                TesseraFlyoutDiagnostics.LogException("[Tessera position]", ex);
            }
            finally
            {
                _relayouting = false;
                TesseraFlyoutDiagnostics.EndSpan(relayoutSpan, $"relayout w{WindowId}");
            }
        }

        private void ApplyStackedBackdropClip(int widthPx, int heightPx, double scale)
        {
            if (BackdropCornerRadiusDip is not { } radiusDip || radiusDip <= 0)
            {
                return;
            }

            bool status = TesseraStatusFlyoutPolicy.IsStatusKind(FlyoutRequest.Kind);
            bool acrylic = UsesOsAcrylicBackdrop();
            if (!acrylic
                && !(status && TesseraStatusFlyoutPolicy.SoftFrostMustRoundClipHwnd))
            {
                return;
            }

            Win32Properties.SetWindowCornerPreference(
                this,
                Win32Properties.WindowCornerPreference.DoNotRound);
            // The card radius is in layout units, so it scales with the flyout scale like the card.
            int radiusPx = TesseraFlyoutHwndRegionSpec.ResolveCornerRadiusPx(
                radiusDip,
                TesseraFlyoutRequestBuilder.FlyoutScaleFromPayload(FlyoutRequest.Payload),
                scale);
            TesseraBackdropClipWrite write = TesseraFlyoutHwndRegionSpec.ResolveBackdropClipWrite(
                FlyoutRequest.StyleId,
                TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(FlyoutRequest.Payload),
                StackedRole,
                status);
            if (write == TesseraBackdropClipWrite.None)
            {
                return;
            }

            Win32WindowChrome.ApplyRoundRectRegion(this, widthPx, heightPx, radiusPx, applySynchronously: true);
        }

        /// <summary>Phase 1 slide on the HWND: rest position plus an offset in physical pixels.</summary>
        internal void ApplySlideOffsetPx(double dx, double dy)
        {
            Position = new PixelPoint(
                _restPosition.X + (int)Math.Round(dx),
                _restPosition.Y + (int)Math.Round(dy));
        }

        internal bool UsesOsAcrylicBackdrop()
        {
            return _material.TransparencyHints.Any(static hint =>
                hint.Equals("AcrylicBlur", StringComparison.OrdinalIgnoreCase));
        }

        private static Screen? ResolveScreen(IReadOnlyList<Screen> screens, int monitorIndexOneBased)
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

        private void CommitRestPosition(PixelPoint rest)
        {
            _restPosition = rest;
            _hasRestPosition = true;
            if (!_motionAnimating)
            {
                Position = _entranceStartPoseArmed ? EntranceStartPosition() : rest;
            }
        }

        /// <summary>
        /// The window will run a phase 1 entrance that slides the window itself: hold it at the
        /// slide's start pose from now on (including relayouts before motion begins), so it is never
        /// shown at rest first. Shown at rest, the pane jumped to the start offset when phase 1 began
        /// and slid back, which read as a jiggle. Cleared when motion begins, on a rebuild and on hide.
        /// </summary>
        internal void ArmEntranceStartPose()
        {
            if (ResolveWindowSlideStartOffsetPx() == (0, 0))
            {
                return;
            }

            _entranceStartPoseArmed = true;
            if (!_motionAnimating && _hasRestPosition)
            {
                Position = EntranceStartPosition();
            }
        }

        internal (int Dx, int Dy) ResolveWindowSlideStartOffsetPx()
        {
            return TesseraFlyoutAnimationPolicy.ResolveWindowEntranceStartOffsetPx(
                FlyoutRequest.Ani, FlyoutRequest.AniDir, FlyoutRequest.AnimationDisplacement, UsesOsAcrylicBackdrop());
        }

        private PixelPoint EntranceStartPosition()
        {
            (int dx, int dy) = ResolveWindowSlideStartOffsetPx();
            return new PixelPoint(_restPosition.X + dx, _restPosition.Y + dy);
        }

        /// <summary>
        /// Debug only. One line per rendered frame, written only when something changed, from the
        /// window opening (or a motion run starting) until 300 ms after the motion ends, 5 s at most.
        /// Records what Avalonia thinks (translate, position, bounds, opacity) beside what the OS has
        /// (frame rectangle, region, layered alpha). Added for Modern Flyouts shaking while it slides
        /// in on a cold summon, which the per-write region and phase 2 lines could not explain.
        /// </summary>
        private void StartMotionFrameSampler(string reason)
        {
            if (!TesseraFlyoutDiagnostics.IsEnabled(DiagnosticLogLevel.Debug))
            {
                return;
            }

            _frameSamplerTailUntil = Environment.TickCount64 + MotionFrameSamplerTailMs;
            if (_frameSamplerStart != 0)
            {
                return;
            }

            _frameSamplerStart = Environment.TickCount64;
            _lastFrameSample = null;
            TesseraFlyoutDiagnostics.Log(
                DiagnosticLogLevel.Debug, $"motion frames start w{WindowId} reason={reason} {MotionStateTag}");
            RequestAnimationFrame(SampleMotionFrame);
        }

        private void SampleMotionFrame(TimeSpan _)
        {
            long now = Environment.TickCount64;
            long elapsed = now - _frameSamplerStart;
            if (_motionAnimating)
            {
                _frameSamplerTailUntil = now + MotionFrameSamplerTailMs;
            }

            string translate = RenderTransform is TranslateTransform tt ? $"{tt.X:0.#},{tt.Y:0.#}" : "-";
            string motion = !_motionAnimating ? "none" : _motionEntrance ? "entering" : "exiting";
            string sample =
                $"tt={translate} pos={Position.X},{Position.Y} bounds={Bounds.Width:0.#}x{Bounds.Height:0.#} " +
                $"op={Opacity:0.##}/{MotionSurface.Opacity:0.##} motion={motion} p2={(Phase2Animating ? 1 : 0)} " +
                Win32WindowChrome.DescribeNativeFrame(this);
            if (!string.Equals(sample, _lastFrameSample, StringComparison.Ordinal))
            {
                _lastFrameSample = sample;
                TesseraFlyoutDiagnostics.Log(
                    DiagnosticLogLevel.Debug,
                    $"motion frame w{WindowId} role={StackedRole?.ToString() ?? "single"} t={elapsed} {sample}");
            }

            if (!IsVisible || now > _frameSamplerTailUntil || elapsed > MotionFrameSamplerMaxMs)
            {
                TesseraFlyoutDiagnostics.Log(DiagnosticLogLevel.Debug, $"motion frames stop w{WindowId} t={elapsed}");
                _frameSamplerStart = 0;
                return;
            }

            RequestAnimationFrame(SampleMotionFrame);
        }

        private Task RunEntranceAnimationAsync()
        {
            return PresenterDrivesMotion ? Task.CompletedTask : FlyoutMotionSession.RunShowAsync([this]);
        }

        /// <summary>
        /// Fire-and-forget entry point for callers that can't await (Dispatcher.Post lambdas).
        /// RunShowAsync already catches everything it can attribute to a specific window/step;
        /// this continuation only guards against something escaping that, so it never becomes
        /// an unobserved fault on this window's Task instead of a silent crash.
        /// </summary>
        private void RunEntranceAnimation()
        {
            _ = RunEntranceAnimationAsync().ContinueWith(
                static t => TesseraFlyoutDiagnostics.Log($"entrance animation faulted: {t.Exception}"),
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        internal double ResolveMonitorScale()
        {
            return Screens?.ScreenFromWindow(this)?.Scaling
            ?? Screens?.Primary?.Scaling
            ?? 1.0;
        }

        internal bool ShouldRunPhase2Reveal(bool showMediaStrip)
        {
            return TesseraFlyoutAnimationPolicy.ShouldRunPhase2Reveal(
                FlyoutRequest.Ani, FlyoutRequest.StyleId, showMediaStrip, StackedRole);
        }

        internal void BeginMotion(bool entrance)
        {
            if (_motionAnimating)
            {
                // The previous sequence never reached CompleteMotion. Each occurrence is one
                // visible restart: BeginMotion re-seeds Position and (for entrance) opacity/reveal
                // from the start pose, so the run in flight is discarded part-way.
                TesseraFlyoutDiagnostics.Log(
                    $"MOTION RESTART begin(entrance={entrance}) over in-flight {MotionStateTag} "
                    + $"kind={FlyoutRequest.Kind} style={FlyoutRequest.StyleId}");
            }

            _motionStartTicks = Environment.TickCount64;
            StartMotionFrameSampler(entrance ? "entrance" : "exit");
            EnsureStatusRoundClipBeforeReveal();
            // An armed entrance keeps its start pose (phase 1 begins there); anything else re-seeds
            // from rest.
            Position = entrance && _entranceStartPoseArmed ? EntranceStartPosition() : _restPosition;
            _entranceStartPoseArmed = false;
            _motionEntrance = entrance;
            if (entrance)
            {
                SetPhase(TesseraFlyoutPhase.Entering);
                _exitAnimating = false;
            }
            else
            {
                SetPhase(TesseraFlyoutPhase.Exiting);
            }
            (int generation, CancellationToken _) = BeginMotionRun();
            _stackedShowGeneration = generation;

            if (!TesseraFlyoutAnimationPolicy.ShouldAnimateOpacity(FlyoutRequest.Ani))
            {
                if (entrance)
                {
                    SetPhase(TesseraFlyoutPhase.Shown);
                    Opacity = 1;
                    MotionSurface.Opacity = 1;
                    RenderTransform = null;
                    ApplyPhase2Reveal(1);
                }
                else
                {
                    SetPhase(TesseraFlyoutPhase.Hidden);
                    MotionSurface.Opacity = 0;
                    RenderTransform = null;
                }

                return;
            }

            _motionAnimating = true;
            bool showMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(FlyoutRequest.Payload);
            if (entrance)
            {
                bool willRunPhase2 = ShouldRunPhase2Reveal(showMedia);
                bool wipe = TesseraFlyoutHwndRegionSpec.ShouldWipeShowRegionOverRestLayout(
                    entrance: true, StackedRole, willRunPhase2);
                ApplyPhase2Reveal(TesseraFlyoutHwndRegionSpec.ResolveShowLayoutRevealProgress(wipe));
                if (wipe)
                {
                    _motionRegionProgress = TesseraFlyoutRevealSpec.RestRevealProgress;
                    _suppressRegionClientRedraw = false;
                    SyncRevealRegion();
                }

                if (TesseraFlyoutAnimationPolicy.ShouldHoldStackedMediaHiddenThroughShowPhase1(
                        FlyoutRequest.Ani, FlyoutRequest.StyleId, showMedia, StackedRole))
                {
                    Opacity = 0;
                    MotionSurface.Opacity = 0;
                }
            }
            else if (TesseraFlyoutAnimationPolicy.Phase2HideMustStartFromRestReveal)
            {
                ApplyPhase2Reveal(TesseraFlyoutRevealSpec.RestRevealProgress);
                SyncRevealRegion();
            }
        }

        internal void PreparePhase2Show()
        {
            if (_stackedShowGeneration != MotionGeneration || !_motionAnimating)
            {
                return;
            }

            bool showMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(FlyoutRequest.Payload);
            bool willRunPhase2 = ShouldRunPhase2Reveal(showMedia);
            bool wipe = TesseraFlyoutHwndRegionSpec.ShouldWipeShowRegionOverRestLayout(
                entrance: true, StackedRole, willRunPhase2);

            if (wipe)
            {
                ApplyPhase2Reveal(TesseraFlyoutHwndRegionSpec.ResolveShowLayoutRevealProgress(true));
                _motionRegionProgress = TesseraFlyoutRevealSpec.RestRevealProgress;
                _suppressRegionClientRedraw = false;
                SyncRevealRegion();
                return;
            }

            if (TesseraFlyoutAnimationPolicy.PreparePhase2ShowMustUnhideStackedMedia)
            {
                Opacity = 1;
                MotionSurface.Opacity = 1;
            }

            ApplyPhase2Reveal(TesseraFlyoutRevealSpec.ResolveShowRevealProgress(willRunPhase2));
        }

        internal void PreparePhase2ShowArmWipe()
        {
            if (_stackedShowGeneration != MotionGeneration || !_motionAnimating)
            {
                return;
            }

            bool showMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(FlyoutRequest.Payload);
            bool willRunPhase2 = ShouldRunPhase2Reveal(showMedia);
            if (!TesseraFlyoutHwndRegionSpec.ShouldWipeShowRegionOverRestLayout(
                    entrance: true, StackedRole, willRunPhase2))
            {
                return;
            }

            _motionRegionProgress = TesseraFlyoutRevealSpec.FancyPhase2StartProgress;
            _suppressRegionClientRedraw = TesseraFlyoutHwndRegionSpec.ShowRegionWipeMustNotRedrawClient;
            SyncRevealRegion();
            if (TesseraFlyoutAnimationPolicy.PreparePhase2ShowMustUnhideStackedMedia)
            {
                Opacity = 1;
                MotionSurface.Opacity = 1;
            }
        }

        internal void SetMotionRegionProgress(double progress)
        {
            _motionRegionProgress = progress;
            _suppressRegionClientRedraw = TesseraFlyoutHwndRegionSpec.ShowRegionWipeMustNotRedrawClient;
        }

        internal async Task RunMotionPhase1Async(bool entrance)
        {
            int generation = _stackedShowGeneration;
            CancellationToken token = _motionCts.Token;
            if (generation != MotionGeneration
                || !TesseraFlyoutAnimationPolicy.ShouldAnimateOpacity(FlyoutRequest.Ani))
            {
                return;
            }

            bool showMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(FlyoutRequest.Payload);
            await FlyoutMotionController.RunPhase1OnlyAsync(
                    CreateMotionContext(
                        showMedia,
                        runPhase2: false,
                        ResolveMonitorScale(),
                        () => generation != MotionGeneration,
                        token),
                    entrance)
                .ConfigureAwait(true);
        }

        internal async Task RunMotionPhase2Async(bool entrance)
        {
            if (!TryCreatePhase2Context(out FlyoutMotionController.MotionContext? ctx) || ctx is null)
            {
                return;
            }

            await FlyoutMotionController.RunPhase2OnlyAsync(ctx, entrance).ConfigureAwait(true);
        }

        internal bool TryCreatePhase2Context([NotNullWhen(true)] out FlyoutMotionController.MotionContext? ctx)
        {
            ctx = null;
            int generation = _stackedShowGeneration;
            CancellationToken token = _motionCts.Token;
            if (generation != MotionGeneration
                || !TesseraFlyoutAnimationPolicy.ShouldAnimateOpacity(FlyoutRequest.Ani))
            {
                return false;
            }

            bool showMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(FlyoutRequest.Payload);
            ctx = CreateMotionContext(
                showMedia,
                ShouldRunPhase2Reveal(showMedia),
                ResolveMonitorScale(),
                () => generation != MotionGeneration,
                token);
            return true;
        }

        internal void CompleteMotion(bool entrance)
        {
            long elapsed = Environment.TickCount64 - _motionStartTicks;
            if (_stackedShowGeneration != MotionGeneration)
            {
                // Superseded: another BeginMotion bumped the generation before this run finished,
                // so this sequence's cleanup is skipped and _motionAnimating stays owned by the
                // newer run. Logged because it is the silent half of a restart.
                TesseraFlyoutDiagnostics.Log(
                    $"motion superseded entrance={entrance} elapsedMs={elapsed} "
                    + $"runGen={_stackedShowGeneration} nowGen={MotionGeneration} {MotionStateTag}");
                return;
            }

            // A competing operation (e.g. a live-apply rebuild or kind/style handoff close)
            // can dispose this window's platform implementation while its own motion sequence
            // was still in flight. That's an expected race, not a bug in this cleanup - the
            // window is going away regardless, so there is nothing left to reset.
            if (PlatformImpl is null)
            {
                TesseraFlyoutDiagnostics.Log(
                    $"motion cleanup skipped (window disposed) entrance={entrance} elapsedMs={elapsed}");
                return;
            }

            TesseraFlyoutDiagnostics.Log(
                $"motion complete entrance={entrance} elapsedMs={elapsed} {MotionStateTag}");
            _motionAnimating = false;
            _motionRegionProgress = null;
            _suppressRegionClientRedraw = false;
            RenderTransform = null;
            if (entrance)
            {
                SetPhase(TesseraFlyoutPhase.Shown);
                Opacity = 1;
                MotionSurface.Opacity = 1;
                Position = _restPosition;
                if (TesseraFlyoutAnimationPolicy.CancelledEntranceMustSnapToRest)
                {
                    ApplyPhase2Reveal(TesseraFlyoutRevealSpec.RestRevealProgress);
                }
            }
            else
            {
                SetPhase(TesseraFlyoutPhase.Hidden);
                MotionSurface.Opacity = 0;
            }
        }

        internal FlyoutMotionController.MotionContext CreateMotionContext(
            bool showMediaStrip,
            bool runPhase2,
            double scale,
            Func<bool> isCancelled,
            CancellationToken motionToken = default)
        {
            return new()
            {
                Window = this,
                MonitorScale = scale,
                ShowMediaStrip = showMediaStrip,
                RunPhase2 = runPhase2,
                IsCancelled = isCancelled,
                MotionToken = motionToken,
            };
        }

        private (int Generation, CancellationToken Token) BeginMotionRun()
        {
            SupersedeMotion();
            return (MotionGeneration, _motionCts.Token);
        }

        private void SupersedeMotion()
        {
            MotionGeneration++;
            try { _motionCts.Cancel(); }
            catch { /* ignore */ }
            _motionCts.Dispose();
            _motionCts = new CancellationTokenSource();
        }

        internal void ApplyPhase2Reveal(double progress)
        {
            bool showMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(FlyoutRequest.Payload);
            bool willRunPhase2 = ShouldRunPhase2Reveal(showMedia);
            foreach (TesseraRevealHost host in this.GetVisualDescendants().OfType<TesseraRevealHost>())
            {
                host.Phase2Engaged = TesseraFlyoutRevealSpec.ResolveMotionPhase2Engaged(
                    progress, willRunPhase2, _motionEntrance);
                host.RevealProgress = progress;
            }

            SyncRevealRegion();
        }

        private void ResetDismissTimer()
        {
            _dismiss?.Stop();
            if (_autoDismissSuppressed || FlyoutRequest.AutoDismissMs <= 0)
            {
                return;
            }

            _dismiss = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(FlyoutRequest.AutoDismissMs) };
            _dismiss.Tick += (_, _) =>
            {
                if (IsFlyoutHovered)
                {
                    return;
                }

                _dismiss?.Stop();
                TransientDismiss();
            };
            _dismiss.Start();
        }

        /// <summary>
        /// SoftFrost-safe dismiss: hide the HWND without destroying it so the next Present
        /// can revive the same surface (Close+new stacks composition layers).
        /// </summary>
        public void TransientDismiss()
        {
            TransientDismissCore(notify: true);
        }

        /// <summary>Hide without raising <see cref="TransientDismissed"/> (stacked sibling cascade).</summary>
        public void TransientDismissWithoutNotify()
        {
            TransientDismissCore(notify: false);
        }

        private void TransientDismissCore(bool notify)
        {
            _dismiss?.Stop();
            IsFlyoutHovered = false;
            _revealGeneration++;
            SupersedeMotion();

            if (_exitAnimating)
            {
                FinishTransientHide(notify);
                return;
            }

            if (TesseraFlyoutAnimationPolicy.ShouldAnimateOpacity(FlyoutRequest.Ani)
                && TesseraFlyoutAnimationPolicy.ExitMustMirrorEntrance
                && !PresenterDrivesMotion)
            {
                _exitAnimating = true;
                SetPhase(TesseraFlyoutPhase.Exiting);
                _ = RunExitAnimationAsync(() => FinishTransientHide(notify));
                return;
            }

            FinishTransientHide(notify);
        }

        private async Task RunExitAnimationAsync(Action onComplete)
        {
            int exitGen = MotionGeneration;
            try
            {
                Task exitAnimation = FlyoutMotionSession.RunHideAsync([this]);
                exitGen = MotionGeneration;
                await exitAnimation.ConfigureAwait(true);
            }
            catch
            {
                /* fall through to hide */
            }
            finally
            {
                if (exitGen == MotionGeneration)
                {
                    _exitAnimating = false;
                }

                if (TesseraFlyoutDismissPolicy.ShouldFinishTransientDismiss(
                        exitGen, MotionGeneration, Phase, IsVisible))
                {
                    onComplete();
                }
            }
        }

        private void FinishTransientHide(bool notify)
        {
            _entranceStartPoseArmed = false;
            SetPhase(TesseraFlyoutPhase.Hidden);
            RenderTransform = null;
            Opacity = 1;
            MotionSurface.Opacity = 0;
            ResetRevealProgress();
            Win32WindowChrome.ClearWindowRegion(this);
            try
            {
                if (TesseraFlyoutLiveSyncPolicy.TransientDismissMustHideNotClose)
                {
                    Hide();
                }
                else
                {
                    Close();
                }
            }
            catch { /* ignore */ }

            if (notify)
            {
                try
                {
                    TransientDismissed?.Invoke(
                        TesseraOsAcrylicStackedPolicy.ResolveTransientDismissNotifyKey(
                            FlyoutRequest.ModuleId, StackedRole));
                }
                catch { /* ignore */ }
            }
        }

        private void ApplyFlyoutMaterial(TesseraFlyoutMaterial material)
        {
            _material = material;
            TesseraPalette.ApplyMaterial(_material);
            TransparencyLevelHint = ParseTransparencyHints(
                TesseraFlyoutWindowPolicy.ResolveTransparencyHints(_material));

            Background = TesseraFlyoutWindowPolicy.WindowBackgroundBrushIsTransparent
                ? Brushes.Transparent
                : new SolidColorBrush(Color.FromArgb(
                    TesseraFlyoutWindowPolicy.ResolveWindowBackgroundAlpha(_material),
                    0x11,
                    0x11,
                    0x1b));

            byte fallbackAlpha = TesseraFlyoutWindowPolicy.ResolveCompositionFallbackAlpha(_material);
            TransparencyBackgroundFallback = fallbackAlpha == 0
                ? Brushes.Transparent
                : new SolidColorBrush(Color.FromArgb(fallbackAlpha, 0x11, 0x11, 0x1b));
        }

        private void OnWheel(object? sender, PointerWheelEventArgs e)
        {
            if (!ModuleIds.IsTessera(FlyoutRequest.ModuleId))
            {
                return;
            }

            ResetDismissTimer();
            FlyoutUserActivity?.Invoke();
        }

        // Avalonia 11: WindowTransparencyLevel is a struct, not an enum, Enum.TryParse throws
        // "Type provided must be an Enum" and aborts flyout construction (see flyout.log).
        private static WindowTransparencyLevel[] ParseTransparencyHints(IReadOnlyList<string> hints)
        {
            List<WindowTransparencyLevel> list = [];
            foreach (string hint in hints)
            {
                if (TryMapTransparencyHint(hint, out WindowTransparencyLevel level))
                {
                    list.Add(level);
                }
            }

            if (list.Count == 0)
            {
                list.Add(TesseraFlyoutWindowPolicy.MustRequestOpaqueToolWindow
                    ? WindowTransparencyLevel.None
                    : WindowTransparencyLevel.Transparent);
            }

            return [.. list];
        }

        private static bool TryMapTransparencyHint(string? hint, out WindowTransparencyLevel level)
        {
            level = WindowTransparencyLevel.None;
            if (string.IsNullOrWhiteSpace(hint))
            {
                return false;
            }

            switch (hint.Trim().ToLowerInvariant())
            {
                case "none":
                    level = WindowTransparencyLevel.None;
                    return true;
                case "transparent":
                    level = WindowTransparencyLevel.Transparent;
                    return true;
                case "blur":
                    level = WindowTransparencyLevel.Blur;
                    return true;
                case "acrylicblur":
                    level = WindowTransparencyLevel.AcrylicBlur;
                    return true;
                case "mica":
                    level = WindowTransparencyLevel.Mica;
                    return true;
                default:
                    return false;
            }
        }
    }
}
