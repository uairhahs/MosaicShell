using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Services;
using MosaicShell.Host.Tiles.Tessera;

namespace MosaicShell.Host.Capabilities;

public sealed class AvaloniaCapabilityUiBridge : ICapabilityUiBridge
{
    public AvaloniaCapabilityUiBridge(IFlyoutPresenter flyouts) => Flyouts = flyouts;
    public IFlyoutPresenter Flyouts { get; }
}

public sealed class AvaloniaFlyoutPresenter : IFlyoutPresenter
{
    private readonly HostServices _services;
    private readonly object _gate = new();
    private readonly Dictionary<string, FlyoutWindow> _windows = new(StringComparer.OrdinalIgnoreCase);
    private FocusDimWindow? _focusDim;
    private TesseraOutsideClickWatcher? _outsideClick;

    public AvaloniaFlyoutPresenter(HostServices services) => _services = services;

    public void Show(FlyoutRequest request) =>
        Dispatcher.UIThread.Post(() => SafeShowOrUpdate(request, resetDismiss: true));

    public void Update(FlyoutRequest request) =>
        Dispatcher.UIThread.Post(() => SafeShowOrUpdate(request, resetDismiss: true));

    public void SoftRefresh(FlyoutRequest request) =>
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                lock (_gate)
                {
                    if (!_windows.TryGetValue(request.ModuleId, out var existing) || !existing.IsVisible)
                        return;
                    existing.ApplyLiveOnly(request, _services);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Tessera soft] {ex}"); }
        });

    public void Hide(string moduleId)
    {
        Dispatcher.UIThread.Post(() =>
        {
            FlyoutWindow? w;
            lock (_gate)
            {
                if (!_windows.Remove(moduleId, out w)) return;
            }
            try { w.Close(); } catch { /* ignore */ }
            if (moduleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
            {
                StopOutsideClickWatcher();
                CloseFocusDim();
            }
        });
    }

    public void HideAll()
    {
        Dispatcher.UIThread.Post(() =>
        {
            List<string> ids;
            lock (_gate) ids = _windows.Keys.ToList();
            foreach (var id in ids)
                Hide(id);
            StopOutsideClickWatcher();
            CloseFocusDim();
        });
    }

    public bool IsVisible(string moduleId)
    {
        lock (_gate)
            return _windows.TryGetValue(moduleId, out var w) && w.IsVisible;
    }

    private void SafeShowOrUpdate(FlyoutRequest request, bool resetDismiss = true)
    {
        try { ShowOrUpdateCore(request, resetDismiss); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Tessera flyout] {ex}"); }
    }

    private void ShowOrUpdateCore(FlyoutRequest request, bool resetDismiss = true)
    {
        lock (_gate)
        {
            if (_windows.TryGetValue(request.ModuleId, out var existing) && existing.IsVisible)
            {
                if (existing.TryApplyLive(request, _services, resetDismiss))
                {
                    SyncFocusDim(request);
                    RestackAboveDim(existing);
                    if (request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
                        EnsureOutsideClickWatcher(existing);
                    return;
                }
                existing.ApplyRequest(request, BuildContent(request));
                SyncFocusDim(request);
                RestackAboveDim(existing);
                if (request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
                    EnsureOutsideClickWatcher(existing);
                return;
            }

            if (_windows.TryGetValue(request.ModuleId, out var old))
            {
                try { old.Close(); } catch { /* ignore */ }
                _windows.Remove(request.ModuleId);
            }
        }

        SyncFocusDim(request);
        var content = BuildContent(request);
        var window = new FlyoutWindow(request, content, _services);
        window.Closed += (_, _) =>
        {
            lock (_gate) _windows.Remove(request.ModuleId);
            if (request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
            {
                StopOutsideClickWatcher();
                CloseFocusDim();
            }
        };
        lock (_gate) _windows[request.ModuleId] = window;
        window.Show();
        RestackAboveDim(window);
        window.PlayShowAnimation();
        if (request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
            StartOutsideClickWatcher(window);
    }

    private static void RestackAboveDim(Window flyout)
    {
        try
        {
            flyout.Topmost = false;
            flyout.Topmost = true;
        }
        catch { /* ignore */ }
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
            _focusDim.Show();
            _focusDim.FadeIn();
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

    private void EnsureOutsideClickWatcher(FlyoutWindow flyout)
    {
        if (_outsideClick is null || !_outsideClick.IsActive)
            StartOutsideClickWatcher(flyout);
    }

    private void StopOutsideClickWatcher()
    {
        _outsideClick?.Dispose();
        _outsideClick = null;
    }

    private void DismissTesseraImmediate()
    {
        FlyoutWindow? flyout;
        lock (_gate) _windows.Remove("Tessera", out flyout);
        try { flyout?.Close(); } catch { /* ignore */ }
        StopOutsideClickWatcher();
        CloseFocusDim();
    }

    private Control BuildContent(FlyoutRequest request)
    {
        if (request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
        {
            var material = TesseraFlyoutMaterialFactory.FromPayload(request.Payload);
            TesseraPalette.ApplyMaterial(material);
            var vm = TesseraFlyoutViewModel.FromRequest(_services, request);
            return TesseraStyleFactory.Create(request.StyleId ?? "Fluent", vm);
        }

        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#E6202020")),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(20),
            Child = new TextBlock
            {
                Text = $"{request.ModuleId} · {request.Kind}",
                Foreground = Brushes.White
            }
        };
    }
}

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
            StartLivePump();
        };
        LayoutUpdated += OnLayoutUpdated;
        Closed += (_, _) => StopLivePump();
        ResetDismissTimer();
    }

    private void StartLivePump()
    {
        if (!string.Equals(_request.ModuleId, "Tessera", StringComparison.OrdinalIgnoreCase))
            return;
        _live?.Stop();
        _live = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        double lastVol = double.NaN;
        bool lastMute = false;
        _live.Tick += (_, _) =>
        {
            try
            {
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

                if (Content is TesseraLiveHost host)
                {
                    host.ApplyLive(_services, _request);
                    return;
                }

                var soft = new FlyoutRequest(
                    _request.ModuleId,
                    _request.Kind,
                    _request.StyleId,
                    _request.Anchor,
                    _request.AutoDismissMs,
                    BuildLivePayload(),
                    _request.MonitorIndex,
                    _request.XPad,
                    _request.YPad,
                    _request.Ani,
                    _request.AniDir);
                TryApplyLive(soft, _services, resetDismiss: false);
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

    private Dictionary<string, string> BuildLivePayload()
    {
        var media = _services.Media.Current;
        return new Dictionary<string, string>
        {
            ["volume"] = _services.Audio.MasterVolume.ToString("0.###"),
            ["muted"] = _services.Audio.IsMuted ? "1" : "0",
            ["brightness"] = _services.Brightness.IsSupported
                ? _services.Brightness.Brightness.ToString("0.###")
                : "0.5",
            ["mediaTitle"] = media?.Title ?? "",
            ["mediaArtist"] = media?.Artist ?? "",
            ["mediaPlaying"] = media?.IsPlaying == true ? "1" : "0",
            ["showMediaStrip"] = _request.Payload?.GetValueOrDefault("showMediaStrip") ?? "1",
        };
    }

    public void ApplyLiveOnly(FlyoutRequest request, HostServices services)
    {
        _request = request;
        if (Content is TesseraLiveHost host)
            host.ApplyLive(services, request);
        else
            TryApplyLive(request, services, resetDismiss: false);
    }

    public bool TryApplyLive(FlyoutRequest request, HostServices services, bool resetDismiss = true)
    {
        if (!request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(_request.Kind, request.Kind, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(_request.StyleId ?? "", request.StyleId ?? "", StringComparison.OrdinalIgnoreCase)) return false;
        if (Content is not Visual root) return false;

        var vm = TesseraFlyoutViewModel.FromRequest(services, request);
        var track = FindNamed<TesseraTrack>(root, "TesseraTrack");
        var percent = FindNamed<TextBlock>(root, "TesseraPercent");
        var glyph = FindNamed<Control>(root, "TesseraGlyph");
        var mediaRoot = FindNamed<Control>(root, "TesseraMediaRoot");

        if (vm.ShowMediaStrip != (mediaRoot is not null)
            && (request.Kind.Equals("vol", StringComparison.OrdinalIgnoreCase)
                || request.Kind.Equals("bright", StringComparison.OrdinalIgnoreCase)))
            return false;

        if (Content is TesseraLiveHost liveHost)
        {
            liveHost.ApplyLive(services, request);
            _request = request;
            if (resetDismiss) ResetDismissTimer();
            return true;
        }

        if (track is null && percent is null && mediaRoot is null) return false;

        var value = request.Kind.Equals("bright", StringComparison.OrdinalIgnoreCase)
            ? vm.Brightness
            : services.Audio.MasterVolume;
        var muted = services.Audio.IsMuted;
        if (track is null || !track.IsUserAdjusting)
            track?.SetValueSilent(value);
        if (percent is not null)
        {
            var shown = track is { IsUserAdjusting: true } ? track.Value : value;
            percent.Text = request.Kind.Equals("bright", StringComparison.OrdinalIgnoreCase)
                ? $"{VolumePercent.ToPercent(shown)}"
                : muted ? "Mute" : $"{VolumePercent.ToPercent(shown)}";
        }
        if (glyph is not null && !request.Kind.Equals("bright", StringComparison.OrdinalIgnoreCase))
        {
            var g = track is { IsUserAdjusting: true } ? track.Value : value;
            var kind = muted || g <= 0.001
                ? Material.Icons.MaterialIconKind.VolumeOff
                : g < 0.20 ? Material.Icons.MaterialIconKind.VolumeLow
                : g < 0.50 ? Material.Icons.MaterialIconKind.VolumeMedium
                : Material.Icons.MaterialIconKind.VolumeHigh;
            if (glyph is Material.Icons.Avalonia.MaterialIcon mi)
                mi.Kind = kind;
        }

        ApplyMediaLive(root, vm, services.Media.Current);

        _request = request;
        if (resetDismiss) ResetDismissTimer();
        return true;
    }

    private static void ApplyMediaLive(Visual root, TesseraFlyoutViewModel vm, MediaSessionInfo? media)
    {
        var titleText = media?.Title ?? vm.MediaTitle;
        var artistText = media?.Artist ?? vm.MediaArtist;
        var playing = media?.IsPlaying ?? vm.IsPlaying;
        var posSec = media?.PositionSeconds ?? vm.MediaPositionSeconds;
        var durSec = media?.DurationSeconds ?? vm.MediaDurationSeconds;
        var progress = durSec > 0.5 ? Math.Clamp(posSec / durSec, 0, 1) : 0;
        var thumb = TesseraLiveHost.ResolveThumbnail(media?.ThumbnailPng ?? vm.ThumbnailPng, titleText);

        if (FindNamed<TextBlock>(root, "TesseraMediaTitle") is { } title)
            title.Text = string.IsNullOrWhiteSpace(titleText) ? " " : titleText;
        if (FindNamed<TextBlock>(root, "TesseraMediaArtist") is { } artist)
            artist.Text = string.IsNullOrWhiteSpace(artistText) ? " " : artistText;

        if (FindNamed<Border>(root, "TesseraMediaArt") is { } art)
        {
            var fillHost = double.IsNaN(art.Width) || art.Width <= 1.0;
            TesseraMediaPanel.ApplyArtToBorder(art, thumb, fillHost);
        }

        if (FindNamed<Border>(root, "TesseraMediaWash") is { } wash)
            TesseraMediaPanel.ApplyArtToBorder(wash, thumb, fillHost: true);

        if (FindNamed<Button>(root, "TesseraPlayPause") is { Content: Material.Icons.Avalonia.MaterialIcon playIcon })
            playIcon.Kind = playing
                ? Material.Icons.MaterialIconKind.Pause
                : Material.Icons.MaterialIconKind.Play;

        if (FindNamed<TesseraTrack>(root, "TesseraMediaScrub") is { } scrub)
            scrub.SetValueSilent(progress);
        if (FindNamed<TextBlock>(root, "TesseraMediaPos") is { } pos)
            pos.Text = FormatMediaTime(posSec);
        if (FindNamed<TextBlock>(root, "TesseraMediaDur") is { } dur)
            dur.Text = FormatMediaTime(durSec);
    }

    private static string FormatMediaTime(double seconds)
    {
        if (seconds <= 0 || double.IsNaN(seconds)) return "0:00";
        var t = TimeSpan.FromSeconds(seconds);
        return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
    }

    private static T? FindNamed<T>(Visual root, string name) where T : class
    {
        if (root is Control { Name: { } n } && n == name && root is T direct)
            return direct;
        foreach (var child in root.GetVisualChildren())
        {
            if (child is Control c && c.Name == name && child is T match)
                return match;
            if (child is Visual nested)
            {
                var found = FindNamed<T>(nested, name);
                if (found is not null) return found;
            }
        }
        return null;
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

internal sealed class FocusDimWindow : Window
{
    private int _monitorIndex;

    private const int GwlExStyle = -20;

    public FocusDimWindow(int monitorIndexOneBased)
    {
        _monitorIndex = monitorIndexOneBased;
        Title = "MosaicShell - Focus dim";
        SystemDecorations = SystemDecorations.None;
        CanResize = false;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        Focusable = false;
        IsHitTestVisible = false;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Background = Brushes.Transparent;
        Opacity = 0;
        Content = new Border
        {
            IsHitTestVisible = false,
            Background = new SolidColorBrush(Color.FromArgb(68, 17, 17, 27))
        };
        Opened += (_, _) =>
        {
            PlaceOnMonitor(_monitorIndex);
            ApplyWin32ClickThrough();
        };
    }

    public void PlaceOnMonitor(int monitorIndexOneBased)
    {
        _monitorIndex = monitorIndexOneBased;
        try
        {
            var screens = Screens?.All?.ToList() ?? [];
            var screen = ResolveScreen(screens, _monitorIndex) ?? Screens?.Primary;
            if (screen is null) return;

            var bounds = screen.Bounds;
            var scale = screen.Scaling > 0.1 ? screen.Scaling : 1.0;
            Position = new PixelPoint(bounds.X, bounds.Y);
            Width = Math.Max(1, bounds.Width / scale);
            Height = Math.Max(1, bounds.Height / scale);
            ApplyWin32ClickThrough();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FocusDim] {ex.Message}");
        }
    }

    public void FadeIn()
    {
        Opacity = 0;
        AnimateOpacity(0, 1, 180);
    }

    public void InstantClose()
    {
        try
        {
            Opacity = 0;
            Close();
        }
        catch { /* ignore */ }
    }

    private void ApplyWin32ClickThrough()
    {
        try
        {
            var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (handle == IntPtr.Zero) return;

            var current = GetWindowLongPtr(handle, GwlExStyle);
            var next = current | 0x80800A0; // WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE
            if (next != current)
                SetWindowLongPtr(handle, GwlExStyle, next);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FocusDim click-through] {ex.Message}");
        }
    }

    private void AnimateOpacity(double from, double to, int ms)
    {
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(ms),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0.0), Setters = { new Setter(OpacityProperty, from) } },
                new KeyFrame { Cue = new Cue(1.0), Setters = { new Setter(OpacityProperty, to) } }
            }
        };
        _ = animation.RunAsync(this);
    }

    private static Screen? ResolveScreen(IReadOnlyList<Screen> screens, int monitorIndexOneBased)
    {
        if (screens.Count == 0) return null;
        if (monitorIndexOneBased <= 1)
            return screens.FirstOrDefault(s => s.IsPrimary) ?? screens[0];

        var idx = Math.Clamp(monitorIndexOneBased - 1, 0, screens.Count - 1);
        return screens[idx];
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);
}

internal sealed class TesseraOutsideClickWatcher : IDisposable
{
    private delegate nint LowLevelMouseProc(int nCode, nint wParam, nint lParam);

    // Fields are written by Win32 via SetWindowsHookEx / Marshal.PtrToStructure.
#pragma warning disable CS0649
    private struct Point
    {
        public int x;
        public int y;
    }

    private struct MsllHookStruct
    {
        public Point pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public nint dwExtraInfo;
    }
#pragma warning restore CS0649

    private readonly FlyoutWindow _flyout;
    private readonly Action _dismiss;
    private nint _hook;
    private LowLevelMouseProc? _proc;

    private const int WhMouseLl = 14;
    private const int WmLButtonDown = 0x0201;
    private const int WmRButtonDown = 0x0204;
    private const int WmMButtonDown = 0x0207;
    private const int WmNcLButtonDown = 0x00A1;

    public bool IsActive => _hook != IntPtr.Zero;

    public TesseraOutsideClickWatcher(FlyoutWindow flyout, Action dismiss)
    {
        _flyout = flyout;
        _dismiss = dismiss;
    }

    public void Start()
    {
        if (IsActive) return;
        _proc = HookCallback;
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        using var module = process.MainModule;
        var hMod = module is null ? IntPtr.Zero : GetModuleHandle(module.ModuleName);
        _hook = SetWindowsHookEx(WhMouseLl, _proc, hMod, 0);
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
        _proc = null;
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            var msg = (int)wParam;
            if (msg is WmNcLButtonDown or WmLButtonDown or WmRButtonDown or WmMButtonDown)
            {
                try
                {
                    var info = Marshal.PtrToStructure<MsllHookStruct>(lParam);
                    if (!PointHitsFlyout(info.pt.x, info.pt.y))
                        Dispatcher.UIThread.Post(_dismiss, DispatcherPriority.Send);
                }
                catch { /* ignore */ }
            }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private bool PointHitsFlyout(int screenX, int screenY)
    {
        try
        {
            if (!_flyout.IsVisible) return false;

            var position = _flyout.Position;
            var bounds = _flyout.Bounds;
            if (bounds.Width < 2 || bounds.Height < 2) return false;

            var screens = _flyout.Screens?.All?.ToList() ?? [];
            var screen = screens.FirstOrDefault(s =>
            {
                var b = s.Bounds;
                return screenX >= b.X && screenX < b.X + b.Width
                       && screenY >= b.Y && screenY < b.Y + b.Height;
            }) ?? _flyout.Screens?.Primary;
            var scale = screen?.Scaling > 0.1 ? screen.Scaling : 1.0;
            var w = (int)Math.Ceiling(bounds.Width * scale);
            var h = (int)Math.Ceiling(bounds.Height * scale);
            return screenX >= position.X && screenX < position.X + w
                   && screenY >= position.Y && screenY < position.Y + h;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string lpModuleName);
}
