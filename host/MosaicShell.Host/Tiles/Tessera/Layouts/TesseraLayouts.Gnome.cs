using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material.Icons.Avalonia;

namespace MosaicShell.Host.Tiles.Tessera
{
    internal static partial class TesseraLayouts
    {

        public static Control Gnome(TesseraFlyoutViewModel vm)
        {
            if (IsStatus(vm))
            {
                return StatusChip(vm, 24);
            }

            Control? stacked = TesseraStackedBuildContext.TryCreatePanel(
                vm,
                buildVolume: GnomeVolumeCore,
                buildMedia: v => TesseraMediaPanel.Create(v, TesseraMediaMode.GnomePill),
                wrapVolume: inner => TesseraRevealHost.WrapGnomeVolumeFill(vm, GnomeVolumeWrap(inner)),
                wrapMedia: static p => p);
            if (stacked is not null)
            {
                return stacked;
            }

            Control volPill = GnomeVolumeWrap(GnomeVolumeCore(vm));
            if (!vm.ShowMediaStrip)
            {
                return volPill;
            }

            TesseraRevealHost mediaHost = TesseraRevealHostFactory.WrapMediaFromCatalog(vm, TesseraMediaPanel.Create(vm, TesseraMediaMode.GnomePill));
            TesseraRevealHost volHost = TesseraRevealHost.WrapGnomeVolumeFill(vm, volPill);

            return new StackPanel
            {
                Spacing = 10,
                Children = { mediaHost, volHost }
            };
        }

        private static Control GnomeVolumeCore(TesseraFlyoutViewModel vm)
        {
            TesseraTrack volTrack = new()
            {
                IsVertical = false,
                Width = 160,
                Height = 24,
                Value = vm.PrimaryValue,
                Name = "TesseraTrack",
                TrackThickness = 5,
                ShowThumb = false
            };
            volTrack.ValueChanged += (_, v) => vm.ApplyPrimary(v);
            Control glyph = TesseraVolumeGlyph.Create(vm, 18);
            glyph.Name = "TesseraGlyph";
            TesseraLiveAmbient.RegisterVolume(volTrack, null, glyph as MaterialIcon);
            StackPanel panel = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children = { glyph, volTrack }
            };
            BindWheel(panel, vm);
            return panel;
        }

        private static Control GnomeVolumeWrap(Control inner)
        {
            // When using OS acrylic, the stacked window itself provides the background
            // Don't add extra border wrapper that could leave artifacts
            return TesseraStackedBuildContext.IsActive && TesseraGlass.UseOsAcrylicChrome
                ? inner
                : TesseraChrome.Glass(inner, 28, new Thickness(14, 10), w: 240);
        }
    }
}
