using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons.Avalonia;

namespace MosaicShell.Host.Tiles.Tessera
{
    internal static partial class TesseraLayouts
    {

        public static Control Square(TesseraFlyoutViewModel vm)
        {
            if (IsStatus(vm))
            {
                return StatusChip(vm, TesseraSquareMetrics.CornerRadius);
            }

            const double size = TesseraSquareMetrics.Size;
            const double r = TesseraSquareMetrics.CornerRadius;
            Control glyph = TesseraVolumeGlyph.Create(vm, TesseraSquareMetrics.GlyphSize);
            glyph.Name = "TesseraGlyph";
            glyph.HorizontalAlignment = HorizontalAlignment.Center;
            TextBlock percent = new()
            {
                Text = vm.PrimaryPercent,
                FontSize = TesseraSquareMetrics.PercentSize,
                FontWeight = FontWeight.SemiBold,
                Foreground = TesseraPalette.FontBrush,
                FontFamily = new FontFamily("Segoe UI Variable, Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Name = "TesseraPercent"
            };
            TesseraTrack track = new()
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
            StackPanel overlay = new()
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 8,
                IsHitTestVisible = false,
                Children = { glyph, percent }
            };
            TesseraRevealHost overlayHost = TesseraRevealHostFactory.WrapMediaFromCatalog(vm, overlay);
            Grid inner = new()
            {
                Width = size,
                Height = size,
                ClipToBounds = true,
                Children = { track, overlayHost }
            };
            BindWheel(inner, vm);
            TesseraLiveAmbient.RegisterVolume(track, percent, glyph as MaterialIcon);
            return TesseraChrome.Glass(inner, r, w: size, h: size);

        }
    }
}
