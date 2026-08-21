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
        double? minWidth = null)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(fill ?? TesseraPalette.Primary),
            CornerRadius = new CornerRadius(cornerRadius),
            BorderBrush = TesseraPalette.StrokeBrush,
            BorderThickness = new Thickness(1),
            Padding = padding ?? new Thickness(0),
            // Don't ClipToBounds on the stroked shell — clips the outline off rounded corners
            ClipToBounds = false,
            Child = cornerRadius > 0
                ? new Border
                {
                    CornerRadius = new CornerRadius(Math.Max(0, cornerRadius - 0.5)),
                    ClipToBounds = true,
                    Child = child
                }
                : child
        };
        if (width is not null) border.MinWidth = width.Value; // MinWidth — allow media strip to widen naturally
        if (width is not null && height is null) { /* width as hint via MinWidth only */ }
        else if (width is not null) border.Width = width.Value;
        if (height is not null) border.Height = height.Value;
        if (minWidth is not null) border.MinWidth = minWidth.Value;
        return border;
    }
}
