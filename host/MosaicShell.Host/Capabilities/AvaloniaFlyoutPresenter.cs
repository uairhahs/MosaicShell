using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using MosaicShell.Core.Capabilities;
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

    public AvaloniaFlyoutPresenter(HostServices services, IHostUiBridge? hostUi = null)
    {
        _services = services;
        _hostUi = hostUi;
    }

    public void AttachHostUi(IHostUiBridge hostUi) => _hostUi = hostUi;

    public void Show(FlyoutRequest request)
    {
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
                    existing.EnsureLivePump();
                    SyncFocusDim(request);
                    RestackAboveDim(existing);
                    if (request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
                        EnsureOutsideClickWatcher(existing);
                    return;
                }
                existing.ApplyRequest(request, BuildContent(request));
                existing.EnsureLivePump();
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
        window.EnsureLivePump();
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
            TesseraGlass.UseBackdropBlur = TesseraFlyoutRequestBuilder.BackdropBlurFromPayload(request.Payload);
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
            return TesseraChrome.WrapFlyoutContent(root);
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

    private static double FlyoutScaleFromPayload(IReadOnlyDictionary<string, string>? payload)
    {
        if (payload is null || !payload.TryGetValue("flyoutScale", out var raw))
            return 1.0;
        if (!int.TryParse(raw, out var pct))
            return 1.0;
        return Math.Clamp(pct, 50, 150) / 100.0;
    }
}
