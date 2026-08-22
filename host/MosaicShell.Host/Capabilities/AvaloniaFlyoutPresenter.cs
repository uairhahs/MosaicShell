using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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
}

public sealed class AvaloniaFlyoutPresenter : IFlyoutPresenter
{
    private readonly HostServices _services;
    private IHostUiBridge? _hostUi;
    private readonly object _gate = new();
    private readonly Dictionary<string, FlyoutWindow> _windows = new(StringComparer.OrdinalIgnoreCase);
    private FocusDimWindow? _focusDim;
    private TesseraOutsideClickWatcher? _outsideClick;
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MosaicShell", "Cache", "flyout.log");

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
        if (IsImmediateStatusKind(request))
            Dispatcher.UIThread.Invoke(() => SafeShowOrUpdate(request, resetDismiss: true));
        else
            Dispatcher.UIThread.Post(() => SafeShowOrUpdate(request, resetDismiss: true));
    }

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
            catch (Exception ex) { Log($"soft refresh {ex}"); }
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
        catch (Exception ex)
        {
            Log($"EXCEPTION {ex}");
            CloseFocusDim();
        }
    }

    private void ShowOrUpdateCore(FlyoutRequest request, bool resetDismiss = true)
    {
        Log($"ShowOrUpdateCore enter kind={request.Kind}");
        lock (_gate)
        {
            if (_windows.TryGetValue(request.ModuleId, out var existing) && existing.IsVisible)
            {
                if (existing.TryApplyLive(request, _services, resetDismiss))
                {
                    existing.EnsureLivePump();
                    PresentFlyout(existing, request);
                    return;
                }
                existing.ApplyRequest(request, BuildContent(request));
                existing.EnsureLivePump();
                PresentFlyout(existing, request);
                return;
            }

            if (_windows.TryGetValue(request.ModuleId, out var old))
            {
                try { old.Close(); } catch { /* ignore */ }
                _windows.Remove(request.ModuleId);
            }
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
        }

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

        var owner = ResolveOwnerWindow();
        if (owner is not null)
            window.Show(owner);
        else
            window.Show();

        window.EnsureLivePump();
        window.PlayShowAnimation();
        PresentFlyout(window, request);
    }

    private static Window? ResolveOwnerWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    private void PresentFlyout(FlyoutWindow window, FlyoutRequest request)
    {
        window.FinishLayout();
        // Dim after flyout is placed so it never races above an empty flyout.
        SyncFocusDim(request);
        RestackAboveDim(window);

        // docs: ActualTransparencyLevel reports what the OS actually granted
        Log(
            $"presented kind={request.Kind} visible={window.IsVisible} " +
            $"bounds={window.Bounds.Width:0}x{window.Bounds.Height:0} " +
            $"desired={window.DesiredSize.Width:0}x{window.DesiredSize.Height:0} " +
            $"pos={window.Position} scaling={window.RenderScaling:0.##} " +
            $"hint={string.Join('|', window.TransparencyLevelHint)} " +
            $"actual={window.ActualTransparencyLevel}");

        if (request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
        {
            Dispatcher.UIThread.Post(
                () =>
                {
                    if (window.IsVisible)
                        StartOutsideClickWatcher(window);
                },
                DispatcherPriority.Background);
        }
    }

    private static void RestackAboveDim(Window flyout)
    {
        try
        {
            flyout.Topmost = false;
            flyout.Topmost = true;
            flyout.Activate();
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
            // BitBlt glass is opt-in (AllowGdiScreenCapture); default uses Avalonia transparency + Skia frost.
            var wantBlur = TesseraFlyoutRequestBuilder.BackdropBlurFromPayload(request.Payload);
            TesseraGlass.UseBackdropBlur = wantBlur && TesseraGlass.AllowGdiScreenCapture;
            var vm = TesseraFlyoutViewModel.FromRequest(_services, request, _hostUi);
            Control root = TesseraStyleFactory.Create(request.StyleId ?? "Fluent", vm);
            var scale = FlyoutScaleFromPayload(request.Payload);
            if (Math.Abs(scale - 1.0) > 0.01)
            {
                root = new LayoutTransformControl
                {
                    LayoutTransform = new ScaleTransform(scale, scale),
                    Child = root
                };
            }

            // No opaque host wrap — window transparency + Tessera chrome tint provide the surface.
            return root;
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

    private static void Log(string message)
    {
        try
        {
            AppPaths.EnsureLayout();
            File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch { /* ignore */ }
        Console.WriteLine($"[Tessera flyout] {message}");
    }
}
