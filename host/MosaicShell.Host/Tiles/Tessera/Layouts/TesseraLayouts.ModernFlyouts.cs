using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons.Avalonia;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Tiles.Tessera
{
    internal static partial class TesseraLayouts
    {

        public static Control ModernFlyouts(TesseraFlyoutViewModel vm)
        {
            if (IsStatus(vm))
            {
                return StatusChip(vm, 12);
            }

            Control? stacked = TesseraStackedBuildContext.TryCreatePanel(
                vm,
                buildVolume: ModernVolumeCore,
                buildMedia: v => TesseraMediaPanel.Create(v, TesseraMediaMode.ModernCard),
                wrapVolume: ModernVolumeWrap,
                wrapMedia: static p => p);
            if (stacked is not null)
            {
                return stacked;
            }

            Control vol = ModernVolumeWrap(ModernVolumeCore(vm));
            return !vm.ShowMediaStrip
                ? vol
                : new StackPanel
                {
                    Spacing = 12,
                    Children =
                {
                    vol,
                    TesseraRevealHostFactory.WrapMediaFromCatalog(
                        vm,
                        TesseraMediaPanel.Create(vm, TesseraMediaMode.ModernCard))
                }
                };
        }

        private static Control ModernVolumeCore(TesseraFlyoutViewModel vm)
        {
            Control glyph = TesseraVolumeGlyph.Create(vm, 16);
            glyph.Name = "TesseraGlyph";
            TesseraTrack track = new()
            {
                IsVertical = false,
                Width = 220,
                Height = 28,
                Value = vm.PrimaryValue,
                Name = "TesseraTrack",
                TrackThickness = 4
            };
            track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
            TextBlock percent = new()
            {
                Text = vm.PrimaryPercent,
                FontSize = 14,
                Width = 40,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = TesseraPalette.FontBrush,
                Name = "TesseraPercent"
            };
            TesseraLiveAmbient.RegisterVolume(track, percent, glyph as MaterialIcon);
            StackPanel panel = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                Children = { glyph, track, percent }
            };
            BindWheel(panel, vm);
            return panel;
        }

        private static Control ModernVolumeWrap(Control inner)
        {
            return TesseraStackedBuildContext.IsActive && TesseraGlass.UseOsAcrylicChrome
                ? new Border
                {
                    Width = TesseraStackedPlacementSpec.ModernFlyoutsVolumeWidthDip,
                    Height = TesseraStackedPlacementSpec.ModernFlyoutsVolumeHeightDip,
                    Padding = new Thickness(14, 10),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    ClipToBounds = true,
                    Child = inner
                }
                : TesseraChrome.Glass(inner, 12, new Thickness(14, 10), w: 320);
        }
    }
}
