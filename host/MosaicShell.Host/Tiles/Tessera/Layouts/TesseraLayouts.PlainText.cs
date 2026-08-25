using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using Material.Icons.Avalonia;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;

namespace MosaicShell.Host.Tiles.Tessera;

internal static partial class TesseraLayouts
{

    public static Control PlainText(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 4);

        var stacked = TesseraStackedBuildContext.TryCreatePanel(
            vm,
            buildVolume: PlainTextVolumeCore,
            buildMedia: PlainTextMediaCore,
            wrapVolume: PlainTextVolumeWrap,
            wrapMedia: PlainTextMediaWrap);
        if (stacked is not null)
            return stacked;

        var kids = PlainTextVolumeChildren(vm);
        if (vm.ShowMediaStrip)
        {
            var mediaKids = PlainTextMediaChildren(vm);
            var mediaPanel = new StackPanel { Spacing = 4 };
            foreach (var c in mediaKids)
                mediaPanel.Children.Add(c);
            kids.Add(TesseraRevealHostFactory.WrapMediaFromCatalog(vm, mediaPanel));
        }

        var panel = new StackPanel { Spacing = 4 };
        foreach (var c in kids) panel.Children.Add(c);
        return PlainTextShell(panel);
    }

    private static Control PlainTextVolumeCore(TesseraFlyoutViewModel vm)
    {
        var panel = new StackPanel { Spacing = 4 };
        foreach (var c in PlainTextVolumeChildren(vm))
            panel.Children.Add(c);
        BindWheel(panel, vm);
        return panel;
    }

    private static Control PlainTextMediaCore(TesseraFlyoutViewModel vm)
    {
        var panel = new StackPanel { Spacing = 4 };
        foreach (var c in PlainTextMediaChildren(vm))
            panel.Children.Add(c);
        return panel;
    }

    private static List<Control> PlainTextVolumeChildren(TesseraFlyoutViewModel vm)
    {
        var pct = VolumePercent.ToPercent(vm.PrimaryValue);
        var header = TesseraChrome.Mono($"Speakers: {pct}%", 14);
        header.Name = "TesseraPercent";
        var slash = TesseraChrome.Mono(TesseraChrome.SlashFill(vm.PrimaryValue), 14);
        TesseraLiveAmbient.RegisterSlash(slash);
        var track = new TesseraTrack
        {
            IsVertical = false,
            Width = 280,
            Height = 20,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack",
            Opacity = 0.01
        };
        track.ValueChanged += (_, v) =>
        {
            vm.ApplyPrimary(v);
            header.Text = $"Speakers: {VolumePercent.ToPercent(v)}%";
            slash.Text = TesseraChrome.SlashFill(v);
        };
        TesseraLiveAmbient.RegisterVolume(track, header, null);
        return
        [
            header,
            slash,
            track,
            TesseraChrome.Mono("------------------------------", 12, muted: true)
        ];
    }

    private static List<Control> PlainTextMediaChildren(TesseraFlyoutViewModel vm)
    {
        var state = vm.IsPlaying ? "Playing" : "Paused";
        var title = TesseraChrome.Mono($"{vm.MediaTitle} > {state} <", 13);
        var artist = TesseraChrome.Mono(vm.MediaArtist, 12, muted: true);
        var prog = TesseraChrome.Mono(
            $"{FormatTime(vm.MediaPositionSeconds)} {TesseraChrome.SlashFill(vm.MediaProgress, 16)} {FormatTime(vm.MediaDurationSeconds)}",
            12);
        TesseraLiveAmbient.RegisterPlainTextMedia(title, artist, prog);
        return
        [
            title,
            artist,
            prog,
            TesseraChrome.Mono("Media playing | Heart: 0 Shuffle: 0 Repeat: 0", 10, muted: true)
        ];
    }

    private static Control PlainTextVolumeWrap(Control panel) =>
        PlainTextShell(panel, TesseraStackedBuildContext.Role == TesseraStackedPanelRole.Volume);

    private static Control PlainTextMediaWrap(Control panel) =>
        PlainTextShell(panel, TesseraStackedBuildContext.Role == TesseraStackedPanelRole.Media);

    private static Control PlainTextShell(Control panel, bool stackedSegment = false)
    {
        var shell = new Grid
        {
            Width = TesseraStackedPlacementSpec.PlainTextWidthDip,
            MinHeight = stackedSegment ? 0 : 80
        };
        var bg = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(200, 0x11, 0x11, 0x1b)),
            BorderBrush = TesseraPalette.AccentBrush,
            BorderThickness = new Thickness(1),
            Child = new Border { Padding = new Thickness(16, 12), Child = panel }
        };
        shell.Children.Add(bg);
        var clipFigures = new PathFigures
        {
            new PathFigure
            {
                StartPoint = new Point(0, 0),
                IsClosed = true,
                Segments = new PathSegments
                {
                    new LineSegment { Point = new Point(340, 0) },
                    new LineSegment { Point = new Point(320, 200) },
                    new LineSegment { Point = new Point(0, 200) }
                }
            }
        };
        bg.Clip = new PathGeometry { Figures = clipFigures };
        return shell;
    }
}
