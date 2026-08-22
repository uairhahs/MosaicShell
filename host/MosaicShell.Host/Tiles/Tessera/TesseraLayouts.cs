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

public static class TesseraStyleFactory
{
    public static Control Create(string styleId, TesseraFlyoutViewModel vm)
    {
        TesseraPalette.RefreshAccent();
        var host = new TesseraLiveHost();
        TesseraLiveAmbient.Current = host.Bindings;
        try
        {
            host.Content = styleId.ToLowerInvariant() switch
            {
                "win11" => TesseraLayouts.Win11(vm),
                "simple" => TesseraLayouts.Simple(vm),
                "pixel" => TesseraLayouts.Pixel(vm),
                "center" => TesseraLayouts.Center(vm),
                "modern" => TesseraLayouts.Modern(vm),
                "amber" => TesseraLayouts.Amber(vm),
                "gnome" => TesseraLayouts.Gnome(vm),
                "smouti" => TesseraLayouts.Smouti(vm),
                "plainext" => TesseraLayouts.Plainext(vm),
                "coreui" => TesseraLayouts.CoreUI(vm),
                _ => TesseraLayouts.Fluent(vm),
            };
        }
        finally
        {
            TesseraLiveAmbient.Current = null;
        }
        return host;
    }
}

/// <summary>YourFlyouts-style layouts - fidelity target ~8 vs .local/Tessera refs.</summary>
internal static class TesseraLayouts
{
    public static Control Fluent(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 0);

        if (vm.Kind.Equals("media", StringComparison.OrdinalIgnoreCase))
        {
            return TesseraShell.Create(
                TesseraMediaPanel.Create(vm, TesseraMediaMode.FluentSide),
                cornerRadius: 10,
                fill: TesseraPalette.Primary,
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

        return TesseraShell.Create(body, cornerRadius: 10, fill: TesseraPalette.Primary,
            maxWidth: TesseraFluentMetrics.MaxShellWidth);
    }

    public static Control Win11(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, TesseraWin11Metrics.CornerRadius, TesseraWin11Metrics.Width, TesseraWin11Metrics.VolumeHeight);

        if (vm.Kind.Equals("media", StringComparison.OrdinalIgnoreCase))
            return TesseraChrome.Glass(
                TesseraMediaPanel.Create(vm, TesseraMediaMode.Win11Below),
                TesseraWin11Metrics.CornerRadius,
                w: TesseraWin11Metrics.Width);

        const double w = TesseraWin11Metrics.Width;
        var glyph = TesseraVolumeGlyph.Create(vm, 18);
        glyph.Name = "TesseraGlyph";
        glyph.VerticalAlignment = VerticalAlignment.Center;
        glyph.HorizontalAlignment = HorizontalAlignment.Center;

        var track = new TesseraTrack
        {
            IsVertical = false,
            Width = w - 112,
            Height = 26,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack",
            TrackThickness = 5
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);

        var percent = new TextBlock
        {
            Text = vm.PrimaryPercent,
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Width = 42,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = TesseraPalette.FontBrush,
            FontFamily = new FontFamily("Segoe UI Variable, Segoe UI"),
            Name = "TesseraPercent"
        };
        TesseraLiveAmbient.RegisterVolume(track, percent, glyph as MaterialIcon);

        var row = new Grid
        {
            Width = w,
            Height = TesseraWin11Metrics.VolumeHeight,
            ColumnDefinitions = new ColumnDefinitions("44,*,46"),
            Margin = new Thickness(TesseraWin11Metrics.Pad / 2, 4, TesseraWin11Metrics.Pad / 2, 0)
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
                Spacing = 2,
                Children =
                {
                    row,
                    new Border { Height = 1, Background = TesseraPalette.StrokeBrush, Margin = new Thickness(14, 2), Opacity = 0.5 },
                    TesseraMediaPanel.Create(vm, TesseraMediaMode.Win11Below)
                }
            };
        }

        return TesseraChrome.Glass(body, TesseraWin11Metrics.CornerRadius, w: w);
    }

    public static Control Center(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, TesseraCenterMetrics.CornerRadius, TesseraCenterMetrics.Size, TesseraCenterMetrics.Size);
        var glyph = TesseraVolumeGlyph.Create(vm, TesseraCenterMetrics.GlyphSize);
        glyph.Name = "TesseraGlyph";
        glyph.HorizontalAlignment = HorizontalAlignment.Center;
        var percent = new TextBlock
        {
            Text = vm.PrimaryPercent,
            FontSize = TesseraCenterMetrics.PercentSize,
            FontWeight = FontWeight.SemiBold,
            Foreground = TesseraPalette.FontBrush,
            FontFamily = new FontFamily("Segoe UI Variable, Segoe UI"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Name = "TesseraPercent"
        };
        var track = new TesseraTrack
        {
            IsVertical = true,
            Width = 1,
            Height = 1,
            Opacity = 0,
            IsHitTestVisible = false,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack"
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        TesseraLiveAmbient.RegisterVolume(track, percent, glyph as MaterialIcon);
        var card = TesseraChrome.Glass(new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 8,
            Children = { glyph, percent, track }
        }, TesseraCenterMetrics.CornerRadius, new Thickness(18), TesseraCenterMetrics.Size, TesseraCenterMetrics.Size);
        BindWheel(card, vm);
        return card;
    }

    public static Control Gnome(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 24);
        var volTrack = new TesseraTrack
        {
            IsVertical = false,
            Width = 160,
            Height = 24,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack",
            TrackThickness = 5,
            ShowThumb = false
        };
        volTrack.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        var glyph = TesseraVolumeGlyph.Create(vm, 18);
        glyph.Name = "TesseraGlyph";
        TesseraLiveAmbient.RegisterVolume(volTrack, null, glyph as MaterialIcon);
        var volPill = TesseraChrome.Glass(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { glyph, volTrack }
        }, 28, new Thickness(14, 10), w: 240);
        BindWheel(volPill, vm);

        if (!vm.ShowMediaStrip)
            return volPill;

        return new StackPanel
        {
            Spacing = 10,
            Children =
            {
                TesseraMediaPanel.Create(vm, TesseraMediaMode.GnomePill),
                volPill
            }
        };
    }

    public static Control Simple(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 12);
        var glyph = TesseraVolumeGlyph.Create(vm, 16);
        glyph.Name = "TesseraGlyph";
        var track = new TesseraTrack
        {
            IsVertical = false,
            Width = 180,
            Height = 26,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack",
            TrackThickness = 3
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        var percent = new TextBlock
        {
            Text = vm.PrimaryPercent,
            FontSize = 14,
            Width = 36,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = TesseraPalette.FontBrush,
            Name = "TesseraPercent"
        };
        TesseraLiveAmbient.RegisterVolume(track, percent, glyph as MaterialIcon);
        var vol = TesseraChrome.Glass(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children = { glyph, track, percent }
        }, 12, new Thickness(12, 10), w: 280);
        BindWheel(vol, vm);
        if (!vm.ShowMediaStrip) return vol;
        return new StackPanel
        {
            Spacing = 10,
            Children = { vol, TesseraMediaPanel.Create(vm, TesseraMediaMode.SimpleRow) }
        };
    }

    public static Control Modern(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 12);
        var glyph = TesseraVolumeGlyph.Create(vm, 16);
        glyph.Name = "TesseraGlyph";
        var track = new TesseraTrack
        {
            IsVertical = false,
            Width = 220,
            Height = 28,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack",
            TrackThickness = 4
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        var percent = new TextBlock
        {
            Text = vm.PrimaryPercent,
            FontSize = 14,
            Width = 40,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = TesseraPalette.FontBrush,
            Name = "TesseraPercent"
        };
        TesseraLiveAmbient.RegisterVolume(track, percent, glyph as MaterialIcon);
        var vol = TesseraChrome.Glass(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children = { glyph, track, percent }
        }, 12, new Thickness(14, 10), w: 320);
        BindWheel(vol, vm);
        if (!vm.ShowMediaStrip) return vol;
        return new StackPanel
        {
            Spacing = 12,
            Children = { vol, TesseraMediaPanel.Create(vm, TesseraMediaMode.ModernCard) }
        };
    }

    public static Control CoreUI(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 8);
        const double gap = 6;

        var device = new Border
        {
            Width = 44,
            Height = 44,
            CornerRadius = new CornerRadius(8),
            Background = TesseraChrome.TileFaceHi,
            BorderBrush = TesseraChrome.SoftStroke,
            BorderThickness = new Thickness(1),
            Child = new MaterialIcon
            {
                Kind = MaterialIconKind.Headphones,
                Width = 18,
                Height = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = TesseraPalette.FontBrush
            }
        };

        var glyph = TesseraVolumeGlyph.Create(vm, 12);
        glyph.Name = "TesseraGlyph";
        var track = new TesseraTrack
        {
            IsVertical = false,
            Width = 140,
            Height = 20,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack",
            TrackThickness = 8,
            ShowThumb = false
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        var percent = new TextBlock
        {
            Text = vm.PrimaryPercent + "%",
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = TesseraPalette.FontBrush,
            Name = "TesseraPercent"
        };
        TesseraLiveAmbient.RegisterVolume(track, percent, glyph as MaterialIcon);
        var volBar = new Border
        {
            Height = 44,
            CornerRadius = new CornerRadius(8),
            Background = TesseraChrome.TileFaceHi,
            BorderBrush = TesseraChrome.SoftStroke,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 0),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { glyph, percent, track }
            }
        };
        BindWheel(volBar, vm);

        var playIcon = new MaterialIcon
        {
            Kind = vm.IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play,
            Width = 12,
            Height = 12,
            Foreground = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var playBtn = new Border
        {
            Width = 26,
            Height = 26,
            CornerRadius = new CornerRadius(13),
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = playIcon
        };
        playBtn.PointerPressed += (_, e) => { _ = vm.PlayPauseAsync(); e.Handled = true; };

        var media = TesseraMediaPanel.Create(vm, TesseraMediaMode.CoreUiBlock);
        var transport = new Border
        {
            Width = 40,
            Height = 72,
            CornerRadius = new CornerRadius(8),
            Background = TesseraChrome.TileFaceHi,
            BorderBrush = TesseraChrome.SoftStroke,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4, 6),
            Child = new StackPanel
            {
                Spacing = 6,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    IconBtn(MaterialIconKind.SkipPrevious, () => _ = vm.PreviousAsync(), 24),
                    playBtn,
                    IconBtn(MaterialIconKind.SkipNext, () => _ = vm.NextAsync(), 24)
                }
            }
        };

        var top = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions($"44,{gap},*"),
            Height = 44
        };
        Grid.SetColumn(device, 0);
        Grid.SetColumn(volBar, 2);
        top.Children.Add(device);
        top.Children.Add(volBar);

        Control body = top;
        if (vm.ShowMediaStrip)
        {
            var bottom = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions($"*,{gap},40"),
                Height = 72
            };
            Grid.SetColumn(media, 0);
            Grid.SetColumn(transport, 2);
            bottom.Children.Add(media);
            bottom.Children.Add(transport);
            body = new StackPanel { Spacing = gap, Children = { top, bottom } };
        }

        return TesseraChrome.Glass(body, 10, new Thickness(gap), 300);
    }

    public static Control Amber(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 16);
        var track = new TesseraTrack
        {
            IsVertical = true,
            Width = 28,
            Height = 200,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack",
            FatThumb = false,
            ShowThumb = false,
            TrackThickness = 28,
            TrackPad = 0 // fill edge-to-edge - no dead space at ends
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        TesseraLiveAmbient.RegisterVolume(track, null, null);
        // Edge-to-edge fill: no stroked Glass (border thickness would inset the track)
        var volPill = new Border
        {
            Width = 28,
            Height = 200,
            CornerRadius = new CornerRadius(14),
            ClipToBounds = true,
            Background = TesseraPalette.TrackBackBrush,
            Child = track
        };
        BindWheel(volPill, vm);

        if (!vm.ShowMediaStrip)
            return volPill;

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children =
            {
                TesseraMediaPanel.Create(vm, TesseraMediaMode.AmberCard),
                volPill
            }
        };
    }

    public static Control Pixel(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 24);
        var track = new TesseraTrack
        {
            IsVertical = true,
            Width = 44,
            Height = 200,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack",
            FatThumb = true
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        var percent = new TextBlock
        {
            Text = vm.PrimaryPercent,
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = TesseraPalette.FontBrush,
            Name = "TesseraPercent",
            Margin = new Thickness(0, 6, 0, 0)
        };
        var head = new Border
        {
            Width = 28,
            Height = 28,
            CornerRadius = new CornerRadius(14),
            Background = new SolidColorBrush(Color.FromRgb(240, 240, 245)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 4),
            Child = new MaterialIcon
            {
                Kind = MaterialIconKind.Headphones,
                Width = 16,
                Height = 16,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        TesseraLiveAmbient.RegisterVolume(track, percent, null);
        var volPill = TesseraChrome.Glass(new StackPanel
        {
            Children = { head, track, percent }
        }, 28, new Thickness(6, 4), w: 56, h: 280);
        BindWheel(volPill, vm);

        var transport = TesseraChrome.Glass(new StackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                IconBtn(MaterialIconKind.SkipPrevious, () => _ = vm.PreviousAsync()),
                PlayPill(vm),
                IconBtn(MaterialIconKind.SkipNext, () => _ = vm.NextAsync())
            }
        }, 28, new Thickness(8), w: 52, h: 160);

        var extras = TesseraChrome.Glass(new StackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                IconBtn(MaterialIconKind.Shuffle, () => { }),
                IconBtn(MaterialIconKind.HeartOutline, () => { }),
                IconBtn(MaterialIconKind.Repeat, () => { })
            }
        }, 28, new Thickness(8), w: 52, h: 130);

        var eq = new Border
        {
            Width = 44,
            Height = 44,
            CornerRadius = new CornerRadius(22),
            Background = TesseraChrome.DarkSolid,
            BorderBrush = TesseraChrome.SoftStroke,
            BorderThickness = new Thickness(1),
            Child = new MaterialIcon
            {
                Kind = MaterialIconKind.TuneVertical,
                Width = 20,
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = TesseraPalette.FontBrush
            }
        };
        eq.PointerPressed += (_, e) =>
        {
            _ = TesseraHostBridge.ArmMixdeckAsync?.Invoke();
            e.Handled = true;
        };

        var right = new StackPanel { Spacing = 10, Children = { volPill, eq } };
        var left = new StackPanel { Spacing = 10, Children = { transport, extras } };
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children = { left, right }
        };
    }

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

    public static Control Smouti(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 10);
        var ring = new TesseraRingVolume { Value = vm.PrimaryValue, Width = 64, Height = 64 };
        ring.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        TesseraLiveAmbient.RegisterRing(ring);

        var left = new StackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                TesseraChrome.Label("Audio level", 12, FontWeight.Bold),
                TesseraChrome.Label("Speakers", 9, muted: true),
                ring
            }
        };
        BindWheel(ring, vm);

        Control body = left;
        if (vm.ShowMediaStrip)
        {
            body = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { left, TesseraMediaPanel.Create(vm, TesseraMediaMode.SmoutiSide) }
            };
        }

        var shell = TesseraChrome.WithArtWash(body, vm.ThumbnailPng, 10, new Thickness(10, 8), 360);
        shell.MaxHeight = 118;
        shell.VerticalAlignment = VerticalAlignment.Top;
        return shell;
    }

    // --- helpers ---

    private static Control StatusChip(TesseraFlyoutViewModel vm, double radius, double? w = null, double? h = null) =>
        TesseraChrome.Glass(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(16, 0),
            Children =
            {
                TesseraVolumeGlyph.Create(vm, 16),
                TesseraChrome.Label(vm.KindLabel, 14)
            }
        }, radius, new Thickness(12, 10), w, h ?? 50);

    private static Control IconBtn(MaterialIconKind kind, Action act, double size = 36)
    {
        var b = new Border
        {
            Width = size,
            Height = size,
            Background = Brushes.Transparent,
            Child = new MaterialIcon
            {
                Kind = kind,
                Width = size * 0.55,
                Height = size * 0.55,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = TesseraPalette.FontBrush
            }
        };
        b.PointerPressed += (_, e) => { act(); e.Handled = true; };
        return b;
    }

    private static Control PlayPill(TesseraFlyoutViewModel vm)
    {
        var icon = new MaterialIcon
        {
            Kind = vm.IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play,
            Width = 18,
            Height = 18,
            Foreground = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var pill = new Border
        {
            Width = 36,
            Height = 44,
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Color.FromRgb(235, 235, 240)),
            Child = icon
        };
        pill.PointerPressed += (_, e) => { _ = vm.PlayPauseAsync(); e.Handled = true; };
        return pill;
    }

    private static void BindWheel(Control c, TesseraFlyoutViewModel vm, double step = 0.02) =>
        c.PointerWheelChanged += (_, e) =>
        {
            vm.Nudge(e.Delta.Y > 0 ? 0.02 : -0.02);
            e.Handled = true;
        };

    private static bool IsStatus(TesseraFlyoutViewModel vm) =>
        vm.Kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
        || vm.Kind.Equals("flight", StringComparison.OrdinalIgnoreCase);

    private static string FormatTime(double seconds)
    {
        if (seconds <= 0 || double.IsNaN(seconds)) return "0:00";
        var t = TimeSpan.FromSeconds(seconds);
        return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
    }
}
