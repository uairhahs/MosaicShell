using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MosaicShell.Host.ViewModels;

namespace MosaicShell.Host.Views;

/// <summary>
/// Keep the Host at a stable fraction of the current screen working area when moving
/// between monitors (Avalonia Screens: WorkingArea=pixels, Width/Height=DIPs, Scaling=DPI).
///
/// Important: only <see cref="WindowResizeReason.User"/> updates the stored fraction.
/// DPI / layout / application resizes must not — that was poisoning frac≈0.97 and making
/// the window fill the destination display (see host-size.log).
/// </summary>
public partial class MainWindow : Window
{
    private const double DefaultFracW = 0.55;
    private const double DefaultFracH = 0.65;
    private const double MaxRememberedFrac = 0.90;
    private const double MinW = 780;
    private const double MinH = 560;

    private double _fracW = DefaultFracW;
    private double _fracH = DefaultFracH;
    private PixelRect _lastSeenWorkArea;
    private double _lastSeenScaling = double.NaN;
    private bool _applying;
    private bool _suppressCapture;
    private EventHandler? _screensChanged;
    private int _applyGeneration;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Seed fraction from the designed startup size once — never from a DPI-bloated size.
        CaptureFractionFromCurrentSize(userInitiated: false);
        SanitizeFraction();
        SyncLastSeenScreen(ResolveScreen());

        ScalingChanged += OnScalingChanged;
        PositionChanged += OnPositionChanged;
        if (Screens is not null)
        {
            _screensChanged = (_, _) => RequestFractionApply("screens", force: true, deferPasses: 2);
            Screens.Changed += _screensChanged;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        ScalingChanged -= OnScalingChanged;
        PositionChanged -= OnPositionChanged;
        if (Screens is not null && _screensChanged is not null)
            Screens.Changed -= _screensChanged;
        base.OnClosed(e);
    }

    private void OnScalingChanged(object? sender, EventArgs e) =>
        RequestFractionApply("scaling", force: true, deferPasses: 3);

    private void OnPositionChanged(object? sender, PixelPointEventArgs e) =>
        RequestFractionApply("position", force: false, deferPasses: 2);

    protected override void OnResized(WindowResizedEventArgs e)
    {
        base.OnResized(e);
        if (_applying || _suppressCapture)
            return;

        // Only an explicit user drag of the resize border/corner may change the remembered ratio.
        // DpiChange / Layout / Application / Unspecified during monitor moves must not.
        if (e.Reason == WindowResizeReason.User)
        {
            CaptureFractionFromCurrentSize(userInitiated: true);
            SanitizeFraction();
            Log($"capture-user frac={_fracW:0.###}x{_fracH:0.###} client={ClientSize.Width:0}x{ClientSize.Height:0}");
            return;
        }

        if (e.Reason == WindowResizeReason.DpiChange)
            RequestFractionApply("dpi-resize", force: true, deferPasses: 3);
    }

    private void RequestFractionApply(string reason, bool force, int deferPasses)
    {
        var gen = ++_applyGeneration;
        _suppressCapture = true;

        for (var i = 0; i < Math.Max(1, deferPasses); i++)
        {
            var pass = i;
            Dispatcher.UIThread.Post(
                () =>
                {
                    if (gen != _applyGeneration)
                        return;
                    ApplyFractionToCurrentScreen(force || pass > 0, reason, pass);
                    if (pass == deferPasses - 1)
                    {
                        // One more tick later, allow user captures again.
                        Dispatcher.UIThread.Post(
                            () =>
                            {
                                if (gen == _applyGeneration)
                                    _suppressCapture = false;
                            },
                            DispatcherPriority.Background);
                    }
                },
                pass == 0 ? DispatcherPriority.Background : DispatcherPriority.Render);
        }
    }

    private void ApplyFractionToCurrentScreen(bool force, string reason, int pass)
    {
        if (_applying)
            return;

        var screen = ResolveScreen();
        if (screen is null)
            return;

        SanitizeFraction();

        var scaling = screen.Scaling > 0.1 ? screen.Scaling : 1.0;
        var work = screen.WorkingArea;
        var workDipW = Math.Max(1.0, work.Width / scaling);
        var workDipH = Math.Max(1.0, work.Height / scaling);
        var targetW = workDipW * _fracW;
        var targetH = workDipH * _fracH;

        var screenChanged = work != _lastSeenWorkArea
                            || double.IsNaN(_lastSeenScaling)
                            || Math.Abs(scaling - _lastSeenScaling) >= 0.001;
        var sizeOff = Math.Abs(Width - targetW) > 4 || Math.Abs(Height - targetH) > 4;
        var desktopScale = DesktopScaling > 0.1 ? DesktopScaling : RenderScaling;
        var scalingSettled = Math.Abs(desktopScale - scaling) < 0.05;

        if (!force && !screenChanged && !sizeOff)
            return;

        if (!scalingSettled && pass == 0)
        {
            RequestFractionApply(reason + "+wait-scale", force: true, deferPasses: 2);
            return;
        }

        _applying = true;
        _suppressCapture = true;
        try
        {
            MinWidth = 1;
            MinHeight = 1;

            var w = Math.Clamp(targetW, Math.Min(MinW, workDipW - 4), Math.Max(1, workDipW - 4));
            var h = Math.Clamp(targetH, Math.Min(MinH, workDipH - 4), Math.Max(1, workDipH - 4));
            Width = w;
            Height = h;

            // Always restore design mins — never pin Min* to the applied size.
            MinWidth = MinW;
            MinHeight = MinH;

            SyncLastSeenScreen(screen);
            Log(
                $"{reason} pass={pass} force={force} settled={scalingSettled} " +
                $"frac={_fracW:0.###}x{_fracH:0.###} " +
                $"workPx={work.Width}x{work.Height} scale={scaling:0.##} desk={desktopScale:0.##} " +
                $"→ {Width:0}x{Height:0}dip");
        }
        finally
        {
            _applying = false;
        }
    }

    private void CaptureFractionFromCurrentSize(bool userInitiated)
    {
        var screen = ResolveScreen();
        if (screen is null)
            return;

        var scaling = screen.Scaling > 0.1 ? screen.Scaling : 1.0;
        var work = screen.WorkingArea;
        var workDipW = Math.Max(1.0, work.Width / scaling);
        var workDipH = Math.Max(1.0, work.Height / scaling);

        var w = ClientSize.Width > 1 ? ClientSize.Width : Width;
        var h = ClientSize.Height > 1 ? ClientSize.Height : Height;
        if (double.IsNaN(w) || double.IsNaN(h) || w < 80 || h < 60)
            return;

        var nextW = Math.Clamp(w / workDipW, 0.15, MaxRememberedFrac);
        var nextH = Math.Clamp(h / workDipH, 0.15, MaxRememberedFrac);

        // Startup / non-user: only accept a sane mid-range fraction; otherwise keep defaults.
        if (!userInitiated && (nextW > 0.85 || nextH > 0.85))
            return;

        _fracW = nextW;
        _fracH = nextH;
    }

    private void SanitizeFraction()
    {
        if (_fracW is < 0.15 or > MaxRememberedFrac) _fracW = DefaultFracW;
        if (_fracH is < 0.15 or > MaxRememberedFrac) _fracH = DefaultFracH;
    }

    private Screen? ResolveScreen()
    {
        try
        {
            var scale = RenderScaling > 0.1 ? RenderScaling : 1.0;
            var cx = Position.X + (int)Math.Round(Math.Max(ClientSize.Width, Width) * scale / 2.0);
            var cy = Position.Y + (int)Math.Round(Math.Max(ClientSize.Height, Height) * scale / 2.0);
            var fromPoint = Screens?.ScreenFromPoint(new PixelPoint(cx, cy));
            if (fromPoint is not null)
                return fromPoint;
        }
        catch
        {
            // fall through
        }

        return Screens?.ScreenFromWindow(this) ?? Screens?.Primary;
    }

    private void SyncLastSeenScreen(Screen? screen)
    {
        if (screen is null) return;
        _lastSeenWorkArea = screen.WorkingArea;
        _lastSeenScaling = screen.Scaling > 0.1 ? screen.Scaling : 1.0;
    }

    private static void Log(string message)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MosaicShell", "Cache");
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "host-size.log"),
                $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch
        {
            // ignore
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.IsCapturingHotkey && vm.TryCaptureHotkey(e))
            return;
        base.OnKeyDown(e);
    }

    private void OnDiscoverCardPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not DiscoverCard card)
            return;
        if (DataContext is MainViewModel vm)
            vm.OpenCardCommand.Execute(card);
    }

    private void OnLibraryTileBodyPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Control source && source.FindAncestorOfType<Button>(includeSelf: true) is not null)
            return;
        if (sender is not Border border || border.DataContext is not LibraryItemViewModel item)
            return;
        if (DataContext is MainViewModel vm)
            vm.OpenModuleConfigCommand.Execute(item);
        e.Handled = true;
    }
}
