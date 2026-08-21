using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MosaicShell.Core.Services;

namespace MosaicShell.Host.Tiles.Tessera;

public sealed class TesseraFlyoutViewModel
{
    public TesseraFlyoutViewModel(HostServices services, string kind, string styleId)
    {
        Services = services;
        Kind = kind;
        StyleId = styleId;
        Volume = services.Audio.MasterVolume;
        IsMuted = services.Audio.IsMuted;
        Brightness = services.Brightness.IsSupported ? services.Brightness.Brightness : 0.5;
        MediaTitle = services.Media.Current?.Title ?? "No media";
        MediaArtist = services.Media.Current?.Artist ?? "";
    }

    public HostServices Services { get; }
    public string Kind { get; }
    public string StyleId { get; }
    public double Volume { get; set; }
    public bool IsMuted { get; set; }
    public double Brightness { get; set; }
    public string MediaTitle { get; }
    public string MediaArtist { get; }

    public void ApplyVolume(double v)
    {
        Volume = v;
        Services.Audio.MasterVolume = v;
    }

    public void ApplyBrightness(double v)
    {
        Brightness = v;
        if (Services.Brightness.IsSupported)
            Services.Brightness.Brightness = v;
    }

    public void ToggleMute()
    {
        IsMuted = !IsMuted;
        Services.Audio.IsMuted = IsMuted;
    }
}

public static class TesseraStyleFactory
{
    public static Control Create(string styleId, TesseraFlyoutViewModel vm) =>
        styleId.ToLowerInvariant() switch
        {
            "win11" => BuildWin11(vm),
            "simple" => BuildSimple(vm),
            "pixel" => BuildPixel(vm),
            "center" => BuildCenter(vm),
            "modern" => BuildModern(vm),
            "amber" => BuildAmber(vm),
            "gnome" => BuildGnome(vm),
            "smouti" => BuildRounded(vm, "#1e1e2e", "#fab387", 20),
            "plainext" => BuildPlainext(vm),
            "coreui" => BuildRounded(vm, "#11111b", "#89b4fa", 10),
            "fluent" or _ => BuildFluent(vm),
        };

    private static Control BuildFluent(TesseraFlyoutViewModel vm) =>
        Shell(vm, "#CC1e1e2e", "#89b4fa", 12, horizontal: true, titleSize: 14);

    private static Control BuildWin11(TesseraFlyoutViewModel vm) =>
        Shell(vm, "#E6202020", "#60cdff", 8, horizontal: true, titleSize: 13, acrylic: true);

    private static Control BuildSimple(TesseraFlyoutViewModel vm) =>
        new Border
        {
            Background = Brush("#F01e1e2e"),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(16, 12),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    Title(vm, 12),
                    ValueText(vm),
                    SliderFor(vm)
                }
            }
        };

    private static Control BuildPixel(TesseraFlyoutViewModel vm) =>
        new Border
        {
            Background = Brush("#FF000000"),
            BorderBrush = Brush("#00ff9c"),
            BorderThickness = new Thickness(2),
            Padding = new Thickness(14),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = Label(vm).ToUpperInvariant(),
                        FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace"),
                        FontSize = 11,
                        Foreground = Brush("#00ff9c")
                    },
                    new TextBlock
                    {
                        Text = ValueString(vm),
                        FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace"),
                        FontSize = 22,
                        FontWeight = FontWeight.Bold,
                        Foreground = Brush("#00ff9c")
                    },
                    SliderFor(vm, accent: "#00ff9c")
                }
            }
        };

    private static Control BuildCenter(TesseraFlyoutViewModel vm) =>
        Shell(vm, "#E6181825", "#cba6f7", 16, horizontal: false, titleSize: 12);

    private static Control BuildModern(TesseraFlyoutViewModel vm) =>
        Shell(vm, "#DD313244", "#a6e3a1", 18, horizontal: true, titleSize: 13);

    private static Control BuildAmber(TesseraFlyoutViewModel vm) =>
        Shell(vm, "#EE2a1f0e", "#f9e2af", 14, horizontal: true, titleSize: 14);

    private static Control BuildGnome(TesseraFlyoutViewModel vm) =>
        Shell(vm, "#E0242424", "#3584e4", 999, horizontal: true, titleSize: 12);

    private static Control BuildPlainext(TesseraFlyoutViewModel vm) =>
        new Border
        {
            Background = Brush("#F0cdd6f4"),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(16),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"{Label(vm)}: {ValueString(vm)}",
                        FontSize = 14,
                        FontWeight = FontWeight.Medium,
                        Foreground = Brush("#1e1e2e")
                    },
                    SliderFor(vm, accent: "#1e1e2e", track: "#bac2de")
                }
            }
        };

    private static Control BuildRounded(
        TesseraFlyoutViewModel vm, string bg, string accent, double radius) =>
        Shell(vm, bg, accent, radius, horizontal: true, titleSize: 13);

    private static Control Shell(
        TesseraFlyoutViewModel vm,
        string bg,
        string accent,
        double radius,
        bool horizontal,
        double titleSize,
        bool acrylic = false)
    {
        var panel = horizontal
            ? new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new StackPanel
                    {
                        Spacing = 2,
                        Width = 90,
                        Children =
                        {
                            Title(vm, titleSize, accent),
                            ValueText(vm, 22, accent)
                        }
                    },
                    SliderFor(vm, accent)
                }
            }
            : new StackPanel
            {
                Spacing = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    Title(vm, titleSize, accent),
                    ValueText(vm, 28, accent),
                    SliderFor(vm, accent)
                }
            };

        return new Border
        {
            Background = Brush(bg),
            CornerRadius = new CornerRadius(radius > 100 ? 40 : radius),
            BorderBrush = acrylic ? Brush("#40ffffff") : Brush("#3345475a"),
            BorderThickness = new Thickness(acrylic ? 1 : 1),
            Padding = new Thickness(18, 14),
            MinWidth = 300,
            Child = panel
        };
    }

    private static TextBlock Title(TesseraFlyoutViewModel vm, double size, string? accent = null) =>
        new()
        {
            Text = Label(vm),
            FontSize = size,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush(accent ?? "#a6adc8")
        };

    private static TextBlock ValueText(TesseraFlyoutViewModel vm, double size = 20, string? accent = null) =>
        new()
        {
            Text = ValueString(vm),
            FontSize = size,
            FontWeight = FontWeight.Bold,
            Foreground = Brush(accent ?? "#cdd6f4")
        };

    private static string Label(TesseraFlyoutViewModel vm) => vm.Kind.ToLowerInvariant() switch
    {
        "bright" => "Brightness",
        "media" => "Now playing",
        "locks" => "Locks",
        "flight" => "Airplane",
        _ => vm.IsMuted ? "Muted" : "Volume"
    };

    private static string ValueString(TesseraFlyoutViewModel vm) => vm.Kind.ToLowerInvariant() switch
    {
        "bright" => $"{(int)(vm.Brightness * 100)}%",
        "media" => string.IsNullOrWhiteSpace(vm.MediaArtist)
            ? vm.MediaTitle
            : $"{vm.MediaTitle} — {vm.MediaArtist}",
        _ => vm.IsMuted ? "Mute" : $"{(int)(vm.Volume * 100)}%"
    };

    private static Control SliderFor(
        TesseraFlyoutViewModel vm,
        string accent = "#89b4fa",
        string track = "#313244")
    {
        if (vm.Kind.Equals("media", StringComparison.OrdinalIgnoreCase))
            return new TextBlock { Text = "", Height = 1 };

        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 1,
            Width = 180,
            Value = vm.Kind.Equals("bright", StringComparison.OrdinalIgnoreCase) ? vm.Brightness : vm.Volume,
            Foreground = Brush(accent),
            Background = Brush(track)
        };
        slider.PropertyChanged += (_, e) =>
        {
            if (e.Property != Slider.ValueProperty) return;
            Dispatcher.UIThread.Post(() =>
            {
                if (vm.Kind.Equals("bright", StringComparison.OrdinalIgnoreCase))
                    vm.ApplyBrightness(slider.Value);
                else
                    vm.ApplyVolume(slider.Value);
            });
        };
        return slider;
    }

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}
