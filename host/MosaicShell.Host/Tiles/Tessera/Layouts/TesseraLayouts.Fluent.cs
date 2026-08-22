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
    public static Control Fluent(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 0);

        if (vm.Kind.Equals("media", StringComparison.OrdinalIgnoreCase))
        {
            return TesseraChrome.Shell(
                TesseraMediaPanel.Create(vm, TesseraMediaMode.FluentSide),
                10,
                TesseraShellOptions.InsetMargin,
                new SolidColorBrush(TesseraPalette.Primary),
                maxWidth: TesseraFluentMetrics.MaxShellWidth);
        }

        const double volumeW = TesseraFluentMetrics.VolumeWidth;
        const double h = TesseraFluentMetrics.Height;
        const double pad = TesseraFluentMetrics.Pad;

        var glyph = TesseraVolumeGlyph.Create(vm, 20);
        glyph.Name = "TesseraGlyph";
        glyph.HorizontalAlignment = HorizontalAlignment.Center;
        glyph.Margin = new Thickness(0, pad, 0, 6);

        var track = new TesseraTrack
        {
            IsVertical = true,
            Width = 26,
            Height = h - pad * 2 - 48,
            HorizontalAlignment = HorizontalAlignment.Center,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack",
            TrackThickness = 5
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);

        var percent = new TextBlock
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

        var volPanel = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(glyph, Avalonia.Controls.Dock.Top);
        DockPanel.SetDock(percent, Avalonia.Controls.Dock.Bottom);
        volPanel.Children.Add(glyph);
        volPanel.Children.Add(percent);
        volPanel.Children.Add(track);

        var volCol = new Border
        {
            Width = volumeW,
            Height = h,
            Background = Brushes.Transparent,
            Child = volPanel
        };
        BindWheel(volCol, vm);

        Control body = volCol;
        if (vm.ShowMediaStrip)
        {
            var divider = new Border
            {
                Width = 1,
                Height = h - pad * 2,
                Background = TesseraPalette.StrokeBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, pad, 0, pad),
                Opacity = 0.55
            };
            var media = TesseraMediaPanel.Create(vm, TesseraMediaMode.FluentSide);
            body = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                Children = { volCol, divider, media }
            };
        }

        return TesseraChrome.Shell(body, 10, TesseraShellOptions.InsetMargin,
            new SolidColorBrush(TesseraPalette.Primary),
            maxWidth: TesseraFluentMetrics.MaxShellWidth);

    }
}