using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
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
