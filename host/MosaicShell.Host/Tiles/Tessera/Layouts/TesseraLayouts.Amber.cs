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

    public static Control Amber(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 16);
        var track = new TesseraTrack
        {
            IsVertical = true,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack",
            FatThumb = false,
            ShowThumb = true,
            TrackThickness = 28,
            TrackPad = 0,
            ShellRadius = 14,
            GlassFill = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        TesseraLiveAmbient.RegisterVolume(track, null, null);
        var volPill = TesseraChrome.Glass(track, 14, w: 28, h: 200);
        BindWheel(volPill, vm);

        if (!vm.ShowMediaStrip)
            return volPill;

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children =
            {
                TesseraMediaPanel.Create(vm, TesseraMediaMode.AmberCard),
                volPill
            }
        };

    }
}