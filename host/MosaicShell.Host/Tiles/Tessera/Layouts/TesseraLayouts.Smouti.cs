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

    public static Control Smouti(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 10);
        const double ringSize = TesseraStyleMetrics.SmoutiRing;
        var ring = new TesseraRingVolume
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
            AccentBrushOverride = TesseraStylePalette.Smouti.AccentBrush,
            PercentBrushOverride = TesseraStylePalette.Smouti.BrightBrush
        };
        ring.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        TesseraLiveAmbient.RegisterRing(ring);
        var ringHost = new Border
        {
            Width = ringSize,
            Height = ringSize,
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = ring
        };

        var left = new StackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                TesseraChrome.Label("Audio level", 13, FontWeight.Bold),
                TesseraChrome.Label("Speakers", 10, muted: true),
                ringHost
            }
        };
        BindWheel(ringHost, vm);

        Control body = left;
        if (vm.ShowMediaStrip)
        {
            var media = TesseraMediaPanel.Create(vm, TesseraMediaMode.SmoutiSide);
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

        var shell = TesseraChrome.WithArtWash(body, vm.ThumbnailPng, 10,
            new Thickness(TesseraStyleMetrics.SmoutiPad, 14),
            TesseraStyleMetrics.SmoutiWidth,
            TesseraStyleMetrics.SmoutiMaxHeight);
        shell.MinHeight = TesseraStyleMetrics.SmoutiMinHeight;
        shell.VerticalAlignment = VerticalAlignment.Top;
        return shell;

    }
}