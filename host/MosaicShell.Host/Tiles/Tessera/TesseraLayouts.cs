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
    public static Control Create(string styleId, TesseraFlyoutViewModel vm) =>
        Create(styleId, vm, accentColor: null);

    public static Control Create(string styleId, TesseraFlyoutViewModel vm, string? accentColor)
    {
        TesseraPalette.ApplyAccentFromSettings(accentColor);
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
            AccentBrushOverride = TesseraStylePalette.Win11.AccentBrush
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
            Height = TesseraWin11Metrics.VolumeHeight,
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
                        Margin = new Thickness(TesseraWin11Metrics.Pad, 0)
                    },
                    TesseraMediaPanel.Create(vm, TesseraMediaMode.Win11Below)
                }
            };
        }

        return TesseraChrome.GlassTinted(body, TesseraWin11Metrics.CornerRadius, TesseraStylePalette.Win11.ShellBrush, w: w);
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
        const double w = TesseraStyleMetrics.CoreUiWidth;
        const double gap = TesseraStyleMetrics.CoreUiGap;
        const double mediaH = TesseraStyleMetrics.CoreUiMediaH;

        var device = new Border
        {
            Width = TesseraStyleMetrics.CoreUiDevice,
            Height = TesseraStyleMetrics.CoreUiDevice,
            CornerRadius = new CornerRadius(8),
            Background = TesseraStylePalette.CoreUi.TileBrush,
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
        TesseraChrome.ApplyHoverHighlight(device, TesseraStylePalette.CoreUi.TileBrush,
            TesseraStylePalette.CoreUi.TileHoverBrush);

        var glyph = TesseraVolumeGlyph.Create(vm, 12);
        glyph.Name = "TesseraGlyph";
        var track = new TesseraTrack
        {
            IsVertical = false,
            Height = 20,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack",
            TrackThickness = 8,
            ShowThumb = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            AccentBrushOverride = TesseraStylePalette.CoreUi.AccentBrush
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        var percent = new TextBlock
        {
            Text = vm.PrimaryPercent,
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 36,
            Foreground = TesseraPalette.FontBrush,
            Name = "TesseraPercent"
        };
        TesseraLiveAmbient.RegisterVolume(track, percent, glyph as MaterialIcon);
        var volInner = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(glyph, 0);
        Grid.SetColumn(percent, 1);
        Grid.SetColumn(track, 2);
        glyph.Margin = new Thickness(0, 0, 8, 0);
        percent.Margin = new Thickness(0, 0, 8, 0);
        volInner.Children.Add(glyph);
        volInner.Children.Add(percent);
        volInner.Children.Add(track);
        var volBar = new Border
        {
            Height = TesseraStyleMetrics.CoreUiVolumeH,
            CornerRadius = new CornerRadius(8),
            Background = TesseraStylePalette.CoreUi.TileBrush,
            BorderBrush = TesseraChrome.SoftStroke,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 0),
            Child = volInner
        };
        TesseraChrome.ApplyHoverHighlight(volBar, TesseraStylePalette.CoreUi.TileBrush,
            TesseraStylePalette.CoreUi.TileHoverBrush);
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
            CornerRadius = new CornerRadius(8),
            ClipToBounds = true,
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = playIcon
        };
        TesseraChrome.ApplyHoverHighlight(playBtn, Brushes.White,
            new SolidColorBrush(Color.FromRgb(245, 245, 250)));
        playBtn.PointerPressed += (_, e) => { _ = vm.PlayPauseAsync(); e.Handled = true; };

        const double transportBtn = 22;
        var media = TesseraMediaPanel.Create(vm, TesseraMediaMode.CoreUiBlock, mediaH);
        var transport = new Border
        {
            Width = TesseraStyleMetrics.CoreUiTransportW,
            Height = mediaH,
            CornerRadius = new CornerRadius(8),
            Background = TesseraStylePalette.CoreUi.TileBrush,
            BorderBrush = TesseraChrome.SoftStroke,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4, 4),
            ClipToBounds = false,
            Child = new StackPanel
            {
                Spacing = 4,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    CoreUiIconBtn(MaterialIconKind.SkipPrevious, () => _ = vm.PreviousAsync(), transportBtn),
                    playBtn,
                    CoreUiIconBtn(MaterialIconKind.SkipNext, () => _ = vm.NextAsync(), transportBtn)
                }
            }
        };
        TesseraChrome.ApplyHoverHighlight(transport, TesseraStylePalette.CoreUi.TileBrush,
            TesseraStylePalette.CoreUi.TileHoverBrush);
        playBtn.Width = 24;
        playBtn.Height = 24;

        var top = new Grid
        {
            Width = w - gap * 2,
            ColumnDefinitions = new ColumnDefinitions($"{TesseraStyleMetrics.CoreUiDevice},{gap},*"),
            Height = TesseraStyleMetrics.CoreUiVolumeH
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
                Width = w - gap * 2,
                ColumnDefinitions = new ColumnDefinitions($"*,{gap},{TesseraStyleMetrics.CoreUiTransportW}"),
                Height = mediaH
            };
            Grid.SetColumn(media, 0);
            Grid.SetColumn(transport, 2);
            bottom.Children.Add(media);
            bottom.Children.Add(transport);
            body = new StackPanel { Spacing = gap, Width = w - gap * 2, Children = { top, bottom } };
        }

        return TesseraChrome.GlassTinted(body, 8, TesseraStylePalette.CoreUi.ShellBrush,
            new Thickness(gap), w);
    }

    public static Control Amber(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return StatusChip(vm, 16);
        var track = new TesseraTrack
        {
            IsVertical = true,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack",
            FatThumb = false,
            ShowThumb = true,
            TrackThickness = 28,
            TrackPad = 0,
            ShellRadius = 14,
            GlassFill = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        TesseraLiveAmbient.RegisterVolume(track, null, null);
        var volPill = TesseraChrome.Glass(track, 14, w: 28, h: 200);
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
        const double colW = TesseraStyleMetrics.PixelColumnW;
        const double gap = TesseraStyleMetrics.PixelGap;
        const double colH = TesseraStyleMetrics.PixelColH;
        const double volH = 2 * colH + gap - colW;
        const double pillR = colW / 2.0;
        const double hit = TesseraStyleMetrics.PixelHitTarget;
        const double pillPadH = (colW - hit) / 2.0;

        var shuffleIcon = PixelIcon(MaterialIconKind.Shuffle, muted: true);
        var heartIcon = PixelIcon(MaterialIconKind.HeartOutline, muted: true);
        var repeatIcon = PixelIcon(MaterialIconKind.Repeat, muted: true);

        var transport = TesseraChrome.SolidPill(new StackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                PixelIconBtn(MaterialIconKind.SkipPrevious, () => _ = vm.PreviousAsync()),
                PixelPlayPill(vm),
                PixelIconBtn(MaterialIconKind.SkipNext, () => _ = vm.NextAsync())
            }
        }, TesseraStylePalette.Pixel.ShellBrush, pillR, colW, colH, new Thickness(pillPadH, 12));

        var extras = TesseraChrome.SolidPill(new StackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                PixelIconBtn(shuffleIcon, () => _ = PixelToggleShuffleAsync(vm, shuffleIcon)),
                PixelIconBtn(heartIcon, () => _ = PixelToggleLikeAsync(vm, heartIcon)),
                PixelIconBtn(repeatIcon, () => _ = PixelToggleRepeatAsync(vm, repeatIcon))
            }
        }, TesseraStylePalette.Pixel.ShellBrush, pillR, colW, colH, new Thickness(pillPadH, 12));

        var track = new TesseraTrack
        {
            IsVertical = true,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack",
            ExpressiveVertical = true,
            ShowThumb = true,
            TrackPad = 0,
            ShellRadius = pillR,
            ShellEndRadius = 0,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            AccentBrushOverride = TesseraStylePalette.Pixel.AccentBrush,
            TrackBackBrushOverride = TesseraStylePalette.Pixel.TrackInactiveBrush
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);

        var percent = new TextBlock
        {
            Text = vm.PrimaryPercent,
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Foreground = TesseraStylePalette.Pixel.AccentBrush,
            Name = "TesseraPercent",
            Margin = new Thickness(0, 0, 0, 10),
            IsVisible = false
        };

        MaterialIcon? glyphIcon = null;
        var glyphControl = TesseraVolumeGlyph.Create(vm, TesseraStyleMetrics.PixelTrackIconSize);
        glyphControl.Name = "TesseraGlyph";
        if (glyphControl is MaterialIcon gi)
        {
            glyphIcon = gi;
            gi.HorizontalAlignment = HorizontalAlignment.Center;
            gi.VerticalAlignment = VerticalAlignment.Bottom;
            gi.Margin = new Thickness(0, 0, 0, TesseraStyleMetrics.PixelTrackIconBottom);
            TesseraPixelM3.ApplyVolumeGlyphTone(gi, vm.PrimaryValue, vm.IsMuted);
        }

        TesseraLiveAmbient.RegisterVolume(track, percent, glyphIcon, pixelVolumeGlyph: true, percentOnAdjustOnly: true);

        var trackHost = new Grid { VerticalAlignment = VerticalAlignment.Stretch, ClipToBounds = true };
        trackHost.Children.Add(track);
        if (glyphIcon is not null)
            trackHost.Children.Add(glyphIcon);

        var volBody = new Grid { VerticalAlignment = VerticalAlignment.Stretch };
        volBody.Children.Add(trackHost);
        volBody.Children.Add(percent);

        var volPill = TesseraChrome.SolidPill(volBody, TesseraStylePalette.Pixel.ShellBrush, pillR, colW, volH,
            new Thickness(0));

        var eq = PixelIconTile(MaterialIconKind.TuneVertical, TesseraStyleMetrics.PixelIconSize, colW);
        eq.PointerPressed += (_, e) =>
        {
            if (vm.HostUi is not null)
                _ = vm.HostUi.OpenOverlayAsync("mixdeck");
            e.Handled = true;
        };

        var left = new StackPanel { Spacing = gap, Children = { transport, extras } };
        var right = new StackPanel { Spacing = gap, Children = { volPill, eq } };
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = gap,
            Children = { left, right }
        };
        BindWheel(volPill, vm);
        return row;
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

    // --- helpers ---

    private static Control StatusChip(TesseraFlyoutViewModel vm, double radius, double? w = null, double? h = null)
    {
        var label = TesseraChrome.Label(vm.KindLabel, 14);
        label.Name = "TesseraStatusLabel";
        return TesseraChrome.Glass(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(16, 0),
            Children =
            {
                TesseraVolumeGlyph.Create(vm, 16),
                label
            }
        }, radius, new Thickness(12, 10), w, h ?? 50);
    }

    private static Control CoreUiIconBtn(MaterialIconKind kind, Action act, double size)
    {
        var icon = new MaterialIcon
        {
            Kind = kind,
            Width = size * 0.55,
            Height = size * 0.55,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = TesseraPalette.FontBrush
        };
        return TesseraChrome.IconButton(icon, act, size, circularHighlight: false,
            hover: TesseraStylePalette.CoreUi.IconHoverBrush);
    }

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

    private static MaterialIcon PixelIcon(MaterialIconKind kind, bool muted = false) =>
        new()
        {
            Kind = kind,
            Width = TesseraStyleMetrics.PixelIconSize,
            Height = TesseraStyleMetrics.PixelIconSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = muted
                ? TesseraStylePalette.Pixel.SecondaryBrush
                : TesseraStylePalette.Pixel.AccentBrush
        };

    private static Border PixelIconTile(MaterialIconKind kind, double iconSize, double tile)
    {
        var tileBorder = new Border
        {
            Width = tile,
            Height = tile,
            CornerRadius = new CornerRadius(tile / 2),
            Background = TesseraStylePalette.Pixel.ShellBrush,
            Child = new MaterialIcon
            {
                Kind = kind,
                Width = iconSize,
                Height = iconSize,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = TesseraStylePalette.Pixel.AccentBrush
            }
        };
        TesseraChrome.ApplyHoverHighlight(tileBorder, TesseraStylePalette.Pixel.ShellBrush,
            new SolidColorBrush(Color.FromRgb(40, 40, 44)));
        return tileBorder;
    }

    private static Control PixelIconBtn(MaterialIconKind kind, Action act) =>
        TesseraChrome.IconButton(PixelIcon(kind), act, TesseraStyleMetrics.PixelHitTarget, circularHighlight: true);

    private static Control PixelIconBtn(MaterialIcon icon, Action act) =>
        TesseraChrome.IconButton(icon, act, TesseraStyleMetrics.PixelHitTarget, circularHighlight: true);

    private static Control PixelPlayPill(TesseraFlyoutViewModel vm)
    {
        var icon = new MaterialIcon
        {
            Kind = vm.IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play,
            Width = TesseraStyleMetrics.PixelIconSize,
            Height = TesseraStyleMetrics.PixelIconSize,
            Foreground = TesseraStylePalette.Pixel.OnAccentBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (TesseraLiveAmbient.Current is { } live)
            live.PlayPauseIcon = icon;
        var normal = TesseraStylePalette.Pixel.AccentBrush;
        var hover = new SolidColorBrush(Color.FromRgb(240, 244, 255));
        var pill = new Border
        {
            Width = TesseraStyleMetrics.PixelPlayW,
            Height = TesseraStyleMetrics.PixelPlayH,
            CornerRadius = new CornerRadius(12),
            Background = normal,
            ClipToBounds = true,
            Child = icon
        };
        TesseraChrome.ApplyHoverHighlight(pill, normal, hover);
        pill.PointerPressed += (_, e) => { _ = vm.PlayPauseAsync(); e.Handled = true; };
        return pill;
    }

    private static async Task PixelToggleShuffleAsync(TesseraFlyoutViewModel vm, MaterialIcon icon)
    {
        await vm.Services.Media.ToggleShuffleAsync();
        var on = icon.Kind != MaterialIconKind.ShuffleVariant;
        icon.Kind = on ? MaterialIconKind.ShuffleVariant : MaterialIconKind.Shuffle;
        icon.Foreground = on
            ? TesseraStylePalette.Pixel.AccentBrush
            : TesseraStylePalette.Pixel.SecondaryBrush;
    }

    private static async Task PixelToggleRepeatAsync(TesseraFlyoutViewModel vm, MaterialIcon icon)
    {
        await vm.Services.Media.ToggleRepeatAsync();
        if (icon.Kind == MaterialIconKind.RepeatOne)
        {
            icon.Kind = MaterialIconKind.Repeat;
            icon.Foreground = TesseraStylePalette.Pixel.SecondaryBrush;
        }
        else if (icon.Kind == MaterialIconKind.Repeat)
        {
            icon.Kind = MaterialIconKind.RepeatOne;
            icon.Foreground = TesseraStylePalette.Pixel.AccentBrush;
        }
        else
        {
            icon.Kind = MaterialIconKind.Repeat;
            icon.Foreground = TesseraStylePalette.Pixel.AccentBrush;
        }
    }

    private static async Task PixelToggleLikeAsync(TesseraFlyoutViewModel vm, MaterialIcon icon)
    {
        var wantLiked = icon.Kind != MaterialIconKind.Heart;
        await vm.Services.Media.ToggleLikeAsync(wantLiked);
        icon.Kind = wantLiked ? MaterialIconKind.Heart : MaterialIconKind.HeartOutline;
        icon.Foreground = wantLiked
            ? TesseraStylePalette.Pixel.AccentBrush
            : TesseraStylePalette.Pixel.SecondaryBrush;
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
