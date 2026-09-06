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

        public static Control Compact(TesseraFlyoutViewModel vm)
        {
            if (IsStatus(vm))
            {
                return StatusChip(vm, 12);
            }

            Control? stacked = TesseraStackedBuildContext.TryCreatePanel(
                vm,
                buildVolume: CompactVolumeCore,
                buildMedia: v => TesseraMediaPanel.Create(v, TesseraMediaMode.SimpleRow),
                wrapVolume: CompactVolumeWrap,
                wrapMedia: static p => p);
            if (stacked is not null)
            {
                return stacked;
            }

            Control vol = CompactVolumeWrap(CompactVolumeCore(vm));
            return !vm.ShowMediaStrip
                ? vol
                : new StackPanel
                {
                    Spacing = 10,
                    Children =
                {
                    vol,
                    TesseraRevealHostFactory.WrapMediaFromCatalog(
                        vm,
                        TesseraMediaPanel.Create(vm, TesseraMediaMode.SimpleRow))
                }
                };
        }

        private static Control CompactVolumeCore(TesseraFlyoutViewModel vm)
        {
            Control glyph = TesseraVolumeGlyph.Create(vm, 16);
            glyph.Name = "TesseraGlyph";
            TesseraTrack track = new()
            {
                IsVertical = false,
                Width = 180,
                Height = 26,
                Value = vm.PrimaryValue,
                Name = "TesseraTrack",
                TrackThickness = 3
            };
            track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
            TextBlock percent = new()
            {
                Text = vm.PrimaryPercent,
                FontSize = 14,
                Width = 36,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = TesseraPalette.FontBrush,
                Name = "TesseraPercent"
            };
            TesseraLiveAmbient.RegisterVolume(track, percent, glyph as MaterialIcon);
            StackPanel panel = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Children = { glyph, track, percent }
            };
            BindWheel(panel, vm);
            return panel;
        }

        private static Control CompactVolumeWrap(Control inner)
        {
            return TesseraStackedBuildContext.IsActive && TesseraGlass.UseOsAcrylicChrome
                ? new Border
                {
                    Width = TesseraStackedPlacementSpec.CompactVolumeWidthDip,
                    Height = TesseraStackedPlacementSpec.CompactVolumeHeightDip,
                    Padding = new Thickness(12, 10),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    ClipToBounds = true,
                    Child = inner
                }
                : TesseraChrome.Glass(inner, 12, new Thickness(12, 10), w: 280);
        }
    }
}
