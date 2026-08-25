using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using Material.Icons.Avalonia;
using MosaicShell.Core.Services;

using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;

namespace MosaicShell.Host.Tiles.Tessera;

internal static partial class TesseraLayouts
{

    public static Control ModernFlyouts(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 12);

        var stacked = TesseraStackedBuildContext.TryCreatePanel(
            vm,
            buildVolume: ModernVolumeCore,
            buildMedia: v => TesseraMediaPanel.Create(v, TesseraMediaMode.ModernCard),
            wrapVolume: ModernVolumeWrap,
            wrapMedia: static p => p);
        if (stacked is not null)
            return stacked;

        var vol = ModernVolumeWrap(ModernVolumeCore(vm));
        if (!vm.ShowMediaStrip) return vol;
        return new StackPanel
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
        var glyph = TesseraVolumeGlyph.Create(vm, 16);
        glyph.Name = "TesseraGlyph";
        var track = new TesseraTrack
        {
            IsVertical = false,
            Width = 220,
            Height = 28,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack",
            TrackThickness = 4
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        var percent = new TextBlock
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
        var panel = new StackPanel
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
        if (TesseraStackedBuildContext.IsActive && TesseraGlass.UseOsAcrylicChrome)
        {
            return new Border
            {
                Width = TesseraStackedPlacementSpec.ModernFlyoutsVolumeWidthDip,
                Height = TesseraStackedPlacementSpec.ModernFlyoutsVolumeHeightDip,
                Padding = new Thickness(14, 10),
                HorizontalAlignment = HorizontalAlignment.Center,
                ClipToBounds = true,
                Child = inner
            };
        }

        return TesseraChrome.Glass(inner, 12, new Thickness(14, 10), w: 320);
    }
}
