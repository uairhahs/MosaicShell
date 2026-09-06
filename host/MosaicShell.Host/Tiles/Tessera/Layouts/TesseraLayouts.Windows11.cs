using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons.Avalonia;

namespace MosaicShell.Host.Tiles.Tessera
{
    internal static partial class TesseraLayouts
    {

        public static Control Windows11(TesseraFlyoutViewModel vm)
        {
            if (IsStatus(vm))
            {
                return StatusChip(vm, TesseraWindows11Metrics.CornerRadius);
            }

            if (vm.Kind.Equals("media", StringComparison.OrdinalIgnoreCase)
                && !TesseraStackedBuildContext.IsActive)
            {
                return TesseraChrome.Glass(
                    TesseraMediaPanel.Create(vm, TesseraMediaMode.Windows11Below),
                    TesseraWindows11Metrics.CornerRadius,
                    w: TesseraWindows11Metrics.Width);
            }

            Control? stacked = TesseraStackedBuildContext.TryCreatePanel(
                vm,
                buildVolume: Win11VolumeCore,
                buildMedia: v => TesseraMediaPanel.Create(v, TesseraMediaMode.Windows11Below),
                wrapVolume: Win11VolumeWrap,
                wrapMedia: Win11MediaWrap);
            if (stacked is not null)
            {
                return stacked;
            }

            Control row = Win11VolumeCore(vm);
            Control body = row;
            if (vm.ShowMediaStrip)
            {
                Border divider = new()
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                    Margin = new Thickness(TesseraWindows11Metrics.Pad, 0)
                };
                Control media = TesseraMediaPanel.Create(vm, TesseraMediaMode.Windows11Below);
                body = TesseraRevealHost.WrapWin11Fancy(
                    vm,
                    row,
                    media,
                    divider,
                    TesseraWindows11Metrics.VolumeHeight,
                    TesseraWindows11Metrics.MediaHeight,
                    TesseraWindows11Metrics.Width,
                    TesseraWindows11Metrics.CornerRadius,
                    TesseraStylePalette.Windows11.ShellBrush);
                return body;
            }

            return TesseraChrome.GlassTinted(body, TesseraWindows11Metrics.CornerRadius, TesseraStylePalette.Windows11.ShellBrush, w: TesseraWindows11Metrics.Width);
        }

        private static Control Win11VolumeCore(TesseraFlyoutViewModel vm)
        {
            const double w = TesseraWindows11Metrics.Width;
            Control glyph = TesseraVolumeGlyph.Create(vm, 16);
            glyph.Name = "TesseraGlyph";
            glyph.VerticalAlignment = VerticalAlignment.Center;
            glyph.HorizontalAlignment = HorizontalAlignment.Center;

            TesseraTrack track = new()
            {
                IsVertical = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 26,
                Value = vm.PrimaryValue,
                Name = "TesseraTrack",
                TrackThickness = 4,
                AccentBrushOverride = TesseraStylePalette.Windows11.AccentBrush
            };
            track.ValueChanged += (_, v) => vm.ApplyPrimary(v);

            TextBlock percent = new()
            {
                Text = vm.PrimaryPercent,
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = TesseraPalette.FontBrush,
                FontFamily = new FontFamily("Segoe UI Variable, Segoe UI"),
                Name = "TesseraPercent"
            };
            TesseraLiveAmbient.RegisterVolume(track, percent, glyph as MaterialIcon);

            Grid row = new()
            {
                Width = w,
                Height = TesseraWindows11Metrics.VolumeHeight,
                ColumnDefinitions = new ColumnDefinitions("60,*,60")
            };
            Grid.SetColumn(glyph, 0);
            Grid.SetColumn(track, 1);
            Grid.SetColumn(percent, 2);
            row.Children.Add(glyph);
            row.Children.Add(track);
            row.Children.Add(percent);
            BindWheel(row, vm);
            return row;
        }

        private static Control Win11VolumeWrap(Control row)
        {
            Control body = row;
            if (TesseraStackedBuildContext.IsActive)
            {
                body = new StackPanel
                {
                    Spacing = 0,
                    Children =
                    {
                        row,
                        new Border
                        {
                            Height = 1,
                            Background = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                            Margin = new Thickness(TesseraWindows11Metrics.Pad, 0)
                        }
                    }
                };
            }

            return TesseraChrome.GlassTinted(body, TesseraWindows11Metrics.CornerRadius, TesseraStylePalette.Windows11.ShellBrush,
                w: TesseraWindows11Metrics.Width);
        }

        private static Control Win11MediaWrap(Control media)
        {
            return TesseraChrome.GlassTinted(media, TesseraWindows11Metrics.CornerRadius, TesseraStylePalette.Windows11.ShellBrush,
                w: TesseraWindows11Metrics.Width);
        }
    }
}
