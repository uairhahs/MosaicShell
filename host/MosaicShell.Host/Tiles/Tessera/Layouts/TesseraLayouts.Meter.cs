using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Tiles.Tessera;

internal static partial class TesseraLayouts
{
    public static Control Meter(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 16);

        // Match Center’s working GlassFill path: fixed size, no M3 thumb, live percent label.
        // Meter-only glass (old Amber) read as a static mocha block under opaque HWND + Patch.
        const double w = 28;
        const double h = 200;
        const double r = 14;

        var percent = new TextBlock
        {
            Text = vm.PrimaryPercent,
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            Foreground = TesseraPalette.FontBrush,
            FontFamily = new FontFamily("Segoe UI Variable, Segoe UI"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8),
            Name = "TesseraPercent"
        };

        var track = new TesseraTrack
        {
            IsVertical = true,
            Width = w,
            Height = h,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack",
            FatThumb = false,
            ShowThumb = false,
            TrackThickness = w,
            TrackPad = 0,
            ShellRadius = r,
            GlassFill = true,
            AccentBrushOverride = TesseraPalette.AccentBrush,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);

        // Contract: TesseraLayoutCoverage.RequiresLiveVolumePercentLabel("Meter")
        TesseraLiveAmbient.RegisterVolume(track, percent, null);

        var overlay = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            IsHitTestVisible = false,
            Children = { percent }
        };
        var inner = new Grid
        {
            Width = w,
            Height = h,
            ClipToBounds = true,
            Children = { track, overlay }
        };
        BindWheel(inner, vm);
        var volPill = TesseraChrome.Glass(inner, r, w: w, h: h);

        if (!vm.ShowMediaStrip)
            return volPill;

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children =
            {
                TesseraMediaPanel.Create(vm, TesseraMediaMode.MeterCard),
                volPill
            }
        };
    }
}
