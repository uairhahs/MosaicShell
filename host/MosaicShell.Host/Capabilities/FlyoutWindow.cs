using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
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

    /// <summary>H3 stacked acrylic: cluster anchor in physical pixels; panel offset in DIP.</summary>
    public int? ClusterOriginX { get; set; }
    public int? ClusterOriginY { get; set; }
    public int PanelOffsetXDip { get; set; }
    public int PanelOffsetYDip { get; set; }

    /// <summary>H3 stacked acrylic: Win32 region clip radius in DIP (pill/card geometry).</summary>
    public double? BackdropCornerRadiusDip { get; set; }

    /// <summary>H3 stacked acrylic: lock HWND client area to signed placement DIP sizes.</summary>
    public double? StackedPanelWidthDip { get; set; }
    public double? StackedPanelHeightDip { get; set; }

    public FlyoutWindow(FlyoutRequest request, Control content, HostServices services)
    {
        _request = request;
        _services = services;
        ApplyFlyoutMaterial(TesseraFlyoutMaterialFactory.FromPayload(request.Payload, request.StyleId));
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

        Content = content;
        // SoftFrost Transparent HWND clears black for 1+ frames until composition settles.
        Opacity = TesseraFlyoutWindowPolicy.HideUntilCompositionReady ? 0 : 1;
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

    /// <summary>
    /// True only when the SoftFrost session is user-visible (not pre-reveal / transient-dismissed).
    /// </summary>
    public bool IsFlyoutSessionShowing =>
        TesseraFlyoutLiveSyncPolicy.IsEffectivelyShowing(IsVisible, Opacity);

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
        _request = request;
        ApplyFlyoutMaterial(TesseraFlyoutMaterialFactory.FromPayload(request.Payload, request.StyleId));
        // Invalidate any posted SoftFrost reveal from the previous surface.
        _revealGeneration++;
        RenderTransform = null;
        if (TesseraFlyoutWindowPolicy.HideUntilCompositionReady)
            Opacity = 0;
        Content = content;
        _lastSize = default;
        StackedPanelWidthDip = null;
        StackedPanelHeightDip = null;
        _clientSizeLocked = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        Width = double.NaN;
        Height = double.NaN;
        MaxWidth = double.PositiveInfinity;
        MaxHeight = double.PositiveInfinity;
        ResetDismissTimer();
        Relayout();
        EnsureLivePump();
    }

    /// <summary>Allow stacked relayout to remeasure when placement floors change.</summary>
    public void UnlockStackedClientSize() => _clientSizeLocked = false;

    /// <summary>
    /// SoftFrost: reveal after layout so the black composition-clear frame is never visible.
    /// Generation-gated so rapid Try now / ApplyRequest does not apply a stale Opacity=1.
    /// </summary>
    public void RevealAfterLayout()
    {
        if (!TesseraFlyoutWindowPolicy.HideUntilCompositionReady)
        {
            Opacity = 1;
            return;
        }

        var generation = _revealGeneration;
        // Post past the first layout/paint so WinUI composition has a settled Transparent surface.
        Dispatcher.UIThread.Post(() =>
        {
            if (!IsVisible) return;
            if (TesseraFlyoutWindowPolicy.RevealMustBeGenerationGated
                && generation != _revealGeneration)
                return;
            Opacity = 1;
        }, DispatcherPriority.Loaded);
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_relayouting) return;
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
        _relayouting = true;
        try
        {
            InvalidateMeasure();
            UpdateLayout();

            var dipW = Math.Max(Bounds.Width, DesiredSize.Width);
            var dipH = Math.Max(Bounds.Height, DesiredSize.Height);
            var stackedCluster = ClusterOriginX is not null && ClusterOriginY is not null;
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
                Position = new PixelPoint(
                    clusterX + (int)Math.Round(PanelOffsetXDip * scale),
                    clusterY + (int)Math.Round(PanelOffsetYDip * scale));
                ApplyStackedBackdropClip(w, h, scale);
                _lastSize = Bounds.Size;
                return;
            }

            var (x, y) = FlyoutAnchor.Compute(
                area.X, area.Y, area.Width, area.Height,
                w, h,
                _request.Anchor ?? "TL",
                xPad,
                yPad);
            Position = new PixelPoint(x, y);
            ApplyStackedBackdropClip(w, h, scale);
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
        if (!UsesOsAcrylicBackdrop() || BackdropCornerRadiusDip is not { } radiusDip || radiusDip <= 0)
            return;

        Win32Properties.SetWindowCornerPreference(
            this,
            Win32Properties.WindowCornerPreference.DoNotRound);
        var radiusPx = Math.Max(1, (int)Math.Round(radiusDip * scale));
        Win32WindowChrome.ApplyRoundRectRegion(this, widthPx, heightPx, radiusPx);
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

    public void PlayShowAnimation()
    {
        try
        {
            Relayout();
            // SoftFrost starts at Opacity 0, RevealAfterLayout owns the reveal.
            if (!TesseraFlyoutWindowPolicy.HideUntilCompositionReady)
                Opacity = 1;
            RenderTransform = null;

            if (_request.Ani <= 0)
                return;

            var dir = (_request.AniDir ?? "Left").ToLowerInvariant();
            var dist = _request.Ani >= 2 ? 28.0 : 14.0;
            double dx = 0, dy = 0;
            switch (dir)
            {
                case "right": dx = dist; break;
                case "top": dy = -dist; break;
                case "bottom": dy = dist; break;
                default: dx = -dist; break;
            }

            var tt = new TranslateTransform(dx, dy);
            RenderTransform = tt;
            AnimateDouble(tt, TranslateTransform.XProperty, dx, 0, 200);
            AnimateDouble(tt, TranslateTransform.YProperty, dy, 0, 200);
        }
        catch
        {
            if (!TesseraFlyoutWindowPolicy.HideUntilCompositionReady)
                Opacity = 1;
            RenderTransform = null;
        }
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
        TransientDismissCore();
        try { TransientDismissed?.Invoke(_request.ModuleId); }
        catch { /* ignore */ }
    }

    /// <summary>Hide without raising <see cref="TransientDismissed"/> (stacked sibling cascade).</summary>
    public void TransientDismissWithoutNotify() => TransientDismissCore();

    private void TransientDismissCore()
    {
        _dismiss?.Stop();
        _hover = false;
        _revealGeneration++;
        RenderTransform = null;
        Opacity = 0;
        try
        {
            if (TesseraFlyoutLiveSyncPolicy.TransientDismissMustHideNotClose)
                Hide();
            else
                Close();
        }
        catch { /* ignore */ }
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

    private static void AnimateDouble(Animatable target, AvaloniaProperty property, double from, double to, int ms)
    {
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(ms),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0.0), Setters = { new Setter(property, from) } },
                new KeyFrame { Cue = new Cue(1.0), Setters = { new Setter(property, to) } }
            }
        };
        _ = animation.RunAsync(target);
    }
}
