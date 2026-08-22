using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Scale;
using MosaicShell.Core.Services;
using MosaicShell.Host.Tiles.Surfaces;

namespace MosaicShell.Host.Tiles;

/// <summary>Host UI hooks for tile overlays (configure / refresh). Set from App.</summary>
public static class TileHostUiBridge
{
    public static Action<string>? OpenModuleConfig { get; set; }
    public static Action<string>? RefreshOverlay { get; set; }
}

public sealed class AvaloniaTileSurfaceHost : ITileSurfaceHost
{
    private readonly Dictionary<string, TileOverlayWindow> _windows = new(StringComparer.OrdinalIgnoreCase);
    private readonly HostServices _services;
    private readonly Func<double> _userScale;
    private readonly Action<string>? _onClosedByUser;

    public AvaloniaTileSurfaceHost(
        HostServices services,
        Func<double> userScale,
        Action<string>? onClosedByUser = null)
    {
        _services = services;
        _userScale = userScale;
        _onClosedByUser = onClosedByUser;
    }

    public bool Show(string moduleId, out string? error) =>
        Show(moduleId, null, out error);

    public bool Show(string moduleId, TileSessionState? restore, out string? error)
    {
        try
        {
            if (_windows.ContainsKey(moduleId))
            {
                Focus(moduleId);
                error = null;
                return true;
            }

            if (!ModuleCatalog.TryGet(moduleId, out var info) || info is null)
            {
                error = $"Unknown module '{moduleId}'.";
                return false;
            }

            var surface = TileSurfaceFactory.Create(info, _services);
            var window = new TileOverlayWindow(info, surface, _userScale());
            window.Closed += (_, _) =>
            {
                PersistAll();
                _windows.Remove(moduleId);
                _onClosedByUser?.Invoke(moduleId);
            };
            window.PropertyChanged += (_, e) =>
            {
                if (e.Property == Window.WindowStateProperty
                    || e.Property == Layoutable.WidthProperty
                    || e.Property == Layoutable.HeightProperty)
                    PersistAll();
            };

            if (restore is not null)
            {
                window.Width = Math.Max(window.MinWidth, restore.Width);
                window.Height = Math.Max(window.MinHeight, restore.Height);
                window.Position = new PixelPoint(restore.X, restore.Y);
            }
            else
            {
                var offset = _windows.Count * 28;
                window.Position = new PixelPoint(80 + offset, 80 + offset);
            }

            window.Show();
            _windows[moduleId] = window;
            PersistAll();
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public void Focus(string moduleId)
    {
        if (!_windows.TryGetValue(moduleId, out var window)) return;
        if (!window.IsVisible) window.Show();
        if (window.IsDesktopWidget)
            window.SendToDesktop();
        else
        {
            window.BringToFront();
            window.Activate();
        }
    }

    public void Close(string moduleId)
    {
        if (!_windows.TryGetValue(moduleId, out var window)) return;
        _windows.Remove(moduleId);
        window.Close();
        PersistAll();
    }

    public void CloseAll()
    {
        foreach (var id in _windows.Keys.ToList())
            Close(id);
    }

    public void Refresh(string moduleId)
    {
        if (!_windows.TryGetValue(moduleId, out var window)) return;
        var state = new TileSessionState(
            moduleId, window.Position.X, window.Position.Y, window.Width, window.Height);
        Close(moduleId);
        Show(moduleId, state, out _);
    }

    public IReadOnlyList<string> OpenModuleIds => _windows.Keys.ToList();

    public void PersistAll()
    {
        var states = _windows.Values.Select(w => new TileSessionState(
            w.ModuleId,
            w.Position.X,
            w.Position.Y,
            w.Width,
            w.Height)).ToList();
        SessionStore.Save(states);
    }

    public void ApplyUserScale(double scale)
    {
        foreach (var w in _windows.Values)
            w.ApplyScale(scale);
    }
}

/// <summary>
/// Borderless desktop/capability frame. Content fills the shell (no nested title chrome).
/// Desktop-widget chrome: drag whole surface; manage via right-click Ctx (align / Z / configure / close).
/// </summary>
public sealed class TileOverlayWindow : Window
{
    public string ModuleId { get; }
    public bool IsDesktopWidget { get; }
    private readonly LayoutTransformControl _scaler;
    private bool _stuckToDesktop;

    public TileOverlayWindow(ModuleInfo info, Control surface, double userScale)
    {
        ModuleId = info.Id;
        IsDesktopWidget = info.Kind == ModuleKind.Widget;
        _stuckToDesktop = IsDesktopWidget
            || info.Id.Equals("Pulse", StringComparison.OrdinalIgnoreCase);

        Title = $"MosaicShell: {info.DisplayName}";
        ApplyDefaultSize(info);
        MinWidth = 160;
        MinHeight = 100;
        CanResize = true;
        SystemDecorations = SystemDecorations.None;
        Topmost = !IsDesktopWidget && !_stuckToDesktop;
        ShowInTaskbar = false;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        Background = Brushes.Transparent;

        var shell = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#E61e1e2e")),
            CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Color.Parse("#45475a")),
            BorderThickness = new Thickness(1),
            ClipToBounds = true,
            // Content is the frame - no title strip.
            Child = new Border
            {
                Padding = new Thickness(IsDesktopWidget ? 12 : 14),
                Child = surface
            }
        };
        shell.PointerPressed += OnSurfacePointerPressed;
        shell.ContextMenu = BuildContextMenu();

        _scaler = new LayoutTransformControl
        {
            Child = shell,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        ApplyScale(userScale);
        Content = _scaler;

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && !IsDesktopWidget)
            {
                e.Handled = true;
                Close();
            }
        };
    }

    private void ApplyDefaultSize(ModuleInfo info)
    {
        if (info.Id.Equals("Canvas", StringComparison.OrdinalIgnoreCase))
        {
            Width = 340;
            Height = 420;
            return;
        }

        if (info.Kind == ModuleKind.Widget)
        {
            Width = 340;
            Height = 300;
            return;
        }

        Width = 420;
        Height = 360;
    }

    public void ApplyScale(double userScale)
    {
        var s = Math.Clamp(userScale, 0.75, 2.0);
        _scaler.LayoutTransform = new ScaleTransform(s, s);
    }

    public void SendToDesktop()
    {
        _stuckToDesktop = true;
        Topmost = false;
        SetZOrder(HwndBottom);
    }

    public void BringToFront()
    {
        _stuckToDesktop = false;
        Topmost = true;
        SetZOrder(HwndTopmost);
        Activate();
    }

    public void SetNormalZ()
    {
        _stuckToDesktop = false;
        Topmost = false;
        SetZOrder(HwndNoTopmost);
    }

    public void AlignTo(AlignPreset preset)
    {
        var screen = Screens?.ScreenFromWindow(this) ?? Screens?.Primary;
        if (screen is null) return;
        var wa = screen.WorkingArea;
        var scale = screen.Scaling > 0.1 ? screen.Scaling : 1.0;
        var w = (int)Math.Round(Bounds.Width * scale);
        var h = (int)Math.Round(Bounds.Height * scale);
        var x = preset switch
        {
            AlignPreset.TopLeft or AlignPreset.BottomLeft => wa.X + 16,
            AlignPreset.TopRight or AlignPreset.BottomRight => wa.X + wa.Width - w - 16,
            AlignPreset.Center or AlignPreset.HorizontalCenter or AlignPreset.VerticalCenter
                or AlignPreset.TopCenter or AlignPreset.BottomCenter =>
                wa.X + (wa.Width - w) / 2,
            _ => Position.X
        };
        var y = preset switch
        {
            AlignPreset.TopLeft or AlignPreset.TopRight or AlignPreset.TopCenter => wa.Y + 16,
            AlignPreset.BottomLeft or AlignPreset.BottomRight or AlignPreset.BottomCenter =>
                wa.Y + wa.Height - h - 16,
            AlignPreset.Center or AlignPreset.HorizontalCenter or AlignPreset.VerticalCenter =>
                wa.Y + (wa.Height - h) / 2,
            _ => Position.Y
        };
        if (preset == AlignPreset.HorizontalCenter)
            y = Position.Y;
        if (preset == AlignPreset.VerticalCenter)
            x = Position.X;
        Position = new PixelPoint(x, y);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (_stuckToDesktop)
            SendToDesktop();
    }

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();

        var configure = new MenuItem { Header = "Configure in Host" };
        configure.Click += (_, _) => TileHostUiBridge.OpenModuleConfig?.Invoke(ModuleId);
        menu.Items.Add(configure);

        var align = new MenuItem { Header = "Align" };
        align.Items.Add(AlignItem("Center", AlignPreset.Center));
        align.Items.Add(AlignItem("Horizontally centered", AlignPreset.HorizontalCenter));
        align.Items.Add(AlignItem("Vertically centered", AlignPreset.VerticalCenter));
        align.Items.Add(new Separator());
        align.Items.Add(AlignItem("Top left", AlignPreset.TopLeft));
        align.Items.Add(AlignItem("Top center", AlignPreset.TopCenter));
        align.Items.Add(AlignItem("Top right", AlignPreset.TopRight));
        align.Items.Add(AlignItem("Bottom left", AlignPreset.BottomLeft));
        align.Items.Add(AlignItem("Bottom center", AlignPreset.BottomCenter));
        align.Items.Add(AlignItem("Bottom right", AlignPreset.BottomRight));
        menu.Items.Add(align);

        var z = new MenuItem { Header = "Change Z layer" };
        var desk = new MenuItem { Header = "Desktop (behind windows)" };
        desk.Click += (_, _) => SendToDesktop();
        var normal = new MenuItem { Header = "Normal" };
        normal.Click += (_, _) => SetNormalZ();
        var top = new MenuItem { Header = "Always on top" };
        top.Click += (_, _) => BringToFront();
        z.Items.Add(desk);
        z.Items.Add(normal);
        z.Items.Add(top);
        menu.Items.Add(z);

        menu.Items.Add(new Separator());

        var refresh = new MenuItem { Header = "Refresh" };
        refresh.Click += (_, _) => TileHostUiBridge.RefreshOverlay?.Invoke(ModuleId);
        menu.Items.Add(refresh);

        var close = new MenuItem { Header = "Unload" };
        close.Click += (_, _) => Close();
        menu.Items.Add(close);

        return menu;
    }

    private MenuItem AlignItem(string header, AlignPreset preset)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => AlignTo(preset);
        return item;
    }

    private void OnSurfacePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Drag from empty chrome / non-interactive padding; don't steal button/slider drags.
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (e.Source is Button or Slider or TextBox or ComboBox) return;
        if (e.Source is Control c && (c is TextBox || AncestorsContainInteractive(c)))
            return;
        BeginMoveDrag(e);
    }

    private static bool AncestorsContainInteractive(Control control)
    {
        for (var p = control.Parent; p is not null; p = p.Parent)
        {
            if (p is Button or Slider or TextBox or ComboBox or ScrollViewer)
                return true;
        }
        return false;
    }

    private void SetZOrder(IntPtr insertAfter)
    {
        try
        {
            var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (hwnd == IntPtr.Zero) return;
            SetWindowPos(hwnd, insertAfter, 0, 0, 0, 0,
                SwpNomove | SwpNosize | SwpNoactivate);
        }
        catch
        {
            // best-effort
        }
    }

    private static readonly IntPtr HwndBottom = new(1);
    private static readonly IntPtr HwndTopmost = new(-1);
    private static readonly IntPtr HwndNoTopmost = new(-2);
    private const uint SwpNomove = 0x0002;
    private const uint SwpNosize = 0x0001;
    private const uint SwpNoactivate = 0x0010;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
}

public enum AlignPreset
{
    Center,
    HorizontalCenter,
    VerticalCenter,
    TopLeft,
    TopCenter,
    TopRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}
