using Avalonia.Controls;
using Material.Icons;
using Material.Icons.Avalonia;

namespace MosaicShell.Host.Tiles.Tessera
{
    public static class TesseraVolumeGlyph
    {
        /// <summary>YourFlyouts Win11 ladder: mute / low / mid / high / full.</summary>
        public static Control Create(TesseraFlyoutViewModel vm, double size = 20)
        {
            if (vm.Kind.Equals("bright", StringComparison.OrdinalIgnoreCase))
            {
                MaterialIconKind bright = vm.Brightness < 0.5 ? MaterialIconKind.Brightness5 : MaterialIconKind.Brightness7;
                return Icon(bright, size);
            }

            if (vm.Kind.Equals("locks", StringComparison.OrdinalIgnoreCase))
            {
                return Icon(MaterialIconKind.AlphaCCircle, size);
            }

            if (vm.Kind.Equals("flight", StringComparison.OrdinalIgnoreCase))
            {
                return Icon(MaterialIconKind.Airplane, size);
            }

            if (vm.Kind.Equals("media", StringComparison.OrdinalIgnoreCase))
            {
                return Icon(MaterialIconKind.Music, size);
            }

            return vm.IsMuted || vm.Volume <= 0.001
                ? Icon(MaterialIconKind.VolumeOff, size)
                : vm.Volume < 0.20
                ? Icon(MaterialIconKind.VolumeLow, size)
                : vm.Volume < 0.50 ? Icon(MaterialIconKind.VolumeMedium, size) : Icon(MaterialIconKind.VolumeHigh, size);
        }

        private static MaterialIcon Icon(MaterialIconKind kind, double size)
        {
            return new()
            {
                Kind = kind,
                Width = size,
                Height = size,
                Foreground = TesseraPalette.FontBrush
            };
        }
    }
}
