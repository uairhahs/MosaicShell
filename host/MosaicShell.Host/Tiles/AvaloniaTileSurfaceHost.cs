using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Scale;
using MosaicShell.Core.Services;
using MosaicShell.Host.Tiles.Surfaces;

namespace MosaicShell.Host.Tiles;

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
                window.Width = restore.Width;
                window.Height = restore.Height;
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
        window.Activate();
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

public sealed class TileOverlayWindow : Window
{
    public string ModuleId { get; }
    private readonly LayoutTransformControl _scaler;

    public TileOverlayWindow(ModuleInfo info, Control surface, double userScale)
    {
        ModuleId = info.Id;
        Title = $"MosaicShell: {info.DisplayName}";
        Width = 380;
        Height = 300;
        MinWidth = 240;
        MinHeight = 160;
        CanResize = true;
        SystemDecorations = SystemDecorations.None;
        Topmost = true;
        ShowInTaskbar = false;
        TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Transparent];
        Background = Brushes.Transparent;

        var shell = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#E61e1e2e")),
            CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Color.Parse("#45475a")),
            BorderThickness = new Thickness(1),
            ClipToBounds = true
        };

        var root = new DockPanel();
        var chrome = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#33202233")),
            Padding = new Thickness(12, 8),
            Child = BuildChrome(info.DisplayName)
        };
        DockPanel.SetDock(chrome, Dock.Top);
        chrome.PointerPressed += OnChromePointerPressed;
        root.Children.Add(chrome);
        root.Children.Add(new Border { Padding = new Thickness(14), Child = surface });
        shell.Child = root;

        _scaler = new LayoutTransformControl { Child = shell };
        ApplyScale(userScale);
        Content = _scaler;
    }

    public void ApplyScale(double userScale)
    {
        var s = Math.Clamp(userScale, 0.75, 2.0);
        _scaler.LayoutTransform = new ScaleTransform(s, s);
    }

    private Control BuildChrome(string title)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto") };
        var titleBlock = new TextBlock
        {
            Text = title,
            FontWeight = FontWeight.SemiBold,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.Parse("#cdd6f4")),
            VerticalAlignment = VerticalAlignment.Center
        };
        var hide = new Button
        {
            Content = "-", Width = 28, Height = 24, Padding = new Thickness(0),
            Background = Brushes.Transparent, Foreground = new SolidColorBrush(Color.Parse("#a6adc8")), Tag = "hide"
        };
        var close = new Button
        {
            Content = "×", Width = 28, Height = 24, Padding = new Thickness(0),
            Background = Brushes.Transparent, Foreground = new SolidColorBrush(Color.Parse("#a6adc8")), Tag = "close"
        };
        Grid.SetColumn(hide, 1);
        Grid.SetColumn(close, 2);
        grid.Children.Add(titleBlock);
        grid.Children.Add(hide);
        grid.Children.Add(close);
        return grid;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (Content is not LayoutTransformControl { Child: Border { Child: DockPanel dock } }) return;
        if (dock.Children[0] is not Border { Child: Grid chrome }) return;
        foreach (var child in chrome.Children.OfType<Button>())
        {
            if (Equals(child.Tag, "hide")) child.Click += (_, _) => Hide();
            if (Equals(child.Tag, "close")) child.Click += (_, _) => Close();
        }
    }

    private void OnChromePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }
}
