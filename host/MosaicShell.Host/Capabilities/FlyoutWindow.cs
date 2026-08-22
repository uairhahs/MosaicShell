using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Services;
using MosaicShell.Host.Tiles.Tessera;

namespace MosaicShell.Host.Capabilities;

internal sealed class FlyoutWindow : Window
{
    private FlyoutRequest _request;
    private readonly HostServices _services;
    private readonly TesseraFlyoutMaterial _material;
    private DispatcherTimer? _dismiss;
    private DispatcherTimer? _live;
    private bool _hover;
    private Size _lastSize;
    private bool _clientSizeLocked;

    public FlyoutWindow(FlyoutRequest request, Control content, HostServices services)
    {
        _request = request;
        _services = services;
        _material = TesseraFlyoutMaterialFactory.FromPayload(request.Payload);
        TesseraPalette.ApplyMaterial(_material);
        Title = $"MosaicShell - {request.ModuleId}";
        SizeToContent = SizeToContent.WidthAndHeight;
        CanResize = false;
        SystemDecorations = SystemDecorations.None;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        Focusable = true;
        IsHitTestVisible = true;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Background = Brushes.Transparent;
        Content = content;
        Opacity = 1;
        PointerEntered += (_, _) => { _hover = true; };
        PointerExited += (_, _) => { _hover = false; };
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

    public void EnsureLivePump() => StartLivePump();

    private void StartLivePump()
    {
        if (!string.Equals(_request.ModuleId, "Tessera", StringComparison.OrdinalIgnoreCase))
            return;
        if (_live is not null) return;
        _live = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        double lastVol = double.NaN;
        bool lastMute = false;
        _live.Tick += (_, _) =>
        {
            try
            {
                if (_request.Kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
                    || _request.Kind.Equals("flight", StringComparison.OrdinalIgnoreCase))
                {
                    RefreshStatusFromServices(resetDismiss: true);
                    return;
                }

                _services.Media.PumpTimeline();

                var vol = _services.Audio.MasterVolume;
                var mute = _services.Audio.IsMuted;
                var volPct = (int)Math.Round(Math.Clamp(vol, 0, 1) * 100);
                var lastPct = double.IsNaN(lastVol) ? int.MinValue : (int)Math.Round(Math.Clamp(lastVol, 0, 1) * 100);
                if (double.IsNaN(lastVol) || volPct != lastPct || mute != lastMute)
                {
                    lastVol = vol;
                    lastMute = mute;
                    ResetDismissTimer();
                }

                if (Content is not Control root || TesseraLiveHost.FindIn(root) is not { } host)
                {
                    System.Diagnostics.Debug.WriteLine("[Tessera live] TesseraLiveHost missing; live pump skipped.");
                    return;
                }

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

    private void RefreshStatusFromServices(bool resetDismiss)
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
        else
            System.Diagnostics.Debug.WriteLine("[Tessera live] TesseraLiveHost missing for status refresh.");
    }

    public void ApplyLiveOnly(FlyoutRequest request, HostServices services)
    {
        _request = request;
        services.Media.PumpTimeline();
        if (_request.Kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
            || _request.Kind.Equals("flight", StringComparison.OrdinalIgnoreCase))
        {
            RefreshStatusFromServices(resetDismiss: false);
            return;
        }
        if (Content is Control root && TesseraLiveHost.FindIn(root) is { } host)
            host.ApplyLive(services, _request);
        else
            System.Diagnostics.Debug.WriteLine("[Tessera live] TesseraLiveHost missing for soft refresh.");
    }

    public bool TryApplyLive(FlyoutRequest request, HostServices services, bool resetDismiss = true)
    {
        if (!request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(_request.Kind, request.Kind, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(_request.StyleId ?? "", request.StyleId ?? "", StringComparison.OrdinalIgnoreCase)) return false;
        if (Content is not Control root) return false;

        if (TesseraLiveHost.FindIn(root) is not { } liveHost)
        {
            System.Diagnostics.Debug.WriteLine("[Tessera live] TesseraLiveHost missing; rebuild required.");
            return false;
        }

        if (request.Kind.Equals("vol", StringComparison.OrdinalIgnoreCase)
            || request.Kind.Equals("bright", StringComparison.OrdinalIgnoreCase))
        {
            var vm = TesseraFlyoutViewModel.FromRequest(services, request);
            var hasMediaStrip = liveHost.Bindings.MediaTitle is not null
                                || liveHost.Bindings.MediaScrub is not null;
            if (vm.ShowMediaStrip != hasMediaStrip)
                return false;
        }

        liveHost.ApplyLive(services, request);
        _request = request;
        if (resetDismiss) ResetDismissTimer();
        return true;
    }

    public void ApplyRequest(FlyoutRequest request, Control content)
    {
        _request = request;
        Content = content;
        _lastSize = default;
        if (_material.ShouldLockClientSize)
        {
            _clientSizeLocked = false;
            SizeToContent = SizeToContent.WidthAndHeight;
            Width = double.NaN;
            Height = double.NaN;
        }
        ResetDismissTimer();
        Relayout();
        EnsureLivePump();
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        var s = Bounds.Size;
        if (s.Width < 2 || s.Height < 2) return;
        if (Math.Abs(s.Width - _lastSize.Width) < 0.5 && Math.Abs(s.Height - _lastSize.Height) < 0.5)
            return;
        _lastSize = s;
        RelayoutImmediate();
    }

    public void Relayout() =>
        Dispatcher.UIThread.Post(RelayoutImmediate, DispatcherPriority.Loaded);

    private void RelayoutImmediate()
    {
        try
        {
            InvalidateMeasure();
            UpdateLayout();

            var dipW = Math.Max(Bounds.Width, DesiredSize.Width);
            var dipH = Math.Max(Bounds.Height, DesiredSize.Height);
            if (dipW < 40 || dipH < 24) return;

            if (_material.ShouldLockClientSize && !_clientSizeLocked)
            {
                Width = dipW;
                Height = dipH;
                SizeToContent = SizeToContent.Manual;
                _clientSizeLocked = true;
            }

            var screens = Screens?.All?.ToList() ?? [];
            var screen = ResolveScreen(screens, _request.MonitorIndex) ?? Screens?.Primary;
            var area = screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);
            var scale = screen?.Scaling > 0.1 ? screen.Scaling : (Screens?.Primary?.Scaling ?? 1.0);
            var w = Math.Max(1, (int)Math.Ceiling(dipW * scale));
            var h = Math.Max(1, (int)Math.Ceiling(dipH * scale));

            var xPad = Math.Clamp(_request.XPad, 0, 200);
            var yPad = Math.Clamp(_request.YPad, 0, 200);
            var (x, y) = FlyoutAnchor.Compute(
                area.X, area.Y, area.Width, area.Height,
                w, h,
                _request.Anchor ?? "TL",
                xPad,
                yPad);
            Position = new PixelPoint(x, y);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Tessera position] {ex.Message}");
        }
    }

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
            if (_request.Ani <= 0)
            {
                Opacity = 0;
                AnimateDouble(this, OpacityProperty, 0, 1, 160);
                return;
            }

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
            Opacity = 0;
            AnimateDouble(this, OpacityProperty, 0, 1, 180);
            AnimateDouble(tt, TranslateTransform.XProperty, dx, 0, 200);
            AnimateDouble(tt, TranslateTransform.YProperty, dy, 0, 200);
        }
        catch
        {
            Opacity = 1;
            RenderTransform = null;
        }
    }

    private void ResetDismissTimer()
    {
        _dismiss?.Stop();
        if (_request.AutoDismissMs <= 0) return;
        _dismiss = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_request.AutoDismissMs) };
        _dismiss.Tick += (_, _) =>
        {
            if (_hover) return;
            _dismiss.Stop();
            try { Close(); } catch { /* ignore */ }
        };
        _dismiss.Start();
    }

    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        if (!_request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase)) return;
        ResetDismissTimer();
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
