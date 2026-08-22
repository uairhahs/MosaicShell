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

    public static Control Center(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, TesseraCenterMetrics.CornerRadius, TesseraCenterMetrics.Size, TesseraCenterMetrics.Size);
        const double size = TesseraCenterMetrics.Size;
        const double r = TesseraCenterMetrics.CornerRadius;
        var glyph = TesseraVolumeGlyph.Create(vm, TesseraCenterMetrics.GlyphSize);
        glyph.Name = "TesseraGlyph";
        glyph.HorizontalAlignment = HorizontalAlignment.Center;
        var percent = new TextBlock
        {
            Text = vm.PrimaryPercent,
            FontSize = TesseraCenterMetrics.PercentSize,
            FontWeight = FontWeight.SemiBold,
            Foreground = TesseraPalette.FontBrush,
            FontFamily = new FontFamily("Segoe UI Variable, Segoe UI"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Name = "TesseraPercent"
        };
        var track = new TesseraTrack
        {
            IsVertical = true,
            Width = size,
            Height = size,
            ShellRadius = r,
            TrackThickness = size,
            TrackPad = 0,
            ShowThumb = false,
            GlassFill = true,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack",
            AccentBrushOverride = TesseraPalette.AccentBrush
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        var overlay = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 8,
            IsHitTestVisible = false,
            Children = { glyph, percent }
        };
        var inner = new Grid
        {
            Width = size,
            Height = size,
            ClipToBounds = true,
            Children = { track, overlay }
        };
        BindWheel(inner, vm);
        TesseraLiveAmbient.RegisterVolume(track, percent, glyph as MaterialIcon);
        return TesseraChrome.Glass(inner, r, w: size, h: size);

    }
}