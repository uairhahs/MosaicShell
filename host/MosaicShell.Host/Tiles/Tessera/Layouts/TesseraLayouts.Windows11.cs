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

    public static Control Windows11(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, TesseraWindows11Metrics.CornerRadius, TesseraWindows11Metrics.Width, TesseraWindows11Metrics.VolumeHeight);

        if (vm.Kind.Equals("media", StringComparison.OrdinalIgnoreCase))
            return TesseraChrome.Glass(
                TesseraMediaPanel.Create(vm, TesseraMediaMode.Windows11Below),
                TesseraWindows11Metrics.CornerRadius,
                w: TesseraWindows11Metrics.Width);

        const double w = TesseraWindows11Metrics.Width;
        var glyph = TesseraVolumeGlyph.Create(vm, 16);
        glyph.Name = "TesseraGlyph";
        glyph.VerticalAlignment = VerticalAlignment.Center;
        glyph.HorizontalAlignment = HorizontalAlignment.Center;

        var track = new TesseraTrack
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

        var percent = new TextBlock
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

        // Win11.inc: icon center @30, slider 60→(W-60), percent center @(W-30)
        var row = new Grid
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

        Control body = row;
        if (vm.ShowMediaStrip)
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
                    },
                    TesseraMediaPanel.Create(vm, TesseraMediaMode.Windows11Below)
                }
            };
        }

        return TesseraChrome.GlassTinted(body, TesseraWindows11Metrics.CornerRadius, TesseraStylePalette.Windows11.ShellBrush, w: w);

    }
}