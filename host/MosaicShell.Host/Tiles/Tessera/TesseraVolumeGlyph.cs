using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material.Icons;
using Material.Icons.Avalonia;

namespace MosaicShell.Host.Tiles.Tessera;

public static class TesseraVolumeGlyph
{
    /// <summary>YourFlyouts Win11 ladder: mute / low / mid / high / full.</summary>
    public static Control Create(TesseraFlyoutViewModel vm, double size = 20)
    {
        if (vm.Kind.Equals("bright", StringComparison.OrdinalIgnoreCase))
        {
            var bright = vm.Brightness < 0.5 ? MaterialIconKind.Brightness5 : MaterialIconKind.Brightness7;
            return Icon(bright, size);
        }

        if (vm.Kind.Equals("locks", StringComparison.OrdinalIgnoreCase))
            return Icon(MaterialIconKind.AlphaCCircle, size);
        if (vm.Kind.Equals("flight", StringComparison.OrdinalIgnoreCase))
            return Icon(MaterialIconKind.Airplane, size);
        if (vm.Kind.Equals("media", StringComparison.OrdinalIgnoreCase))
            return Icon(MaterialIconKind.Music, size);

        if (vm.IsMuted || vm.Volume <= 0.001)
            return Icon(MaterialIconKind.VolumeOff, size);
        if (vm.Volume < 0.20)
            return Icon(MaterialIconKind.VolumeLow, size);
        if (vm.Volume < 0.50)
            return Icon(MaterialIconKind.VolumeMedium, size);
        return Icon(MaterialIconKind.VolumeHigh, size);
    }

    private static MaterialIcon Icon(MaterialIconKind kind, double size) =>
        new()
        {
            Kind = kind,
            Width = size,
            Height = size,
            Foreground = TesseraPalette.FontBrush
        };
}

public static class TesseraShell
{
    public static Border Create(
        Control child,
        double cornerRadius,
        Thickness? padding = null,
        Color? fill = null,
        double? width = null,
        double? height = null,
        double? minWidth = null,
        double? maxWidth = null)
    {
        Control content = child;
        if (TesseraBakedFrost.TryGetBrush(out var frost) && TesseraPalette.UseEdgeBlend)
        {
            content = new Grid
            {
                Children =
                {
                    new Border
                    {
                        Background = frost,
                        Opacity = 0.55,
                        IsHitTestVisible = false
                    },
                    child
                }
            };
        }

        var border = new Border
        {
            Background = TesseraPalette.UseEdgeBlend
                ? TesseraPalette.SoftFrostFill()
                : new SolidColorBrush(fill ?? TesseraPalette.Primary),
            CornerRadius = new CornerRadius(cornerRadius),
            BorderBrush = TesseraPalette.StrokeBrush,
            BorderThickness = new Thickness(1),
            Padding = padding ?? new Thickness(0),
            ClipToBounds = false,
            Child = cornerRadius > 0
                ? new Border
                {
                    CornerRadius = new CornerRadius(Math.Max(0, cornerRadius - 0.5)),
                    ClipToBounds = true,
                    Child = content
                }
                : content
        };
        if (minWidth is not null) border.MinWidth = minWidth.Value;
        if (width is not null) border.MinWidth = width.Value;
        if (width is not null && height is not null) border.Width = width.Value;
        if (height is not null) border.Height = height.Value;
        if (maxWidth is not null) border.MaxWidth = maxWidth.Value;
        return border;
    }
}
