using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;

namespace MosaicShell.Host.Tiles.Tessera;

internal static partial class TesseraLayouts
{
    public static Control Meter(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 16);

        var stacked = TesseraStackedBuildContext.TryCreatePanel(
            vm,
            buildVolume: MeterVolumeCore,
            buildMedia: v => TesseraMediaPanel.Create(v, TesseraMediaMode.MeterCard),
            wrapVolume: MeterVolumeWrap,
            wrapMedia: static p => p);
        if (stacked is not null)
            return stacked;

        var volPill = MeterVolumeWrap(MeterVolumeCore(vm));
        if (!vm.ShowMediaStrip)
            return volPill;

        TesseraFlyoutTweenTargetCatalog.TryResolveMediaRestSizeDip(StyleIds.Meter, out var mediaW, out var mediaH);
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children =
            {
                TesseraRevealHost.WrapMedia(
                    vm,
                    TesseraMediaPanel.Create(vm, TesseraMediaMode.MeterCard),
                    fullMediaWidth: mediaW,
                    fullMediaHeight: mediaH),
                volPill
            }
        };
    }

    private static Control MeterVolumeCore(TesseraFlyoutViewModel vm)
    {
        const double w = TesseraStackedPlacementSpec.MeterVolumeWidthDip;
        const double h = TesseraStackedPlacementSpec.MeterVolumeHeightDip;
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

        var stackedOsAcrylic = TesseraStackedBuildContext.IsActive && TesseraGlass.UseOsAcrylicChrome;

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
            GlassFill = !stackedOsAcrylic
                && !TesseraGlass.SuppressInnerSkiaGlass
                && !TesseraStackedBuildContext.IsActive,
            AccentBrushOverride = TesseraPalette.AccentBrush,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);

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
        return inner;
    }

    private static Control MeterVolumeWrap(Control inner)
    {
        const double w = TesseraStackedPlacementSpec.MeterVolumeWidthDip;
        const double h = TesseraStackedPlacementSpec.MeterVolumeHeightDip;
        const double r = 14;

        if (TesseraStackedBuildContext.IsActive && TesseraGlass.UseOsAcrylicChrome)
        {
            return new Border
            {
                Width = w,
                Height = h,
                CornerRadius = new CornerRadius(TesseraStackedPlacementSpec.MeterVolumeCornerRadiusDip),
                Background = Brushes.Transparent,
                ClipToBounds = true,
                Child = inner
            };
        }

        return TesseraGlass.SuppressInnerSkiaGlass
            ? TesseraChrome.SolidPill(inner, TesseraChrome.TileFace, r, w, h)
            : TesseraChrome.Glass(inner, r, w: w, h: h);
    }
}
