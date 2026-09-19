using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons.Avalonia;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Tiles.Tessera
{
    internal static partial class TesseraLayouts
    {
        public static Control Fluent(TesseraFlyoutViewModel vm)
        {
            if (IsStatus(vm))
            {
                return StatusChip(vm, TesseraStatusFlyoutPolicy.ResolveChipCornerRadiusDip("Fluent"));
            }

            if (vm.Kind.Equals("media", StringComparison.OrdinalIgnoreCase)
                && !TesseraStackedBuildContext.IsActive)
            {
                // Must go through FluentMediaReveal, not TesseraChrome.Shell directly: phase 2
                // drives TesseraRevealHost instances found in the visual tree, so a media card
                // built without one is collected as zero hosts and the reveal is skipped outright
                // (measured: 26 ticks for the volume flyout, 0 for this one). The chrome below is
                // identical to FluentMediaWrap's - this branch had forked it and lost the wrap.
                return FluentMediaReveal(vm, TesseraMediaPanel.Create(vm, TesseraMediaMode.FluentSide));
            }

            Control? stacked = TesseraStackedBuildContext.TryCreatePanel(
                vm,
                buildVolume: FluentVolumeCore,
                buildMedia: v => TesseraMediaPanel.Create(v, TesseraMediaMode.FluentSide),
                wrapVolume: vol => FluentVolumeWrap(vm, vol),
                wrapMedia: media => FluentMediaReveal(vm, media));
            if (stacked is not null)
            {
                return stacked;
            }

            Control volCol = FluentVolumeCore(vm);
            Control body = volCol;
            if (vm.ShowMediaStrip)
            {
                Control media = TesseraMediaPanel.Create(vm, TesseraMediaMode.FluentSide);
                TesseraRevealHost reveal = TesseraRevealHostFactory.WrapMediaFromCatalog(vm, media);
                body = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 0,
                    Children = { AttachFluentDivider(vm, volCol), reveal }
                };
            }

            return TesseraChrome.Shell(body, 10, TesseraShellOptions.InsetMargin,
                new SolidColorBrush(TesseraPalette.Primary),
                maxWidth: TesseraFluentMetrics.MaxShellWidth);
        }

        private static Control FluentVolumeCore(TesseraFlyoutViewModel vm)
        {
            const double volumeW = TesseraFluentMetrics.VolumeWidth;
            const double h = TesseraFluentMetrics.Height;
            const double pad = TesseraFluentMetrics.Pad;

            Control glyph = TesseraVolumeGlyph.Create(vm, 20);
            glyph.Name = "TesseraGlyph";
            glyph.HorizontalAlignment = HorizontalAlignment.Center;
            glyph.Margin = new Thickness(0, pad, 0, 6);

            TesseraTrack track = new()
            {
                IsVertical = true,
                Width = 26,
                Height = h - (pad * 2) - 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Value = vm.PrimaryValue,
                Name = "TesseraTrack",
                TrackThickness = 5
            };
            track.ValueChanged += (_, v) => vm.ApplyPrimary(v);

            TextBlock percent = new()
            {
                Text = vm.PrimaryPercent,
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = TesseraPalette.FontBrush,
                FontFamily = new FontFamily("Segoe UI Variable, Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, pad),
                Name = "TesseraPercent"
            };
            TesseraLiveAmbient.RegisterVolume(track, percent, glyph as MaterialIcon);

            DockPanel volPanel = new() { LastChildFill = true };
            DockPanel.SetDock(glyph, Dock.Top);
            DockPanel.SetDock(percent, Dock.Bottom);
            volPanel.Children.Add(glyph);
            volPanel.Children.Add(percent);
            volPanel.Children.Add(track);

            Border volCol = new()
            {
                Width = volumeW,
                Height = h,
                Background = Brushes.Transparent,
                Child = volPanel
            };
            BindWheel(volCol, vm);
            return volCol;
        }

        private static Control FluentVolumeWrap(TesseraFlyoutViewModel vm, Control volCol)
        {
            Control inner = AttachFluentDivider(vm, volCol);
            TesseraShellOptions options = TesseraStackedBuildContext.IsActive ? TesseraShellOptions.None : TesseraShellOptions.InsetMargin;
            return TesseraChrome.Shell(inner, 10, options,
                new SolidColorBrush(TesseraPalette.Primary));
        }

        private static Control AttachFluentDivider(TesseraFlyoutViewModel vm, Control volCol)
        {
            return !vm.ShowMediaStrip || !TesseraFlyoutAnimatedTargetSpec.FluentDividerMustTweenInPlaceOnShow
                ? volCol
                : TesseraRevealHost.WrapFluentVolumeDivider(vm, volCol);
        }

        private static Control FluentMediaReveal(TesseraFlyoutViewModel vm, Control media)
        {
            return TesseraRevealHostFactory.WrapMediaFromCatalog(vm, FluentMediaWrap(media));
        }

        private static Control FluentMediaWrap(Control media)
        {
            TesseraShellOptions options = TesseraStackedBuildContext.IsActive ? TesseraShellOptions.None : TesseraShellOptions.InsetMargin;
            return TesseraChrome.Shell(media, 10, options,
                new SolidColorBrush(TesseraPalette.Primary),
                maxWidth: TesseraFluentMetrics.MaxShellWidth);
        }
    }
}
