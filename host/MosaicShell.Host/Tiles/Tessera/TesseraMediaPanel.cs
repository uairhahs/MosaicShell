using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Material.Icons;
using Material.Icons.Avalonia;
using System.IO;

namespace MosaicShell.Host.Tiles.Tessera;

public enum TesseraMediaMode
{
    FluentSide,
    Win11Below,
    ModernCard,
    SimpleRow,
    GnomePill,
    AmberCard,
    CoreUiBlock,
    SmoutiSide
}

public static class TesseraMediaPanel
{
    public static Control Create(TesseraFlyoutViewModel vm, TesseraMediaMode mode, double coreUiHeight = 72) => mode switch
    {
        TesseraMediaMode.Win11Below => Win11(vm),
        TesseraMediaMode.ModernCard => ModernCard(vm),
        TesseraMediaMode.SimpleRow => SimpleRow(vm),
        TesseraMediaMode.GnomePill => GnomePill(vm),
        TesseraMediaMode.AmberCard => AmberCard(vm),
        TesseraMediaMode.CoreUiBlock => CoreUiBlock(vm, coreUiHeight),
        TesseraMediaMode.SmoutiSide => SmoutiSide(vm),
        _ => Fluent(vm),
    };

    private static Control Fluent(TesseraFlyoutViewModel vm)
    {
        const double mediaW = TesseraFluentMetrics.MediaWidth - 16;
        const double h = TesseraFluentMetrics.Height;
        var art = AlbumArt(vm, 56);
        art.Margin = new Thickness(14, 14, 0, 0);
        var title = Text(vm.MediaTitle, 15, FontWeight.SemiBold, 240);
        var artist = Text(vm.MediaArtist, 11, FontWeight.Normal, 240, muted: true);
        var titles = new StackPanel
        {
            Margin = new Thickness(10, 14, 14, 0),
            Spacing = 2,
            Children = { title, artist }
        };
        var playIcon = PlayIcon(vm, 16);
        var transport = TransportRow(vm, playIcon, shuffleRepeat: true, spacing: 12, btnSize: 28);
        transport.Margin = new Thickness(0, 6, 0, 0);
        var (scrubCol, scrub, pos, dur) = ScrubberStacked(vm, mediaW - 80);
        scrubCol.Margin = new Thickness(28, 4, 28, 10);
        TesseraLiveAmbient.RegisterMedia(art, title, artist, scrub, pos, dur, playIcon);

        return new Border
        {
            Name = "TesseraMediaRoot",
            Width = mediaW,
            Height = h,
            Background = Brushes.Transparent,
            Child = new StackPanel
            {
                Children =
                {
                    new StackPanel { Orientation = Orientation.Horizontal, Children = { art, titles } },
                    transport,
                    scrubCol
                }
            }
        };
    }

    private static Control Win11(TesseraFlyoutViewModel vm)
    {
        const double w = TesseraWin11Metrics.Width;
        const double pad = TesseraWin11Metrics.Pad;
        var art = AlbumArt(vm, 80);
        art.HorizontalAlignment = HorizontalAlignment.Right;
        art.VerticalAlignment = VerticalAlignment.Top;
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children =
            {
                new MaterialIcon
                {
                    Kind = MaterialIconKind.MusicNote,
                    Width = 12,
                    Height = 12,
                    Foreground = TesseraPalette.FontBrush,
                    VerticalAlignment = VerticalAlignment.Center
                },
                TesseraChrome.Label("Media playing", 10, muted: true)
            }
        };
        var title = Text(vm.MediaTitle, 12, FontWeight.SemiBold, w - 80 - pad * 3);
        var artist = Text(vm.MediaArtist, 11, FontWeight.Normal, w - 80 - pad * 3, muted: true);
        var textCol = new StackPanel { Spacing = 4, Margin = new Thickness(0, 4, 0, 0), Children = { header, title, artist } };
        var top = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,80"),
            Margin = new Thickness(pad, 8, pad, 0)
        };
        Grid.SetColumn(textCol, 0);
        Grid.SetColumn(art, 1);
        top.Children.Add(textCol);
        top.Children.Add(art);

        var playIcon = PlayIcon(vm, 16);
        var likeIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.HeartOutline,
            Width = 14,
            Height = 14,
            Foreground = TesseraPalette.FontBrush
        };
        var shuffleIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.Shuffle,
            Width = 14,
            Height = 14,
            Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255))
        };
        var (scrubCol, scrub, pos, dur) = ScrubberStacked(vm, w - pad * 2);
        scrub.AccentBrushOverride = TesseraStylePalette.Win11.AccentBrush;
        scrubCol.Margin = new Thickness(pad, 6, pad, 0);
        var transport = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 16,
            Margin = new Thickness(0, 8, 0, 10),
            Children =
            {
                GlyphBtn(likeIcon, () => _ = vm.ToggleLikeAsync(likeIcon), size: 28),
                GlyphBtn(MaterialIconKind.SkipPrevious, () => _ = vm.PreviousAsync(), size: 28),
                GlyphBtn(playIcon, () => _ = vm.PlayPauseAsync(), size: 28, playStyle: true),
                GlyphBtn(MaterialIconKind.SkipNext, () => _ = vm.NextAsync(), size: 28),
                GlyphBtn(shuffleIcon, () => _ = vm.ToggleShuffleAsync(shuffleIcon), size: 28)
            }
        };
        TesseraLiveAmbient.RegisterMedia(art, title, artist, scrub, pos, dur, playIcon);
        return new Border
        {
            Name = "TesseraMediaRoot",
            Width = w,
            Height = TesseraWin11Metrics.MediaHeight,
            Background = Brushes.Transparent,
            Child = new StackPanel { Children = { top, scrubCol, transport } }
        };
    }

    private static Control ModernCard(TesseraFlyoutViewModel vm)
    {
        // Modern.inc: dark MediaC shell + square cover top-right (80×80) — not full-card art wash.
        const double artSize = 80;
        var art = AlbumArt(vm, artSize);
        art.HorizontalAlignment = HorizontalAlignment.Right;
        art.VerticalAlignment = VerticalAlignment.Top;
        var header = TesseraChrome.Label("Media playing", 10, muted: true);
        header.Margin = new Thickness(0, 0, 0, 4);
        var title = Text(vm.MediaTitle, 15, FontWeight.Bold, 220);
        var artist = Text(vm.MediaArtist, 11, FontWeight.Normal, 220, muted: true);
        var playIcon = PlayIcon(vm, 16);
        var likeIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.HeartOutline,
            Width = 14,
            Height = 14,
            Foreground = TesseraPalette.FontBrush
        };
        var shuffleIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.Shuffle,
            Width = 14,
            Height = 14,
            Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255))
        };
        var transport = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 12,
            Margin = new Thickness(0, 6, 0, 0),
            Children =
            {
                GlyphBtn(likeIcon, () => _ = vm.ToggleLikeAsync(likeIcon), size: 28),
                GlyphBtn(MaterialIconKind.SkipPrevious, () => _ = vm.PreviousAsync(), size: 28),
                GlyphBtn(playIcon, () => _ = vm.PlayPauseAsync(), size: 28, playStyle: true),
                GlyphBtn(MaterialIconKind.SkipNext, () => _ = vm.NextAsync(), size: 28),
                GlyphBtn(shuffleIcon, () => _ = vm.ToggleShuffleAsync(shuffleIcon), size: 28)
            }
        };
        var (scrub, track, pos, dur) = ScrubberStacked(vm, 260);
        TesseraLiveAmbient.RegisterMedia(art, title, artist, track, pos, dur, playIcon);
        var top = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        var left = new StackPanel
        {
            Margin = new Thickness(0, 0, 8, 0),
            Children = { header, title, artist }
        };
        Grid.SetColumn(left, 0);
        Grid.SetColumn(art, 1);
        top.Children.Add(left);
        top.Children.Add(art);
        return TesseraChrome.Glass(
            new StackPanel { Spacing = 4, Children = { top, scrub, transport } },
            12,
            new Thickness(12),
            w: 320,
            h: 190);
    }

    private static Control SimpleRow(TesseraFlyoutViewModel vm)
    {
        var art = AlbumArt(vm, 64);
        var title = Text(vm.MediaTitle, 15, FontWeight.SemiBold, 200);
        var artist = Text(vm.MediaArtist, 12, FontWeight.Normal, 200, muted: true);
        var time = Text($"{FormatTime(vm.MediaPositionSeconds)} / {FormatTime(vm.MediaDurationSeconds)}", 11, FontWeight.Normal, 160, muted: true);
        time.Name = "TesseraMediaPos";
        var heart = GlyphBtn(MaterialIconKind.Heart, () => { });
        heart.HorizontalAlignment = HorizontalAlignment.Left;
        TesseraLiveAmbient.RegisterMedia(art, title, artist, null, time, null, null);
        return TesseraChrome.Glass(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children =
            {
                art,
                new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center, Children = { title, artist, time, heart } }
            }
        }, 14, new Thickness(12), w: 320);
    }

    private static Control GnomePill(TesseraFlyoutViewModel vm)
    {
        var art = AlbumArt(vm, 36);
        art.CornerRadius = new CornerRadius(18);
        var title = Text(vm.MediaTitle, 13, FontWeight.SemiBold, 140);
        var artist = Text(vm.MediaArtist, 11, FontWeight.Normal, 140, muted: true);
        var playIcon = PlayIcon(vm, 16);
        var prev = GlyphBtn(MaterialIconKind.SkipPrevious, () => _ = vm.PreviousAsync(), size: 32);
        var play = GlyphBtn(playIcon, () => _ = vm.PlayPauseAsync(), size: 32, playStyle: true);
        var next = GlyphBtn(MaterialIconKind.SkipNext, () => _ = vm.NextAsync(), size: 32);
        TesseraLiveAmbient.RegisterMedia(art, title, artist, null, null, null, playIcon);
        return TesseraChrome.Glass(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                art,
                new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center, Width = 140, Children = { title, artist } },
                prev,
                play,
                next
            }
        }, 28, new Thickness(10, 8), w: 340);
    }

    private static Control AmberCard(TesseraFlyoutViewModel vm)
    {
        var art = AlbumArt(vm, 120);
        art.HorizontalAlignment = HorizontalAlignment.Center;
        art.Margin = new Thickness(0, 8, 0, 8);
        var title = Text(vm.MediaTitle, 15, FontWeight.SemiBold, 160);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.TextAlignment = TextAlignment.Center;
        var artist = Text(vm.MediaArtist, 12, FontWeight.Normal, 160, muted: true);
        artist.HorizontalAlignment = HorizontalAlignment.Center;
        artist.TextAlignment = TextAlignment.Center;
        var playIcon = PlayIcon(vm);
        var transport = TransportRow(vm, playIcon, shuffleRepeat: false, spacing: 20);
        transport.Margin = new Thickness(0, 10, 0, 4);
        TesseraLiveAmbient.RegisterMedia(art, title, artist, null, null, null, playIcon);
        return TesseraChrome.Glass(new StackPanel
        {
            Width = 180,
            Children = { art, title, artist, transport }
        }, 16, new Thickness(14, 10));
    }

    private static Control CoreUiBlock(TesseraFlyoutViewModel vm, double height = 150)
    {
        const double pad = TesseraStyleMetrics.CoreUiPad;
        var artSize = height - pad * 2;
        var art = AlbumArt(vm, artSize);
        art.Name = "TesseraMediaArt";
        var header = TesseraChrome.Mono("Media playing", 8, muted: true);
        var title = Text(vm.MediaTitle, 12, FontWeight.Bold, 220);
        title.FontFamily = new FontFamily("Poppins, Segoe UI");
        var artist = Text(vm.MediaArtist, 11, FontWeight.Normal, 220, muted: true);
        artist.FontFamily = new FontFamily("Poppins, Segoe UI");
        var time = TesseraChrome.Mono($"{FormatTime(vm.MediaPositionSeconds)} / {FormatTime(vm.MediaDurationSeconds)}", 9, muted: true);
        time.Name = "TesseraMediaPos";

        var textCol = new StackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, pad, 0),
            Children = { header, title, artist, time }
        };

        var row = new Grid
        {
            Height = height,
            ColumnDefinitions = new ColumnDefinitions($"{artSize},*")
        };
        Grid.SetColumn(art, 0);
        Grid.SetColumn(textCol, 1);
        row.Children.Add(art);
        row.Children.Add(textCol);

        TesseraLiveAmbient.RegisterMedia(art, title, artist, null, time, null, null);
        return TesseraChrome.GlassTinted(row, 8, TesseraStylePalette.CoreUi.TileBrush, new Thickness(pad), h: height);
    }

    private static Control SmoutiSide(TesseraFlyoutViewModel vm)
    {
        var title = Text(vm.MediaTitle, 13, FontWeight.SemiBold, 220);
        title.Foreground = TesseraStylePalette.Smouti.BrightBrush;
        title.TextTrimming = TextTrimming.CharacterEllipsis;
        var artist = Text(vm.MediaArtist, 11, FontWeight.Normal, 220, muted: true);
        artist.Foreground = TesseraStylePalette.Smouti.AccentHiBrush;
        artist.TextTrimming = TextTrimming.CharacterEllipsis;
        var pos = Text(FormatTime(vm.MediaPositionSeconds), 14, FontWeight.Bold, 48);
        pos.Foreground = TesseraStylePalette.Smouti.BrightBrush;
        var dur = Text(FormatTime(vm.MediaDurationSeconds), 11, FontWeight.Normal, 44, muted: true);
        dur.Foreground = TesseraStylePalette.Smouti.AccentHiBrush;
        var scrub = new TesseraTrack
        {
            IsVertical = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 16,
            Value = vm.MediaProgress,
            TrackThickness = 3,
            ShowThumb = false,
            AccentBrushOverride = TesseraStylePalette.Smouti.AccentBrush,
            TrackBackBrushOverride = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255))
        };
        scrub.ValueChanged += (_, v) =>
        {
            var d = vm.Services.Media.Current?.DurationSeconds ?? vm.MediaDurationSeconds;
            if (d > 0) _ = vm.SeekAsync(v * d);
        };
        var playIcon = PlayIcon(vm, 16);
        playIcon.Foreground = TesseraStylePalette.Smouti.BrightBrush;
        var likeIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.HeartOutline,
            Width = 14,
            Height = 14,
            Foreground = TesseraStylePalette.Smouti.BrightBrush
        };
        var shuffleIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.Shuffle,
            Width = 14,
            Height = 14,
            Foreground = TesseraStylePalette.Smouti.AccentHiBrush
        };
        var repeatIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.Repeat,
            Width = 14,
            Height = 14,
            Foreground = TesseraStylePalette.Smouti.AccentHiBrush
        };
        TesseraLiveAmbient.RegisterMedia(new Border { Name = "TesseraMediaArt", Width = 1, Height = 1, IsVisible = false },
            title, artist, scrub, pos, dur, playIcon);

        var topIcons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 0, 2),
            Children =
            {
                GlyphBtn(likeIcon, () => _ = vm.ToggleLikeAsync(likeIcon), size: 24),
                GlyphBtn(repeatIcon, () => _ = vm.ToggleRepeatAsync(repeatIcon), size: 24),
                GlyphBtn(shuffleIcon, () => _ = vm.ToggleShuffleAsync(shuffleIcon), size: 24)
            }
        };
        var timeRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Margin = new Thickness(0, 6, 0, 6)
        };
        Grid.SetColumn(pos, 0);
        Grid.SetColumn(scrub, 1);
        Grid.SetColumn(dur, 2);
        scrub.Margin = new Thickness(8, 0);
        scrub.VerticalAlignment = VerticalAlignment.Center;
        pos.VerticalAlignment = VerticalAlignment.Center;
        dur.VerticalAlignment = VerticalAlignment.Center;
        timeRow.Children.Add(pos);
        timeRow.Children.Add(scrub);
        timeRow.Children.Add(dur);

        var transport = new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)),
            Padding = new Thickness(8, 4),
            HorizontalAlignment = HorizontalAlignment.Right,
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children =
                {
                    GlyphBtn(playIcon, () => _ = vm.PlayPauseAsync(), size: 26, playStyle: true),
                    GlyphBtn(MaterialIconKind.SkipNext, () => _ = vm.NextAsync(), size: 26)
                }
            }
        };

        return new StackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            MaxWidth = 300,
            Children = { topIcons, timeRow, title, artist, transport }
        };
    }

    private static (StackPanel Col, TesseraTrack Track, TextBlock Pos, TextBlock Dur) ScrubberStacked(
        TesseraFlyoutViewModel vm, double width)
    {
        var pos = Text(FormatTime(vm.MediaPositionSeconds), 10, FontWeight.SemiBold, 40, muted: true);
        var dur = Text(FormatTime(vm.MediaDurationSeconds), 10, FontWeight.SemiBold, 40, muted: true);
        var track = new TesseraTrack { IsVertical = false, Width = width, Height = 22, Value = vm.MediaProgress, TrackThickness = 3 };
        track.ValueChanged += (_, v) =>
        {
            var d = vm.Services.Media.Current?.DurationSeconds ?? vm.MediaDurationSeconds;
            if (d > 0) _ = vm.SeekAsync(v * d);
        };
        var times = new Grid { Width = width, ColumnDefinitions = new ColumnDefinitions("*,*") };
        pos.HorizontalAlignment = HorizontalAlignment.Left;
        dur.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(dur, 1);
        times.Children.Add(pos);
        times.Children.Add(dur);
        var col = new StackPanel { Spacing = 2, Children = { track, times } };
        return (col, track, pos, dur);
    }

    private static StackPanel TransportRow(
        TesseraFlyoutViewModel vm, MaterialIcon playIcon, bool shuffleRepeat, double spacing, double btnSize = 32)
    {
        var kids = new List<Control>();
        if (shuffleRepeat)
        {
            var shuffleIcon = new MaterialIcon
            {
                Kind = MaterialIconKind.Shuffle,
                Width = btnSize * 0.5,
                Height = btnSize * 0.5,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255))
            };
            kids.Add(GlyphBtn(shuffleIcon, () => _ = vm.ToggleShuffleAsync(shuffleIcon), btnSize));
        }
        kids.Add(GlyphBtn(MaterialIconKind.SkipPrevious, () => _ = vm.PreviousAsync(), size: btnSize));
        kids.Add(GlyphBtn(playIcon, () => _ = vm.PlayPauseAsync(), size: btnSize, playStyle: true));
        kids.Add(GlyphBtn(MaterialIconKind.SkipNext, () => _ = vm.NextAsync(), size: btnSize));
        if (shuffleRepeat)
        {
            var likeIcon = new MaterialIcon
            {
                Kind = MaterialIconKind.HeartOutline,
                Width = btnSize * 0.5,
                Height = btnSize * 0.5,
                Foreground = TesseraPalette.FontBrush
            };
            // Official Fluent/Modern: heart often opposite shuffle; keep both wired
            var repeatIcon = new MaterialIcon
            {
                Kind = MaterialIconKind.Repeat,
                Width = btnSize * 0.5,
                Height = btnSize * 0.5,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255))
            };
            kids.Insert(0, GlyphBtn(likeIcon, () => _ = vm.ToggleLikeAsync(likeIcon), btnSize));
            kids.Add(GlyphBtn(repeatIcon, () => _ = vm.ToggleRepeatAsync(repeatIcon), btnSize));
        }
        var sp = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = spacing
        };
        foreach (var c in kids) sp.Children.Add(c);
        return sp;
    }

    private static MaterialIcon PlayIcon(TesseraFlyoutViewModel vm, double size = 20) =>
        new()
        {
            Kind = vm.IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play,
            Width = size,
            Height = size,
            Foreground = TesseraPalette.FontBrush
        };

    public static Border AlbumArt(TesseraFlyoutViewModel vm, double size)
    {
        var border = new Border
        {
            Name = "TesseraMediaArt",
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size > 70 ? 8 : 6),
            ClipToBounds = true,
            Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
            Child = new MaterialIcon
            {
                Kind = MaterialIconKind.Music,
                Width = size * 0.4,
                Height = size * 0.4,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = TesseraPalette.FontMutedBrush
            }
        };
        ApplyArtToBorder(border, vm.ThumbnailPng);
        return border;
    }

    public static Bitmap? TryCreateBitmap(byte[]? bytes, int decodeWidth = 128)
    {
        if (bytes is null || bytes.Length < 32) return null;
        try
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"mosaic-art-{Guid.NewGuid():N}.img");
            File.WriteAllBytes(tmp, bytes);
            try
            {
                using var fs = File.OpenRead(tmp);
                return Bitmap.DecodeToWidth(fs, Math.Max(32, decodeWidth));
            }
            finally
            {
                try { File.Delete(tmp); } catch { /* ignore */ }
            }
        }
        catch { /* fall through */ }

        try
        {
            using var ms = new MemoryStream(bytes, writable: false);
            return Bitmap.DecodeToWidth(ms, Math.Max(32, decodeWidth));
        }
        catch
        {
            try
            {
                using var ms = new MemoryStream(bytes, writable: false);
                return new Bitmap(ms);
            }
            catch { return null; }
        }
    }

    public static void ApplyArtToBorder(Border border, byte[]? bytes, bool fillHost = false)
    {
        if (bytes is null || bytes.Length < 32) return;
        var sig = bytes.Length ^ (bytes.Length > 16 ? bytes[8] << 8 | bytes[16] : 0);
        if (border.Tag is int prev && prev == sig && border.Child is Image) return;
        var size = !fillHost && border.Width > 1 && !double.IsNaN(border.Width)
            ? border.Width
            : fillHost ? 128 : 64;
        if (TryCreateBitmap(bytes, (int)size) is not { } bmp) return;
        border.Tag = sig;
        border.ClipToBounds = true;
        if (border.Child is Image img)
        {
            var old = img.Source;
            img.Source = bmp;
            (old as IDisposable)?.Dispose();
            if (fillHost)
            {
                img.Width = double.NaN;
                img.Height = double.NaN;
                img.HorizontalAlignment = HorizontalAlignment.Stretch;
                img.VerticalAlignment = VerticalAlignment.Stretch;
                img.Stretch = Stretch.UniformToFill;
            }
        }
        else
        {
            border.Background = Brushes.Transparent;
            border.Child = fillHost
                ? new Image
                {
                    Source = bmp,
                    Stretch = Stretch.UniformToFill,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                }
                : new Image
                {
                    Source = bmp,
                    Stretch = Stretch.UniformToFill,
                    Width = size,
                    Height = size
                };
        }
    }

    private static Control GlyphBtn(MaterialIconKind kind, Action act, bool dim = false, double size = 36, bool playStyle = false)
    {
        var icon = new MaterialIcon
        {
            Kind = kind,
            Width = size * 0.55,
            Height = size * 0.55,
            Foreground = dim
                ? new SolidColorBrush(Color.FromArgb(150, 255, 255, 255))
                : TesseraPalette.FontBrush
        };
        return GlyphBtn(icon, act, size, playStyle);
    }

    private static Control GlyphBtn(MaterialIcon icon, Action act, double size = 36, bool playStyle = false) =>
        TesseraChrome.IconButton(icon, act, size, circularHighlight: !playStyle);

    private static TextBlock Text(string text, double size, FontWeight weight, double maxWidth, bool muted = false) =>
        new()
        {
            Text = string.IsNullOrWhiteSpace(text) ? " " : text,
            FontSize = size,
            FontWeight = weight,
            Foreground = muted ? TesseraPalette.FontMutedBrush : TesseraPalette.FontBrush,
            FontFamily = new FontFamily("Segoe UI Variable, Segoe UI"),
            MaxWidth = maxWidth,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

    private static string FormatTime(double seconds)
    {
        if (seconds <= 0 || double.IsNaN(seconds)) return "0:00";
        var t = TimeSpan.FromSeconds(seconds);
        return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
    }
}
