using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MosaicShell.Host.Tiles.Tessera
{
    internal static partial class TesseraLayouts
    {

        public static Control Radial(TesseraFlyoutViewModel vm)
        {
            if (IsStatus(vm))
            {
                return StatusChip(vm, 10);
            }

            Control? stacked = TesseraStackedBuildContext.TryCreatePanel(
                vm,
                buildVolume: RadialVolumeCore,
                buildMedia: v => TesseraRevealHostFactory.WrapMediaFromCatalog(
                    v, TesseraMediaPanel.Create(v, TesseraMediaMode.RadialSide)),
                wrapVolume: RadialVolumeWrap,
                wrapMedia: RadialMediaWrap);
            if (stacked is not null)
            {
                return stacked;
            }

            Control left = RadialVolumeCore(vm);
            Control body = left;
            if (vm.ShowMediaStrip)
            {
                TesseraRevealHost media = TesseraRevealHostFactory.WrapMediaFromCatalog(
                    vm, TesseraMediaPanel.Create(vm, TesseraMediaMode.RadialSide));
                media.HorizontalAlignment = HorizontalAlignment.Right;
                media.VerticalAlignment = VerticalAlignment.Center;

                body = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
                    Children = { left, media }
                };
                Grid.SetColumn(left, 0);
                Grid.SetColumn(media, 2);
            }

            Control shell = TesseraChrome.WithArtWash(body, vm.ThumbnailPng, 10,
                new Thickness(TesseraStyleMetrics.RadialPad, 14),
                TesseraStyleMetrics.RadialWidth,
                TesseraStyleMetrics.RadialMaxHeight);
            shell.MinHeight = TesseraStyleMetrics.RadialMinHeight;
            shell.VerticalAlignment = VerticalAlignment.Top;
            return shell;
        }

        private static Control RadialVolumeCore(TesseraFlyoutViewModel vm)
        {
            const double ringSize = TesseraStyleMetrics.RadialRing;
            TesseraRingVolume ring = new()
            {
                Value = vm.PrimaryValue,
                Width = ringSize,
                Height = ringSize,
                MinWidth = ringSize,
                MinHeight = ringSize,
                MaxWidth = ringSize,
                MaxHeight = ringSize,
                Showcase = true,
                ClipToBounds = true,
                AccentBrushOverride = TesseraStylePalette.Radial.AccentBrush,
                PercentBrushOverride = TesseraStylePalette.Radial.BrightBrush
            };
            ring.ValueChanged += (_, v) => vm.ApplyPrimary(v);
            TesseraLiveAmbient.RegisterRing(ring);
            Border ringHost = new()
            {
                Width = ringSize,
                Height = ringSize,
                ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = ring
            };
            TesseraRevealHost ringReveal = TesseraRevealHost.WrapRadialRingSweep(vm, ringHost, ring);

            StackPanel left = new()
            {
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    TesseraChrome.Label("Audio level", 13, FontWeight.Bold),
                    TesseraChrome.Label("Speakers", 10, muted: true),
                    ringReveal
                }
            };
            BindWheel(ringHost, vm);
            return left;
        }

        private static Control RadialVolumeWrap(Control inner)
        {
            return TesseraChrome.Glass(inner, 10, new Thickness(TesseraStyleMetrics.RadialPad, 14));
        }

        private static Control RadialMediaWrap(Control media)
        {
            media.HorizontalAlignment = HorizontalAlignment.Right;
            media.VerticalAlignment = VerticalAlignment.Center;
            return TesseraStackedBuildContext.IsActive ? media : TesseraChrome.Glass(media, 10, new Thickness(12, 14));
        }
    }
}
