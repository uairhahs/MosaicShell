using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
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

    public AvaloniaFlyoutPresenter(HostServices services) => _services = services;

    public void Show(FlyoutRequest request) =>
        Dispatcher.UIThread.Post(() => SafeShowOrUpdate(request));

    public void Update(FlyoutRequest request) =>
        Dispatcher.UIThread.Post(() => SafeShowOrUpdate(request));

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
        });
    }

    public bool IsVisible(string moduleId)
    {
        lock (_gate)
            return _windows.TryGetValue(moduleId, out var w) && w.IsVisible;
    }

    private void SafeShowOrUpdate(FlyoutRequest request)
    {
        try { ShowOrUpdateCore(request); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Tessera flyout] {ex}"); }
    }

    private void ShowOrUpdateCore(FlyoutRequest request)
    {
        lock (_gate)
        {
            if (_windows.TryGetValue(request.ModuleId, out var existing) && existing.IsVisible)
            {
                // Same kind/style → update track/percent in place for realtime feel
                if (existing.TryApplyLive(request, _services))
                    return;
                existing.ApplyRequest(request, BuildContent(request));
                return;
            }

            if (_windows.TryGetValue(request.ModuleId, out var old))
            {
                try { old.Close(); } catch { /* ignore */ }
                _windows.Remove(request.ModuleId);
            }
        }

        var content = BuildContent(request);
        var window = new FlyoutWindow(request, content);
        window.Closed += (_, _) => { lock (_gate) _windows.Remove(request.ModuleId); };
        lock (_gate) _windows[request.ModuleId] = window;
        window.Show();
        window.PlayShowAnimation();
    }

    private Control BuildContent(FlyoutRequest request)
    {
        if (request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
        {
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
    private DispatcherTimer? _dismiss;
    private bool _hover;
    private Size _lastSize;

    public FlyoutWindow(FlyoutRequest request, Control content)
    {
        _request = request;
        Title = $"MosaicShell — {request.ModuleId}";
        SizeToContent = SizeToContent.WidthAndHeight;
        CanResize = false;
        SystemDecorations = SystemDecorations.None;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        var styleLooksAcrylic = StyleLooksAcrylic(request);
        TransparencyLevelHint = styleLooksAcrylic
            ? [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Transparent]
            : [WindowTransparencyLevel.Transparent];
        Background = Brushes.Transparent;
        Content = content;
        Opacity = 1;
        PointerEntered += (_, _) => { _hover = true; };
        PointerExited += (_, _) => { _hover = false; };
        PointerWheelChanged += OnWheel;
        Opened += (_, _) => Relayout();
        LayoutUpdated += OnLayoutUpdated;
        ResetDismissTimer();
    }

    private static bool StyleLooksAcrylic(FlyoutRequest request)
    {
        if (!request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase)) return false;
        var s = (request.StyleId ?? "Fluent").ToLowerInvariant();
        return s is "fluent" or "win11" or "modern" or "coreui";
    }

    public bool TryApplyLive(FlyoutRequest request, HostServices services)
    {
        if (!request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(_request.Kind, request.Kind, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(_request.StyleId ?? "", request.StyleId ?? "", StringComparison.OrdinalIgnoreCase)) return false;
        if (Content is not Visual root) return false;

        var vm = TesseraFlyoutViewModel.FromRequest(services, request);
        var track = FindNamed<TesseraTrack>(root, "TesseraTrack");
        var percent = FindNamed<TextBlock>(root, "TesseraPercent");
        var glyph = FindNamed<Control>(root, "TesseraGlyph");
        if (track is null && percent is null) return false;

        // Prefer live endpoint level (ModernFlyouts pattern) over stale payload parse
        var value = request.Kind.Equals("bright", StringComparison.OrdinalIgnoreCase)
            ? vm.Brightness
            : services.Audio.MasterVolume;
        var muted = services.Audio.IsMuted;
        track?.SetValueSilent(value);
        if (percent is not null)
        {
            percent.Text = request.Kind.Equals("bright", StringComparison.OrdinalIgnoreCase)
                ? $"{(int)(value * 100)}"
                : muted ? "Mute" : $"{(int)(value * 100)}";
        }
        if (glyph is not null && !request.Kind.Equals("bright", StringComparison.OrdinalIgnoreCase))
        {
            var kind = muted || value <= 0.001
                ? Material.Icons.MaterialIconKind.VolumeOff
                : value < 0.20 ? Material.Icons.MaterialIconKind.VolumeLow
                : value < 0.50 ? Material.Icons.MaterialIconKind.VolumeMedium
                : Material.Icons.MaterialIconKind.VolumeHigh;
            if (glyph is Material.Icons.Avalonia.MaterialIcon mi)
                mi.Kind = kind;
        }

        _request = request;
        ResetDismissTimer();
        return true;
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
            var w = (int)Math.Ceiling(Math.Max(Bounds.Width, DesiredSize.Width));
            var h = (int)Math.Ceiling(Math.Max(Bounds.Height, DesiredSize.Height));
            if (w < 40 || h < 24) return; // wait for real measure — do not invent BR-sized fallbacks

            var screens = Screens?.All;
            var count = screens?.Count ?? 0;
            var idx = count == 0 ? 0 : Math.Clamp(_request.MonitorIndex - 1, 0, count - 1);
            var screen = count > 0 ? screens![idx] : Screens?.Primary;
            var area = screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);
            var (x, y) = FlyoutAnchor.Compute(
                area.X, area.Y, area.Width, area.Height,
                w, h,
                _request.Anchor ?? "TL",
                _request.XPad,
                _request.YPad);
            Position = new PixelPoint(x, y);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Tessera position] {ex.Message}");
        }
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
