using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;
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
    private readonly Dictionary<string, FlyoutWindow> _windows = new(StringComparer.OrdinalIgnoreCase);

    public AvaloniaFlyoutPresenter(HostServices services) => _services = services;

    public void Show(FlyoutRequest request)
    {
        Dispatcher.UIThread.Post(() => ShowCore(request));
    }

    public void Hide(string moduleId)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_windows.Remove(moduleId, out var w))
                w.Close();
        });
    }

    public void HideAll()
    {
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var id in _windows.Keys.ToList())
                Hide(id);
        });
    }

    private void ShowCore(FlyoutRequest request)
    {
        if (_windows.TryGetValue(request.ModuleId, out var existing))
        {
            existing.Close();
            _windows.Remove(request.ModuleId);
        }

        Control content = request.ModuleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase)
            ? TesseraStyleFactory.Create(request.StyleId ?? "Fluent", BuildTesseraVm(request.Kind))
            : BuildGeneric(request);

        var window = new FlyoutWindow(request.ModuleId, content, request.AutoDismissMs);
        Position(window, request.Anchor ?? "BR");
        window.Closed += (_, _) => _windows.Remove(request.ModuleId);
        _windows[request.ModuleId] = window;
        window.Show();
    }

    private TesseraFlyoutViewModel BuildTesseraVm(string kind)
    {
        var settings = MosaicShell.Core.Runtime.ModuleSettingsStore.Load("Tessera", () => new TesseraSettings());
        return new TesseraFlyoutViewModel(_services, kind, settings.Style);
    }

    private static Control BuildGeneric(FlyoutRequest request)
    {
        var style = request.StyleId ?? StyleCatalog.DefaultFor(request.ModuleId);
        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#E61e1e2e")),
            CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Color.Parse("#45475a")),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(20),
            MinWidth = 280,
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = request.ModuleId,
                        FontSize = 18,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(Color.Parse("#cdd6f4"))
                    },
                    new TextBlock
                    {
                        Text = $"{request.Kind} · style {style}",
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.Parse("#a6adc8"))
                    }
                }
            }
        };
    }

    private static void Position(Window window, string anchor)
    {
        var screen = window.Screens?.Primary?.WorkingArea
                     ?? new PixelRect(0, 0, 1920, 1080);
        const int pad = 24;
        var w = (int)window.Width;
        var h = (int)window.Height;
        var x = anchor.ToUpperInvariant() switch
        {
            "TL" or "TC" or "TR" => screen.X + pad,
            "CL" => screen.X + pad,
            "CR" => screen.X + screen.Width - w - pad,
            "BL" or "BC" or "BR" => screen.X + screen.Width - w - pad,
            _ => screen.X + screen.Width - w - pad
        };
        if (anchor.Equals("TC", StringComparison.OrdinalIgnoreCase)
            || anchor.Equals("BC", StringComparison.OrdinalIgnoreCase)
            || anchor.Equals("Center", StringComparison.OrdinalIgnoreCase))
            x = screen.X + (screen.Width - w) / 2;
        if (anchor.Equals("TL", StringComparison.OrdinalIgnoreCase)
            || anchor.Equals("BL", StringComparison.OrdinalIgnoreCase)
            || anchor.Equals("CL", StringComparison.OrdinalIgnoreCase))
            x = screen.X + pad;
        if (anchor.Equals("TR", StringComparison.OrdinalIgnoreCase)
            || anchor.Equals("BR", StringComparison.OrdinalIgnoreCase)
            || anchor.Equals("CR", StringComparison.OrdinalIgnoreCase))
            x = screen.X + screen.Width - w - pad;

        var y = anchor.ToUpperInvariant() switch
        {
            "TL" or "TC" or "TR" => screen.Y + pad,
            "CL" or "CR" or "CENTER" => screen.Y + (screen.Height - h) / 2,
            _ => screen.Y + screen.Height - h - pad
        };
        window.Position = new PixelPoint(x, y);
    }
}

internal sealed class FlyoutWindow : Window
{
    private readonly DispatcherTimer? _dismiss;
    private bool _hover;

    public FlyoutWindow(string moduleId, Control content, int autoDismissMs)
    {
        Title = $"MosaicShell — {moduleId}";
        Width = 360;
        Height = 160;
        CanResize = false;
        SystemDecorations = SystemDecorations.None;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        Background = Brushes.Transparent;
        Content = content;
        PointerEntered += (_, _) => _hover = true;
        PointerExited += (_, _) => _hover = false;

        if (autoDismissMs > 0)
        {
            _dismiss = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(autoDismissMs) };
            _dismiss.Tick += (_, _) =>
            {
                if (_hover) return;
                _dismiss.Stop();
                Close();
            };
            _dismiss.Start();
        }
    }
}
