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

    public static Control Compact(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 12);

        var stacked = TesseraStackedBuildContext.TryCreatePanel(
            vm,
            buildVolume: CompactVolumeCore,
            buildMedia: v => TesseraMediaPanel.Create(v, TesseraMediaMode.SimpleRow),
            wrapVolume: CompactVolumeWrap,
            wrapMedia: static p => p);
        if (stacked is not null)
            return stacked;

        var vol = CompactVolumeWrap(CompactVolumeCore(vm));
        if (!vm.ShowMediaStrip) return vol;
        TesseraFlyoutTweenTargetCatalog.TryResolveMediaRestSizeDip(StyleIds.Compact, out var mediaW, out var mediaH);
        return new StackPanel
        {
            Spacing = 10,
            Children =
            {
                vol,
                TesseraRevealHost.WrapMedia(
                    vm,
                    TesseraMediaPanel.Create(vm, TesseraMediaMode.SimpleRow),
                    fullMediaWidth: mediaW,
                    fullMediaHeight: mediaH)
            }
        };
    }

    private static Control CompactVolumeCore(TesseraFlyoutViewModel vm)
    {
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
        var panel = new StackPanel
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
        if (TesseraStackedBuildContext.IsActive && TesseraGlass.UseOsAcrylicChrome)
        {
            return new Border
            {
                Width = TesseraStackedPlacementSpec.CompactVolumeWidthDip,
                Height = TesseraStackedPlacementSpec.CompactVolumeHeightDip,
                Padding = new Thickness(12, 10),
                HorizontalAlignment = HorizontalAlignment.Center,
                ClipToBounds = true,
                Child = inner
            };
        }

        return TesseraChrome.Glass(inner, 12, new Thickness(12, 10), w: 280);
    }
}
