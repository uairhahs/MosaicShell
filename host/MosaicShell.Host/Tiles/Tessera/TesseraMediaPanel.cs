using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Material.Icons;
using Material.Icons.Avalonia;

namespace MosaicShell.Host.Tiles.Tessera;

public enum TesseraMediaMode
{
    /// <summary>Fluent side panel ~500×200 with 64 art, scrubber, shuffle/repeat.</summary>
    FluentSide,
    /// <summary>Win11 below panel ~320×175 with 80 art.</summary>
    Win11Below
}

public static class TesseraMediaPanel
{
    public static Control Create(TesseraFlyoutViewModel vm, TesseraMediaMode mode)
    {
        return mode switch
        {
            TesseraMediaMode.Win11Below => Win11(vm),
            _ => Fluent(vm)
        };
    }

    private static Control Fluent(TesseraFlyoutViewModel vm)
    {
        const double mediaW = 480;
        const double h = 200;
        var art = AlbumArt(vm, 64);
        art.Margin = new Thickness(20, 20, 0, 0);

        var titles = new StackPanel
        {
            Margin = new Thickness(12, 20, 20, 0),
            Spacing = 2,
            Children =
            {
                Text(vm.MediaTitle, 18, FontWeight.SemiBold, 320),
                Text(vm.MediaArtist, 12, FontWeight.Normal, 320, muted: true)
            }
        };

        var top = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { art, titles }
        };

        var transport = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 16,
            Margin = new Thickness(0, 12, 0, 0),
            Children =
            {
                GlyphBtn(MaterialIconKind.Shuffle, () => { }, dim: true),
                GlyphBtn(MaterialIconKind.SkipPrevious, () => _ = vm.PreviousAsync()),
                GlyphBtn(vm.IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play, () => _ = vm.PlayPauseAsync()),
                GlyphBtn(MaterialIconKind.SkipNext, () => _ = vm.NextAsync()),
                GlyphBtn(MaterialIconKind.Repeat, () => { }, dim: true)
            }
        };

        var scrub = Scrubber(vm, mediaW - 100);
        scrub.Margin = new Thickness(40, 8, 40, 16);

        return new Border
        {
            Width = mediaW,
            Height = h,
            Background = Brushes.Transparent,
            Child = new StackPanel { Children = { top, transport, scrub } }
        };
    }

    private static Control Win11(TesseraFlyoutViewModel vm)
    {
        const double w = 320;
        const double mediaH = 160;
        var art = AlbumArt(vm, 80);
        Canvas.SetLeft(art, w - 80 - 15);
        Canvas.SetTop(art, 15);

        var titles = new StackPanel
        {
            Margin = new Thickness(15, 15, 100, 0),
            Spacing = 4,
            Children =
            {
                Text(vm.MediaTitle, 12, FontWeight.SemiBold, 180),
                Text(vm.MediaArtist, 11, FontWeight.Normal, 180, muted: true)
            }
        };

        var transport = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 20,
            Margin = new Thickness(0, 50, 0, 0),
            Children =
            {
                GlyphBtn(MaterialIconKind.SkipPrevious, () => _ = vm.PreviousAsync()),
                GlyphBtn(vm.IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play, () => _ = vm.PlayPauseAsync()),
                GlyphBtn(MaterialIconKind.SkipNext, () => _ = vm.NextAsync())
            }
        };

        var root = new Canvas { Width = w, Height = mediaH };
        root.Children.Add(titles);
        root.Children.Add(art);
        // transport as overlay at bottom-ish
        var transportHost = new Border
        {
            Width = w,
            Child = transport
        };
        Canvas.SetTop(transportHost, 95);
        root.Children.Add(transportHost);
        return root;
    }

    private static Control Scrubber(TesseraFlyoutViewModel vm, double width)
    {
        var track = new TesseraTrack
        {
            IsVertical = false,
            Width = width,
            Height = 24,
            Value = vm.MediaProgress
        };
        track.ValueChanged += (_, v) =>
        {
            if (vm.MediaDurationSeconds > 0)
                _ = vm.SeekAsync(v * vm.MediaDurationSeconds);
        };

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                Text(FormatTime(vm.MediaPositionSeconds), 10, FontWeight.SemiBold, 36, muted: true),
                track,
                Text(FormatTime(vm.MediaDurationSeconds), 10, FontWeight.SemiBold, 36, muted: true)
            }
        };
    }

    private static Control AlbumArt(TesseraFlyoutViewModel vm, double size)
    {
        var border = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size > 70 ? 8 : 6),
            ClipToBounds = true,
            Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255))
        };

        if (vm.ThumbnailPng is { Length: > 32 } bytes)
        {
            try
            {
                using var ms = new MemoryStream(bytes);
                var bmp = new Bitmap(ms);
                border.Child = new Image
                {
                    Source = bmp,
                    Stretch = Stretch.UniformToFill,
                    Width = size,
                    Height = size
                };
                return border;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Tessera art] decode fail: {ex.Message}");
            }
        }

        border.Child = PlaceholderArt(size);
        return border;
    }

    private static Control PlaceholderArt(double size) =>
        new MaterialIcon
        {
            Kind = MaterialIconKind.Music,
            Width = size * 0.4,
            Height = size * 0.4,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = TesseraPalette.FontMutedBrush
        };

    private static Button GlyphBtn(MaterialIconKind kind, Action act, bool dim = false)
    {
        var b = new Button
        {
            Width = 28,
            Height = 28,
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            Content = new MaterialIcon
            {
                Kind = kind,
                Width = 18,
                Height = 18,
                Foreground = dim
                    ? new SolidColorBrush(Color.FromArgb(150, 255, 255, 255))
                    : TesseraPalette.FontBrush
            }
        };
        b.Click += (_, _) => act();
        return b;
    }

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
