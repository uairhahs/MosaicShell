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

namespace MosaicShell.Host.Tiles.Tessera;

internal static partial class TesseraLayouts
{

    public static Control Gnome(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 24);

        var stacked = TesseraStackedBuildContext.TryCreatePanel(
            vm,
            buildVolume: GnomeVolumeCore,
            buildMedia: v => TesseraMediaPanel.Create(v, TesseraMediaMode.GnomePill),
            wrapVolume: GnomeVolumeWrap,
            wrapMedia: static p => p);
        if (stacked is not null)
            return stacked;

        var volPill = GnomeVolumeWrap(GnomeVolumeCore(vm));
        if (!vm.ShowMediaStrip)
            return volPill;

        var mediaHost = TesseraRevealHost.WrapMedia(vm, TesseraMediaPanel.Create(vm, TesseraMediaMode.GnomePill));
        var volHost = TesseraRevealHost.WrapMedia(vm, volPill);

        return new StackPanel
        {
            Spacing = 10,
            Children = { mediaHost, volHost }
        };
    }

    private static Control GnomeVolumeCore(TesseraFlyoutViewModel vm)
    {
        var volTrack = new TesseraTrack
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
        var glyph = TesseraVolumeGlyph.Create(vm, 18);
        glyph.Name = "TesseraGlyph";
        TesseraLiveAmbient.RegisterVolume(volTrack, null, glyph as MaterialIcon);
        var panel = new StackPanel
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
        if (TesseraStackedBuildContext.IsActive && TesseraGlass.UseOsAcrylicChrome)
        {
            return new Border
            {
                Width = TesseraStackedPlacementSpec.GnomeVolumeWidthDip,
                Height = TesseraStackedPlacementSpec.GnomeVolumeHeightDip,
                Padding = new Thickness(14, 10),
                HorizontalAlignment = HorizontalAlignment.Center,
                ClipToBounds = true,
                Child = inner
            };
        }

        return TesseraChrome.Glass(inner, 28, new Thickness(14, 10), w: 240);
    }
}
