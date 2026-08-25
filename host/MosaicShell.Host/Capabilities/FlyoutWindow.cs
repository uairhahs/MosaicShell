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

namespace MosaicShell.Host.Capabilities;

/// <summary>
/// Tessera flyout surface. Configured per Avalonia window docs:
/// TransparencyLevelHint + Transparent Background + TransparencyBackgroundFallback,
/// SizeToContent, unowned Show(), Topmost, Screens for placement.
/// </summary>
internal sealed class FlyoutWindow : Window
{
    private FlyoutRequest _request;
    private readonly HostServices _services;
    private TesseraFlyoutMaterial _material = null!;
    private DispatcherTimer? _dismiss;
    private DispatcherTimer? _live;
    private int _revealGeneration;
    private bool _hover;
    private bool _autoDismissSuppressed;
    private Size _lastSize;
    private bool _clientSizeLocked;
    private bool _relayouting;
    private PixelPoint _restPosition;
    private bool _motionAnimating;
    private bool _phase2Animating;
    private bool _syncingRevealRegion;
    private double _signedRevealRestWidthDip;
    private double _signedRevealRestHeightDip;
    private int _motionGeneration;
    private int _stackedShowGeneration;
    private CancellationTokenSource _motionCts = new();
    private readonly Panel _motionSurface = new();

    /// <summary>Fades with flyout content; acrylic HWND stays opaque until Hide.</summary>
    internal Panel MotionSurface => _motionSurface;

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

    internal bool Phase2Animating
    {
        get => _phase2Animating;
        set => _phase2Animating = value;
    }

    internal FlyoutRequest FlyoutRequest => _request;

    /// <summary>H3 stacked acrylic: lock HWND client area to signed placement DIP sizes.</summary>
    public double? StackedPanelWidthDip { get; set; }
    public double? StackedPanelHeightDip { get; set; }

    public FlyoutWindow(FlyoutRequest request, Control content, HostServices services)
    {
        _request = request;
        _services = services;
        ApplyFlyoutMaterial(TesseraFlyoutMaterialFactory.FromPayload(request.Payload, request.StyleId, request.Kind));
        // Win32 title is for HWND identity only, WindowDecorations.None; never a visible chrome strip.
        Title = $"MosaicShell - {request.ModuleId}";

        // docs: SizeToContent for content-sized tool windows
        SizeToContent = SizeToContent.WidthAndHeight;
        CanResize = false;
        WindowDecorations = Avalonia.Controls.WindowDecorations.None;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        Focusable = true;
        IsHitTestVisible = true;

        Content = _motionSurface;
        SetFlyoutContent(content);
        // Keep HWND composited; fade the motion surface (glass + controls) instead of Window.Opacity alone.
        Opacity = 1;
        _motionSurface.Opacity = TesseraFlyoutWindowPolicy.HideUntilCompositionReady ? 0 : 1;
        PointerEntered += (_, _) =>
        {
            _hover = true;
            PointerHoverChanged?.Invoke();
            FlyoutUserActivity?.Invoke();
        };
        PointerExited += (_, _) =>
        {
            _hover = false;
            PointerHoverChanged?.Invoke();
        };
        PointerWheelChanged += OnWheel;
        Opened += (_, _) =>
        {
            Relayout();
            EnsureLivePump();
        };
        LayoutUpdated += OnLayoutUpdated;
        Closed += (_, _) => StopLivePump();
        ResetDismissTimer();
    }

    public string Kind => _request.Kind;
    public string? StyleId => _request.StyleId;
    internal int MotionGeneration => _motionGeneration;

    /// <summary>
    /// True only when the SoftFrost session is user-visible (not pre-reveal / transient-dismissed).
    /// </summary>
    public bool IsFlyoutSessionShowing =>
        TesseraFlyoutLiveSyncPolicy.IsEffectivelyShowing(IsVisible, _motionSurface.Opacity);

    private void SetFlyoutContent(Control content)
    {
        _motionSurface.Children.Clear();
        _motionSurface.Children.Add(content);
    }

    private void ResetRevealProgress()
    {
        var showMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(_request.Payload);
        var willRunPhase2 = ShouldRunPhase2Reveal(showMedia);
        var progress = TesseraFlyoutRevealSpec.ResolveHideRevealProgress(willRunPhase2);
        var engaged = TesseraFlyoutRevealSpec.ResolveHidePhase2Engaged(willRunPhase2);
        foreach (var host in this.GetVisualDescendants().OfType<TesseraRevealHost>())
        {
            host.Phase2Engaged = engaged;
            host.RevealProgress = progress;
        }
    }

    internal void SyncStrokeBRegion() => SyncRevealRegion();

    internal void SyncRevealRegion()
    {
        if (_syncingRevealRegion)
            return;

        var showMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(_request.Payload);
        if (!TesseraFlyoutHwndRegionSpec.StyleNeedsRevealRegion(_request.StyleId, showMedia, StackedRole)
            && !TesseraFlyoutHwndRegionSpec.StyleNeedsStrokeBRegion(_request.StyleId, showMedia))
            return;

        _syncingRevealRegion = true;
        try
        {
            var hosts = this.GetVisualDescendants().OfType<TesseraRevealHost>().ToList();
            var forceRest = TesseraFlyoutHwndRegionSpec.ShouldForceRestRevealRegion(
                _motionAnimating, _phase2Animating, IsFlyoutSessionShowing);
            if (forceRest)
            {
                foreach (var host in hosts)
                {
                    host.Phase2Engaged = true;
                    if (!TesseraFlyoutHwndRegionSpec.IsRestRevealProgress(host.RevealProgress))
                        host.RevealProgress = TesseraFlyoutRevealSpec.RestRevealProgress;
                }
            }

            var progress = TesseraFlyoutHwndRegionSpec.ResolveRegionProgress(
                hosts.ConvertAll(static h => h.RevealProgress),
                _motionAnimating,
                _phase2Animating,
                IsFlyoutSessionShowing);
            var engaged = forceRest
                          || TesseraFlyoutHwndRegionSpec.IsRestRevealProgress(progress)
                          || hosts.Exists(static h => h.Phase2Engaged);

            var signedW = StackedPanelWidthDip ?? (_signedRevealRestWidthDip > 1 ? _signedRevealRestWidthDip : 0);
            var signedH = StackedPanelHeightDip ?? (_signedRevealRestHeightDip > 1 ? _signedRevealRestHeightDip : 0);
            var restW = TesseraFlyoutHwndRegionSpec.ResolveRestExtentDip(signedW, Bounds.Width);
            var restH = TesseraFlyoutHwndRegionSpec.ResolveRestExtentDip(signedH, Bounds.Height);
            var region = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                _request.StyleId,
                StackedRole,
                progress,
                engaged,
                showMedia,
                restW,
                restH);
            var scale = ResolveMonitorScale();
            var radiusDip = BackdropCornerRadiusDip
                            ?? (TesseraFlyoutHwndRegionSpec.StyleNeedsStrokeBRegion(_request.StyleId, showMedia)
                                ? TesseraStackedPlacementSpec.Win11CornerRadiusDip
                                : 10);

            if (region.HideChrome
                && TesseraFlyoutHwndRegionSpec.CollapsedRevealRegionMustHideRestChrome
                && !TesseraFlyoutHwndRegionSpec.IsRestRevealProgress(progress))
            {
                Win32Properties.SetWindowCornerPreference(
                    this,
                    Win32Properties.WindowCornerPreference.DoNotRound);
                Win32WindowChrome.ApplyRoundRectRegion(this, 1, 1, 1, applySynchronously: true);
                if (StackedRole == TesseraStackedPanelRole.Media)
                    Opacity = 0;
                return;
            }

            var phys = TesseraFlyoutHwndRegionSpec.ResolveRoundRectPhysical(
                region.WidthDip, region.HeightDip, radiusDip, scale);
            if (phys.HeightPx < 2 || phys.WidthPx < 2)
                return;

            if (StackedRole == TesseraStackedPanelRole.Media)
                Opacity = 1;

            Win32Properties.SetWindowCornerPreference(
                this,
                Win32Properties.WindowCornerPreference.DoNotRound);
            Win32WindowChrome.ApplyRoundRectRegion(
                this,
                phys.WidthPx,
                phys.HeightPx,
                phys.CornerRadiusPx,
                applySynchronously: true);
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

    public bool IsFlyoutHovered => _hover;

    /// <summary>H3 stacked acrylic: only the presenter runs auto-dismiss for the cluster.</summary>
    public void SuppressAutoDismiss()
    {
        _autoDismissSuppressed = true;
        _dismiss?.Stop();
    }

    public void EnsureLivePump() => StartLivePump();

    private void StartLivePump()
    {
        if (!string.Equals(_request.ModuleId, "Tessera", StringComparison.OrdinalIgnoreCase))
            return;
        if (_live is not null) return;
        _live = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _live.Tick += (_, _) =>
        {
            try
            {
                if (_request.Kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
                    || _request.Kind.Equals("flight", StringComparison.OrdinalIgnoreCase))
                {
                    RefreshStatusFromServices();
                    return;
                }

                // Timeline UI: ApplyLive only. PumpTimeline runs from armed poll / user events.
                if (Content is Control root && TesseraLiveHost.FindIn(root) is { } host)
                    host.ApplyLive(_services, _request);
                if (TesseraFlyoutHwndRegionSpec.ShouldForceRestRevealRegion(
                        _motionAnimating, _phase2Animating, IsFlyoutSessionShowing))
                    SyncRevealRegion();
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
        var payload = TesseraFlyoutRequestBuilder.RefreshStatusPayload(
            _services, _request.Kind, _request.Payload);
        var prevOn = _request.Payload?.GetValueOrDefault("on");
        var nextOn = payload.GetValueOrDefault("on");
        var prevLock = _request.Payload?.GetValueOrDefault("lock");
        var nextLock = payload.GetValueOrDefault("lock");
        if (prevOn == nextOn && prevLock == nextLock)
            return;

        _request = _request with { Payload = payload };
        if (Content is Control root && TesseraLiveHost.FindIn(root) is { } host)
            host.ApplyLive(_services, _request);
    }

    public void ApplyLiveOnly(FlyoutRequest request, HostServices services)
    {
        _request = request;
        if (_request.Kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
            || _request.Kind.Equals("flight", StringComparison.OrdinalIgnoreCase))
        {
            RefreshStatusFromServices();
            return;
        }
        if (Content is Control root && TesseraLiveHost.FindIn(root) is { } host)
            host.ApplyLive(services, _request);
    }

    public bool TryApplyLive(FlyoutRequest request, HostServices services, bool resetDismiss = true)
    {
        if (!request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(_request.Kind, request.Kind, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(_request.StyleId ?? "", request.StyleId ?? "", StringComparison.OrdinalIgnoreCase)) return false;
        if (Content is not Control root) return false;

        if (TesseraLiveHost.FindIn(root) is not { } liveHost)
            return false;

        // Do NOT force a full rebuild when media-strip presence flips mid-volume.
        // Rebuild + PresentFlyout (Win32 restack ×3) on every Audio.Changed freezes the app.
        // Strip structure can catch up on the next cold Show.
        liveHost.ApplyLive(services, request);
        _request = request;
        if (resetDismiss) ResetDismissTimer();
        return true;
    }

    public void ApplyRequest(FlyoutRequest request, Control content)
    {
        var wasShowing = IsFlyoutSessionShowing;
        _request = request;
        ApplyFlyoutMaterial(TesseraFlyoutMaterialFactory.FromPayload(request.Payload, request.StyleId, request.Kind));
        // Invalidate any posted SoftFrost reveal from the previous surface.
        _revealGeneration++;
        SupersedeMotion();
        if (TesseraFlyoutAnimationPolicy.MotionAnimatingMustClearOnSupersede)
            _motionAnimating = false;
        Win32WindowChrome.ClearWindowRegion(this);
        PresenterDrivesMotion = false;
        RenderTransform = null;
        Opacity = 1;
        var hideUntilReady = TesseraFlyoutWindowPolicy.HideUntilCompositionReady;
        if (TesseraFlyoutLiveSyncPolicy.ShouldZeroMotionSurfaceOnApplyRequest(wasShowing, hideUntilReady))
            _motionSurface.Opacity = 0;
        else if (wasShowing)
            _motionSurface.Opacity = 1;
        SetFlyoutContent(content);
        if (TesseraFlyoutLiveSyncPolicy.ShouldSnapRevealToRestAfterApplyRequest(wasShowing))
            ApplyPhase2Reveal(TesseraFlyoutRevealSpec.RestRevealProgress);
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
    public void UnlockStackedClientSize() => _clientSizeLocked = false;

    private bool _exitAnimating;

    /// <summary>
    /// SoftFrost: reveal after layout with optional slide + eased opacity (settings Motion page).
    /// Generation-gated so rapid ApplyRequest does not apply a stale entrance.
    /// </summary>
    public void RevealAfterLayout()
    {
        void Run()
        {
            if (!IsVisible) return;
            RunEntranceAnimation();
        }

        if (!TesseraFlyoutWindowPolicy.HideUntilCompositionReady)
        {
            Run();
            return;
        }

        var generation = _revealGeneration;
        // One Loaded + one Render frame so SoftFrost composition settles before motion starts.
        Dispatcher.UIThread.Post(() =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!IsVisible) return;
                if (TesseraFlyoutWindowPolicy.RevealMustBeGenerationGated
                    && generation != _revealGeneration)
                    return;
                RunEntranceAnimation();
            }, DispatcherPriority.Render);
        }, DispatcherPriority.Loaded);
    }

    /// <summary>Sync layout then reveal (stacked + single Present paths).</summary>
    public void PlayShowAnimation()
    {
        try
        {
            FinishLayout();
            RevealAfterLayout();
        }
        catch
        {
            _motionSurface.Opacity = 1;
            RenderTransform = null;
            Position = _restPosition;
        }
    }

    private void EnsureStatusRoundClipBeforeReveal()
    {
        if (!TesseraStatusFlyoutPolicy.IsStatusKind(_request.Kind)
            || !TesseraStatusFlyoutPolicy.MustRoundClipHwndBeforeReveal)
            return;

        if (BackdropCornerRadiusDip is not { } radiusDip || radiusDip <= 0)
            BackdropCornerRadiusDip = TesseraStatusFlyoutPolicy.ResolveChipCornerRadiusDip(_request.StyleId);

        var (dipW, dipH) = TesseraStatusFlyoutPolicy.ResolveStatusClientSizeDip(
            Bounds.Width, Bounds.Height, DesiredSize.Width, DesiredSize.Height);
        if (!TesseraFlyoutWindowPolicy.MeetsRelayoutSizeGate(dipW, dipH, stackedClusterPanel: false))
            return;

        var scale = Screens?.ScreenFromWindow(this)?.Scaling
                    ?? Screens?.Primary?.Scaling
                    ?? 1.0;
        if (scale < 0.1) scale = 1.0;
        var w = Math.Max(1, (int)Math.Ceiling(dipW * scale));
        var h = Math.Max(1, (int)Math.Ceiling(dipH * scale));
        ApplyStackedBackdropClip(w, h, scale);
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_relayouting || TesseraFlyoutAnimationPolicy.ShouldDeferRelayoutDuringMotion(_motionAnimating, _phase2Animating))
            return;
        var s = Bounds.Size;
        if (s.Width < 2 || s.Height < 2) return;
        if (ClusterOriginX is not null
            && _clientSizeLocked
            && !double.IsNaN(Width)
            && !double.IsNaN(Height)
            && Math.Abs(s.Width - Width) < 0.5
            && Math.Abs(s.Height - Height) < 0.5)
            return;
        if (Math.Abs(s.Width - _lastSize.Width) < 0.5 && Math.Abs(s.Height - _lastSize.Height) < 0.5)
            return;
        _lastSize = s;
        RelayoutImmediate();
    }

    public void Relayout() =>
        Dispatcher.UIThread.Post(RelayoutImmediate, DispatcherPriority.Loaded);

    public void FinishLayout() => RelayoutImmediate();

    private void RelayoutImmediate()
    {
        if (_relayouting) return;
        if (TesseraFlyoutAnimationPolicy.ShouldDeferRelayoutDuringMotion(_motionAnimating, _phase2Animating))
            return;
        _relayouting = true;
        try
        {
            InvalidateMeasure();
            UpdateLayout();

            var dipW = Math.Max(Bounds.Width, DesiredSize.Width);
            var dipH = Math.Max(Bounds.Height, DesiredSize.Height);
            var stackedCluster = ClusterOriginX is not null && ClusterOriginY is not null;
            var statusChip = TesseraStatusFlyoutPolicy.IsStatusKind(_request.Kind)
                && TesseraStatusFlyoutPolicy.ForbidFixedVolumeShellSize;
            if (statusChip)
            {
                (dipW, dipH) = TesseraStatusFlyoutPolicy.ResolveStatusClientSizeDip(
                    Bounds.Width, Bounds.Height, DesiredSize.Width, DesiredSize.Height);
            }

            if (!TesseraFlyoutWindowPolicy.MeetsRelayoutSizeGate(dipW, dipH, stackedCluster))
                return;

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
                var measuredW = Math.Max(DesiredSize.Width, 0);
                var measuredH = Math.Max(DesiredSize.Height, 0);
                measuredW = TesseraStackedPlacementPolicy.SanitizeStackedPanelMeasure(panelW, measuredW);
                measuredH = TesseraStackedPlacementPolicy.SanitizeStackedPanelMeasure(panelH, measuredH);
                var (clientW, clientH) = TesseraStackedPlacementPolicy.ResolveStackedClientSize(
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
            var screens = Screens?.All?.ToList() ?? [];
            var screen = ResolveScreen(screens, _request.MonitorIndex) ?? Screens?.Primary;
            var area = screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);
            var scale = screen?.Scaling > 0.1 ? screen.Scaling : (Screens?.Primary?.Scaling ?? 1.0);
            var w = Math.Max(1, (int)Math.Ceiling(dipW * scale));
            var h = Math.Max(1, (int)Math.Ceiling(dipH * scale));

            var xPad = Math.Clamp(_request.XPad, 0, 200);
            var yPad = Math.Clamp(_request.YPad, 0, 200);

            if (ClusterOriginX is int clusterX && ClusterOriginY is int clusterY)
            {
                var pt = new PixelPoint(
                    clusterX + (int)Math.Round(PanelOffsetXDip * scale),
                    clusterY + (int)Math.Round(PanelOffsetYDip * scale));
                CommitRestPosition(pt);
                ApplyStackedBackdropClip(w, h, scale);
                SyncStrokeBRegion();
                _lastSize = Bounds.Size;
                return;
            }

            var (x, y) = FlyoutAnchor.Compute(
                area.X, area.Y, area.Width, area.Height,
                w, h,
                _request.Anchor ?? "TL",
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
        }
    }

    private void ApplyStackedBackdropClip(int widthPx, int heightPx, double scale)
    {
        if (BackdropCornerRadiusDip is not { } radiusDip || radiusDip <= 0)
            return;

        var status = TesseraStatusFlyoutPolicy.IsStatusKind(_request.Kind);
        var acrylic = UsesOsAcrylicBackdrop();
        if (!acrylic
            && !(status && TesseraStatusFlyoutPolicy.SoftFrostMustRoundClipHwnd))
            return;

        Win32Properties.SetWindowCornerPreference(
            this,
            Win32Properties.WindowCornerPreference.DoNotRound);
        var radiusPx = Math.Max(1, (int)Math.Round(radiusDip * scale));
        var sync = status && TesseraStatusFlyoutPolicy.RoundClipMustApplySynchronouslyWhenHandleReady;
        Win32WindowChrome.ApplyRoundRectRegion(this, widthPx, heightPx, radiusPx, applySynchronously: sync);
    }

    private bool UsesOsAcrylicBackdrop() =>
        _material.TransparencyHints.Any(static hint =>
            hint.Equals("AcrylicBlur", StringComparison.OrdinalIgnoreCase));

    private static Screen? ResolveScreen(IReadOnlyList<Screen> screens, int monitorIndexOneBased)
    {
        if (screens.Count == 0) return null;
        if (monitorIndexOneBased <= 1)
            return screens.FirstOrDefault(s => s.IsPrimary) ?? screens[0];

        var idx = Math.Clamp(monitorIndexOneBased - 1, 0, screens.Count - 1);
        return screens[idx];
    }

    private void CommitRestPosition(PixelPoint rest)
    {
        _restPosition = rest;
        if (!_motionAnimating)
            Position = rest;
    }

    private async void RunEntranceAnimation()
    {
        if (PresenterDrivesMotion)
            return;

        await FlyoutMotionSession.RunShowAsync([this]).ConfigureAwait(true);
    }

    internal double ResolveMonitorScale() =>
        Screens?.ScreenFromWindow(this)?.Scaling
        ?? Screens?.Primary?.Scaling
        ?? 1.0;

    internal bool ShouldRunPhase2Reveal(bool showMediaStrip) =>
        TesseraFlyoutAnimationPolicy.ShouldRunPhase2Reveal(
            _request.Ani, _request.StyleId, showMediaStrip, StackedRole);

    internal void BeginMotion(bool entrance)
    {
        EnsureStatusRoundClipBeforeReveal();
        Position = _restPosition;
        var (generation, _) = BeginMotionRun();
        _stackedShowGeneration = generation;

        if (!TesseraFlyoutAnimationPolicy.ShouldAnimateOpacity(_request.Ani))
        {
            if (entrance)
            {
                Opacity = 1;
                _motionSurface.Opacity = 1;
                RenderTransform = null;
                ApplyPhase2Reveal(1);
            }
            else
            {
                _motionSurface.Opacity = 0;
                RenderTransform = null;
            }

            return;
        }

        _motionAnimating = true;
        var showMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(_request.Payload);
        if (entrance)
        {
            ApplyPhase2Reveal(TesseraFlyoutRevealSpec.ResolveShowRevealProgress(ShouldRunPhase2Reveal(showMedia)));
            if (TesseraFlyoutAnimationPolicy.ShouldHoldStackedMediaHiddenThroughShowPhase1(
                    _request.Ani, _request.StyleId, showMedia, StackedRole))
            {
                Opacity = 0;
                _motionSurface.Opacity = 0;
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
        if (_stackedShowGeneration != _motionGeneration || !_motionAnimating)
            return;
        _motionSurface.Opacity = 1;
        SyncRevealRegion();
    }

    internal async Task RunMotionPhase1Async(bool entrance)
    {
        var generation = _stackedShowGeneration;
        var token = _motionCts.Token;
        if (generation != _motionGeneration
            || !TesseraFlyoutAnimationPolicy.ShouldAnimateOpacity(_request.Ani))
            return;

        var showMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(_request.Payload);
        await FlyoutMotionController.RunPhase1OnlyAsync(
                CreateMotionContext(
                    showMedia,
                    runPhase2: false,
                    ResolveMonitorScale(),
                    () => generation != _motionGeneration,
                    token),
                entrance)
            .ConfigureAwait(true);
    }

    internal async Task RunMotionPhase2Async(bool entrance)
    {
        var generation = _stackedShowGeneration;
        var token = _motionCts.Token;
        if (generation != _motionGeneration
            || !TesseraFlyoutAnimationPolicy.ShouldAnimateOpacity(_request.Ani))
            return;

        var showMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(_request.Payload);
        await FlyoutMotionController.RunPhase2OnlyAsync(
                CreateMotionContext(
                    showMedia,
                    ShouldRunPhase2Reveal(showMedia),
                    ResolveMonitorScale(),
                    () => generation != _motionGeneration,
                    token),
                entrance)
            .ConfigureAwait(true);
    }

    internal void CompleteMotion(bool entrance)
    {
        if (_stackedShowGeneration != _motionGeneration)
            return;

        _motionAnimating = false;
        RenderTransform = null;
        if (entrance)
        {
            Opacity = 1;
            _motionSurface.Opacity = 1;
            Position = _restPosition;
            if (TesseraFlyoutAnimationPolicy.CancelledEntranceMustSnapToRest)
                ApplyPhase2Reveal(TesseraFlyoutRevealSpec.RestRevealProgress);
        }
        else
        {
            _motionSurface.Opacity = 0;
        }
    }

    internal FlyoutMotionController.MotionContext CreateMotionContext(
        bool showMediaStrip,
        bool runPhase2,
        double scale,
        Func<bool> isCancelled,
        CancellationToken motionToken = default) =>
        new()
        {
            Window = this,
            MonitorScale = scale,
            ShowMediaStrip = showMediaStrip,
            RunPhase2 = runPhase2,
            IsCancelled = isCancelled,
            MotionToken = motionToken,
        };

    private (int Generation, CancellationToken Token) BeginMotionRun()
    {
        SupersedeMotion();
        return (_motionGeneration, _motionCts.Token);
    }

    private void SupersedeMotion()
    {
        _motionGeneration++;
        try { _motionCts.Cancel(); }
        catch { /* ignore */ }
        _motionCts.Dispose();
        _motionCts = new CancellationTokenSource();
    }

    internal void ApplyPhase2Reveal(double progress)
    {
        foreach (var host in this.GetVisualDescendants().OfType<TesseraRevealHost>())
        {
            host.Phase2Engaged = progress >= TesseraFlyoutRevealSpec.RestRevealProgress - 0.001;
            host.RevealProgress = progress;
        }

        SyncRevealRegion();
    }

    private void ResetDismissTimer()
    {
        _dismiss?.Stop();
        if (_autoDismissSuppressed || _request.AutoDismissMs <= 0) return;
        _dismiss = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_request.AutoDismissMs) };
        _dismiss.Tick += (_, _) =>
        {
            if (_hover) return;
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
    public void TransientDismissWithoutNotify() => TransientDismissCore(notify: false);

    private void TransientDismissCore(bool notify)
    {
        _dismiss?.Stop();
        _hover = false;
        _revealGeneration++;
        SupersedeMotion();

        if (_exitAnimating)
        {
            FinishTransientHide(notify);
            return;
        }

        if (TesseraFlyoutAnimationPolicy.ShouldAnimateOpacity(_request.Ani)
            && TesseraFlyoutAnimationPolicy.ExitMustMirrorEntrance
            && !PresenterDrivesMotion)
        {
            _exitAnimating = true;
            _ = RunExitAnimationAsync(() => FinishTransientHide(notify));
            return;
        }

        FinishTransientHide(notify);
    }

    private async Task RunExitAnimationAsync(Action onComplete)
    {
        try
        {
            await FlyoutMotionSession.RunHideAsync([this]).ConfigureAwait(true);
        }
        catch
        {
            /* fall through to hide */
        }
        finally
        {
            _exitAnimating = false;
            onComplete();
        }
    }

    private void FinishTransientHide(bool notify)
    {
        RenderTransform = null;
        Opacity = 1;
        _motionSurface.Opacity = 0;
        ResetRevealProgress();
        Win32WindowChrome.ClearWindowRegion(this);
        try
        {
            if (TesseraFlyoutLiveSyncPolicy.TransientDismissMustHideNotClose)
                Hide();
            else
                Close();
        }
        catch { /* ignore */ }

        if (notify)
        {
            try
            {
                TransientDismissed?.Invoke(
                    TesseraOsAcrylicStackedPolicy.ResolveTransientDismissNotifyKey(
                        _request.ModuleId, StackedRole));
            }
            catch { /* ignore */ }
        }
    }

    private void ApplyFlyoutMaterial(TesseraFlyoutMaterial material)
    {
        _material = material;
        TesseraPalette.ApplyMaterial(_material);

        var shellAlpha = TesseraFlyoutWindowPolicy.ResolveWindowBackgroundAlpha(_material);
        var shell = new SolidColorBrush(Color.FromArgb(shellAlpha, 0x11, 0x11, 0x1b));
        TransparencyLevelHint = ParseTransparencyHints(
            TesseraFlyoutWindowPolicy.ResolveTransparencyHints(_material));
        Background = TesseraFlyoutWindowPolicy.WindowBackgroundBrushIsTransparent
            ? Brushes.Transparent
            : shell;

        var fallbackAlpha = TesseraFlyoutWindowPolicy.ResolveCompositionFallbackAlpha(_material);
        TransparencyBackgroundFallback = fallbackAlpha == 0
            ? Brushes.Transparent
            : new SolidColorBrush(Color.FromArgb(fallbackAlpha, 0x11, 0x11, 0x1b));
    }

    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        if (!_request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase)) return;
        ResetDismissTimer();
        FlyoutUserActivity?.Invoke();
    }

    // Avalonia 11: WindowTransparencyLevel is a struct, not an enum, Enum.TryParse throws
    // "Type provided must be an Enum" and aborts flyout construction (see flyout.log).
    private static WindowTransparencyLevel[] ParseTransparencyHints(IReadOnlyList<string> hints)
    {
        var list = new List<WindowTransparencyLevel>();
        foreach (var hint in hints)
        {
            if (TryMapTransparencyHint(hint, out var level))
                list.Add(level);
        }

        if (list.Count == 0)
            list.Add(TesseraFlyoutWindowPolicy.MustRequestOpaqueToolWindow
                ? WindowTransparencyLevel.None
                : WindowTransparencyLevel.Transparent);
        return list.ToArray();
    }

    private static bool TryMapTransparencyHint(string? hint, out WindowTransparencyLevel level)
    {
        level = WindowTransparencyLevel.None;
        if (string.IsNullOrWhiteSpace(hint))
            return false;

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
