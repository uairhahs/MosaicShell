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

    public static Control Gnome(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 24);
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
        var volPill = TesseraChrome.Glass(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { glyph, volTrack }
        }, 28, new Thickness(14, 10), w: 240);
        BindWheel(volPill, vm);

        if (!vm.ShowMediaStrip)
            return volPill;

        return new StackPanel
        {
            Spacing = 10,
            Children =
            {
                TesseraMediaPanel.Create(vm, TesseraMediaMode.GnomePill),
                volPill
            }
        };

    }
}