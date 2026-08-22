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

    public static Control Simple(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 12);
        var glyph = TesseraVolumeGlyph.Create(vm, 16);
        glyph.Name = "TesseraGlyph";
        var track = new TesseraTrack
        {
            IsVertical = false,
            Width = 180,
            Height = 26,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack",
            TrackThickness = 3
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        var percent = new TextBlock
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
        var vol = TesseraChrome.Glass(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children = { glyph, track, percent }
        }, 12, new Thickness(12, 10), w: 280);
        BindWheel(vol, vm);
        if (!vm.ShowMediaStrip) return vol;
        return new StackPanel
        {
            Spacing = 10,
            Children = { vol, TesseraMediaPanel.Create(vm, TesseraMediaMode.SimpleRow) }
        };

    }
}