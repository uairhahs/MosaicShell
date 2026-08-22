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

    public static Control Plainext(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 4);
        var pct = VolumePercent.ToPercent(vm.PrimaryValue);
        var header = TesseraChrome.Mono($"Speakers: {pct}%", 14);
        header.Name = "TesseraPercent";
        var slash = TesseraChrome.Mono(TesseraChrome.SlashFill(vm.PrimaryValue), 14);
        TesseraLiveAmbient.RegisterSlash(slash);
        // Hidden track for interaction
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

        var kids = new List<Control>
        {
            header,
            slash,
            track,
            TesseraChrome.Mono("------------------------------", 12, muted: true)
        };

        if (vm.ShowMediaStrip)
        {
            var state = vm.IsPlaying ? "Playing" : "Paused";
            kids.Add(TesseraChrome.Mono($"{vm.MediaTitle} > {state} <", 13));
            kids.Add(TesseraChrome.Mono(vm.MediaArtist, 12, muted: true));
            var prog = TesseraChrome.Mono(
                $"{FormatTime(vm.MediaPositionSeconds)} {TesseraChrome.SlashFill(vm.MediaProgress, 16)} {FormatTime(vm.MediaDurationSeconds)}",
                12);
            kids.Add(prog);
            kids.Add(TesseraChrome.Mono("Media playing | Heart: 0 Shuffle: 0 Repeat: 0", 10, muted: true));
        }

        var panel = new StackPanel { Spacing = 4 };
        foreach (var c in kids) panel.Children.Add(c);

        // Diagonal cut via clipped polygon overlay on the right
        var shell = new Grid { Width = 360, MinHeight = 80 };
        var bg = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(200, 0x11, 0x11, 0x1b)),
            BorderBrush = TesseraPalette.AccentBrush,
            BorderThickness = new Thickness(1),
            Child = new Border { Padding = new Thickness(16, 12), Child = panel }
        };
        // Skewed right edge
        var cut = new Polygon
        {
            Points = new Points
            {
                new Point(330, 0),
                new Point(360, 0),
                new Point(360, 200),
                new Point(300, 200)
            },
            Fill = Brushes.Transparent,
            IsHitTestVisible = false
        };
        shell.Children.Add(bg);
        shell.Children.Add(cut);
        // Clip to parallelogram-ish using Border with custom - approximate with opacity mask
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
        BindWheel(shell, vm);
        return shell;

    }
}