using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using Material.Icons.Avalonia;
using MosaicShell.Core.Services;

namespace MosaicShell.Host.Tiles.Tessera;

internal static partial class TesseraLayouts
{

    private static Control StatusChip(TesseraFlyoutViewModel vm, double radius, double? w = null, double? h = null)
    {
        var label = TesseraChrome.Label(vm.KindLabel, 14);
        label.Name = "TesseraStatusLabel";
        TesseraLiveAmbient.RegisterStatus(label);
        return TesseraChrome.Glass(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(16, 0),
            Children =
            {
                TesseraVolumeGlyph.Create(vm, 16),
                label
            }
        }, radius, new Thickness(12, 10), w, h ?? 50);
    }

    private static Control CoreUiIconBtn(MaterialIconKind kind, Action act, double size)
    {
        var icon = new MaterialIcon
        {
            Kind = kind,
            Width = size * 0.55,
            Height = size * 0.55,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = TesseraPalette.FontBrush
        };
        return TesseraChrome.IconButton(icon, act, size, circularHighlight: false,
            hover: TesseraStylePalette.CoreUi.IconHoverBrush);
    }

    private static Control IconBtn(MaterialIconKind kind, Action act, double size = 36)
    {
        var b = new Border
        {
            Width = size,
            Height = size,
            Background = Brushes.Transparent,
            Child = new MaterialIcon
            {
                Kind = kind,
                Width = size * 0.55,
                Height = size * 0.55,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = TesseraPalette.FontBrush
            }
        };
        b.PointerPressed += (_, e) => { act(); e.Handled = true; };
        return b;
    }

    private static Control PlayPill(TesseraFlyoutViewModel vm)
    {
        var icon = new MaterialIcon
        {
            Kind = vm.IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play,
            Width = 18,
            Height = 18,
            Foreground = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var pill = new Border
        {
            Width = 36,
            Height = 44,
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Color.FromRgb(235, 235, 240)),
            Child = icon
        };
        pill.PointerPressed += (_, e) => { _ = vm.PlayPauseAsync(); e.Handled = true; };
        return pill;
    }

    private static MaterialIcon PixelIcon(MaterialIconKind kind, bool muted = false) =>
        new()
        {
            Kind = kind,
            Width = TesseraStyleMetrics.PixelIconSize,
            Height = TesseraStyleMetrics.PixelIconSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = muted
                ? TesseraStylePalette.Pixel.SecondaryBrush
                : TesseraStylePalette.Pixel.AccentBrush
        };

    private static Border PixelIconTile(MaterialIconKind kind, double iconSize, double tile)
    {
        var tileBorder = new Border
        {
            Width = tile,
            Height = tile,
            CornerRadius = new CornerRadius(tile / 2),
            Background = TesseraStylePalette.Pixel.ShellBrush,
            Child = new MaterialIcon
            {
                Kind = kind,
                Width = iconSize,
                Height = iconSize,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = TesseraStylePalette.Pixel.AccentBrush
            }
        };
        TesseraChrome.ApplyHoverHighlight(tileBorder, TesseraStylePalette.Pixel.ShellBrush,
            new SolidColorBrush(Color.FromRgb(40, 40, 44)));
        return tileBorder;
    }

    private static Control PixelIconBtn(MaterialIconKind kind, Action act) =>
        TesseraChrome.IconButton(PixelIcon(kind), act, TesseraStyleMetrics.PixelHitTarget, circularHighlight: true);

    private static Control PixelIconBtn(MaterialIcon icon, Action act) =>
        TesseraChrome.IconButton(icon, act, TesseraStyleMetrics.PixelHitTarget, circularHighlight: true);

    private static Control PixelPlayPill(TesseraFlyoutViewModel vm)
    {
        var icon = new MaterialIcon
        {
            Kind = vm.IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play,
            Width = TesseraStyleMetrics.PixelIconSize,
            Height = TesseraStyleMetrics.PixelIconSize,
            Foreground = TesseraStylePalette.Pixel.OnAccentBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (TesseraLiveAmbient.Current is { } live)
            live.PlayPauseIcon = icon;
        var normal = TesseraStylePalette.Pixel.AccentBrush;
        var hover = new SolidColorBrush(Color.FromRgb(240, 244, 255));
        var pill = new Border
        {
            Width = TesseraStyleMetrics.PixelPlayW,
            Height = TesseraStyleMetrics.PixelPlayH,
            CornerRadius = new CornerRadius(12),
            Background = normal,
            ClipToBounds = true,
            Child = icon
        };
        TesseraChrome.ApplyHoverHighlight(pill, normal, hover);
        pill.PointerPressed += (_, e) => { _ = vm.PlayPauseAsync(); e.Handled = true; };
        return pill;
    }

    private static async Task PixelToggleShuffleAsync(TesseraFlyoutViewModel vm, MaterialIcon icon)
    {
        await vm.Services.Media.ToggleShuffleAsync();
        var on = icon.Kind != MaterialIconKind.ShuffleVariant;
        icon.Kind = on ? MaterialIconKind.ShuffleVariant : MaterialIconKind.Shuffle;
        icon.Foreground = on
            ? TesseraStylePalette.Pixel.AccentBrush
            : TesseraStylePalette.Pixel.SecondaryBrush;
    }

    private static async Task PixelToggleRepeatAsync(TesseraFlyoutViewModel vm, MaterialIcon icon)
    {
        await vm.Services.Media.ToggleRepeatAsync();
        if (icon.Kind == MaterialIconKind.RepeatOne)
        {
            icon.Kind = MaterialIconKind.Repeat;
            icon.Foreground = TesseraStylePalette.Pixel.SecondaryBrush;
        }
        else if (icon.Kind == MaterialIconKind.Repeat)
        {
            icon.Kind = MaterialIconKind.RepeatOne;
            icon.Foreground = TesseraStylePalette.Pixel.AccentBrush;
        }
        else
        {
            icon.Kind = MaterialIconKind.Repeat;
            icon.Foreground = TesseraStylePalette.Pixel.AccentBrush;
        }
    }

    private static async Task PixelToggleLikeAsync(TesseraFlyoutViewModel vm, MaterialIcon icon)
    {
        var wantLiked = icon.Kind != MaterialIconKind.Heart;
        await vm.Services.Media.ToggleLikeAsync(wantLiked);
        icon.Kind = wantLiked ? MaterialIconKind.Heart : MaterialIconKind.HeartOutline;
        icon.Foreground = wantLiked
            ? TesseraStylePalette.Pixel.AccentBrush
            : TesseraStylePalette.Pixel.SecondaryBrush;
    }

    private static void BindWheel(Control c, TesseraFlyoutViewModel vm, double step = 0.02) =>
        c.PointerWheelChanged += (_, e) =>
        {
            vm.Nudge(e.Delta.Y > 0 ? 0.02 : -0.02);
            e.Handled = true;
        };

    private static bool IsStatus(TesseraFlyoutViewModel vm) =>
        vm.Kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
        || vm.Kind.Equals("flight", StringComparison.OrdinalIgnoreCase);

    private static string FormatTime(double seconds)
    {
        if (seconds <= 0 || double.IsNaN(seconds)) return "0:00";
        var t = TimeSpan.FromSeconds(seconds);
        return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");

    }
}