using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Material.Icons;
using Material.Icons.Avalonia;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;

namespace MosaicShell.Host.Tiles.Tessera
{
    public enum TesseraMediaMode
    {
        FluentSide,
        Windows11Below,
        ModernCard,
        SimpleRow,
        GnomePill,
        MeterCard,
        CoreUiBlock,
        RadialSide
    }

    public static class TesseraMediaPanel
    {
        public static Control Create(TesseraFlyoutViewModel vm, TesseraMediaMode mode, double coreUiHeight = 72)
        {
            return mode switch
            {
                TesseraMediaMode.Windows11Below => Windows11(vm),
                TesseraMediaMode.ModernCard => ModernCard(vm),
                TesseraMediaMode.SimpleRow => SimpleRow(vm),
                TesseraMediaMode.GnomePill => GnomePill(vm),
                TesseraMediaMode.MeterCard => MeterCard(vm),
                TesseraMediaMode.CoreUiBlock => CoreUiBlock(vm, coreUiHeight),
                TesseraMediaMode.RadialSide => RadialSide(vm),
                _ => Fluent(vm),
            };
        }

        private static Control Fluent(TesseraFlyoutViewModel vm)
        {
            const double mediaW = TesseraFluentMetrics.MediaWidth - 16;
            const double h = TesseraFluentMetrics.Height;
            Border art = AlbumArt(vm, 56);
            art.Margin = new Thickness(14, 14, 0, 0);
            TextBlock title = Text(vm.MediaTitle, 15, FontWeight.SemiBold, 240);
            TextBlock artist = Text(vm.MediaArtist, 11, FontWeight.Normal, 240, muted: true);
            StackPanel titles = new()
            {
                Margin = new Thickness(10, 14, 14, 0),
                Spacing = 2,
                Children = { TesseraMarqueeText.Wrap(title, 240, vm.AutoDismissMs), artist }
            };
            MaterialIcon playIcon = PlayIcon(vm, 16);
            (StackPanel? transport, MaterialIcon? likeIcon, MaterialIcon? dislikeIcon) = TransportRow(vm, playIcon, shuffleRepeat: true, spacing: 12, btnSize: 28);
            transport.Margin = new Thickness(0, 6, 0, 0);
            (StackPanel? scrubCol, TesseraTrack? scrub, TextBlock? pos, TextBlock? dur) = ScrubberStacked(vm, mediaW - 80);
            scrubCol.Margin = new Thickness(28, 4, 28, 10);
            TesseraLiveAmbient.RegisterMedia(art, title, artist, scrub, pos, dur, playIcon, likeIcon, dislikeIcon);

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

        private static Control Windows11(TesseraFlyoutViewModel vm)
        {
            const double w = TesseraWindows11Metrics.Width;
            const double pad = TesseraWindows11Metrics.Pad;
            Border art = AlbumArt(vm, 80);
            art.HorizontalAlignment = HorizontalAlignment.Right;
            art.VerticalAlignment = VerticalAlignment.Top;
            StackPanel header = new()
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
            double titleWidth = w - 80 - (pad * 3);
            TextBlock title = Text(vm.MediaTitle, 12, FontWeight.SemiBold, titleWidth);
            TextBlock artist = Text(vm.MediaArtist, 11, FontWeight.Normal, titleWidth, muted: true);
            // Same box as the title's marquee viewport, anchored at the same left edge, so artist
            // lines up flush with title instead of stretching to whatever the column's widest
            // sibling (e.g. the header row) happens to need.
            artist.HorizontalAlignment = HorizontalAlignment.Left;
            StackPanel textCol = new() { Spacing = 4, Margin = new Thickness(0, 4, 0, 0), Children = { header, TesseraMarqueeText.Wrap(title, titleWidth, vm.AutoDismissMs), artist } };
            Grid top = new()
            {
                ColumnDefinitions = new ColumnDefinitions("*,80"),
                Margin = new Thickness(pad, 8, pad, 0)
            };
            Grid.SetColumn(textCol, 0);
            Grid.SetColumn(art, 1);
            top.Children.Add(textCol);
            top.Children.Add(art);

            MaterialIcon playIcon = PlayIcon(vm, 16);
            MaterialIcon likeIcon = CreateLikeIcon(14);
            MaterialIcon? dislikeIcon = null;
            MaterialIcon shuffleIcon = new()
            {
                Kind = MaterialIconKind.Shuffle,
                Width = 14,
                Height = 14,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255))
            };
            (StackPanel? scrubCol, TesseraTrack? scrub, TextBlock? pos, TextBlock? dur) = ScrubberStacked(vm, w - (pad * 2));
            scrub.AccentBrushOverride = TesseraStylePalette.Windows11.AccentBrush;
            scrubCol.Margin = new Thickness(pad, 6, pad, 0);
            List<Control> transportKids = [];
            dislikeIcon = AppendRatingButtons(vm, transportKids, likeIcon, 28);
            transportKids.Add(GlyphBtn(MaterialIconKind.SkipPrevious, () => _ = vm.PreviousAsync(), size: 28));
            transportKids.Add(GlyphBtn(playIcon, () => _ = vm.PlayPauseAsync(), size: 28, playStyle: true));
            transportKids.Add(GlyphBtn(MaterialIconKind.SkipNext, () => _ = vm.NextAsync(), size: 28));
            transportKids.Add(GlyphBtn(shuffleIcon, () => _ = vm.ToggleShuffleAsync(shuffleIcon), size: 28));
            StackPanel transport = new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 16,
                Margin = new Thickness(0, 8, 0, 10),
                Children = { }
            };
            foreach (Control kid in transportKids)
            {
                transport.Children.Add(kid);
            }

            TesseraLiveAmbient.RegisterMedia(art, title, artist, scrub, pos, dur, playIcon, likeIcon, dislikeIcon);
            return new Border
            {
                Name = "TesseraMediaRoot",
                Width = w,
                Height = TesseraWindows11Metrics.MediaHeight,
                Background = Brushes.Transparent,
                Child = new StackPanel { Children = { top, scrubCol, transport } }
            };
        }

        private static Control ModernCard(TesseraFlyoutViewModel vm)
        {
            // Modern.inc: dark MediaC shell + square cover top-right (80×80), not full-card art wash.
            const double artSize = 80;
            Border art = AlbumArt(vm, artSize);
            art.HorizontalAlignment = HorizontalAlignment.Right;
            art.VerticalAlignment = VerticalAlignment.Top;
            TextBlock header = TesseraChrome.Label("Media playing", 10, muted: true);
            header.Margin = new Thickness(0, 0, 0, 4);
            TextBlock title = Text(vm.MediaTitle, 15, FontWeight.Bold, 220);
            TextBlock artist = Text(vm.MediaArtist, 11, FontWeight.Normal, 220, muted: true);
            MaterialIcon playIcon = PlayIcon(vm, 16);
            MaterialIcon likeIcon = CreateLikeIcon(14);
            MaterialIcon? dislikeIcon = null;
            MaterialIcon shuffleIcon = new()
            {
                Kind = MaterialIconKind.Shuffle,
                Width = 14,
                Height = 14,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255))
            };
            List<Control> transportKids = [];
            dislikeIcon = AppendRatingButtons(vm, transportKids, likeIcon, 28);
            transportKids.Add(GlyphBtn(MaterialIconKind.SkipPrevious, () => _ = vm.PreviousAsync(), size: 28));
            transportKids.Add(GlyphBtn(playIcon, () => _ = vm.PlayPauseAsync(), size: 28, playStyle: true));
            transportKids.Add(GlyphBtn(MaterialIconKind.SkipNext, () => _ = vm.NextAsync(), size: 28));
            transportKids.Add(GlyphBtn(shuffleIcon, () => _ = vm.ToggleShuffleAsync(shuffleIcon), size: 28));
            StackPanel transport = new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 12,
                Margin = new Thickness(0, 6, 0, 0),
                Children = { }
            };
            foreach (Control kid in transportKids)
            {
                transport.Children.Add(kid);
            }

            (StackPanel? scrub, TesseraTrack? track, TextBlock? pos, TextBlock? dur) = ScrubberStacked(vm, 260);
            TesseraLiveAmbient.RegisterMedia(art, title, artist, track, pos, dur, playIcon, likeIcon, dislikeIcon);
            Grid top = new() { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            StackPanel left = new()
            {
                Margin = new Thickness(0, 0, 8, 0),
                Children = { header, TesseraMarqueeText.Wrap(title, 220, vm.AutoDismissMs), artist }
            };
            Grid.SetColumn(left, 0);
            Grid.SetColumn(art, 1);
            top.Children.Add(left);
            top.Children.Add(art);
            StackPanel body = new() { Spacing = 4, Children = { top, scrub, transport } };

            return TesseraStackedBuildContext.IsActive && TesseraGlass.UseOsAcrylicChrome
                ? new Border
                {
                    Width = TesseraStackedPlacementSpec.ModernFlyoutsMediaWidthDip,
                    Height = TesseraStackedPlacementSpec.ModernFlyoutsMediaHeightDip,
                    CornerRadius = new CornerRadius(12),
                    Background = Brushes.Transparent,
                    Padding = new Thickness(12),
                    ClipToBounds = true,
                    Child = body
                }
                : TesseraChrome.Glass(body, 12, new Thickness(12), w: 320, h: 190);
        }

        private static Control SimpleRow(TesseraFlyoutViewModel vm)
        {
            bool stackedOsAcrylic = TesseraStackedBuildContext.IsActive && TesseraGlass.UseOsAcrylicChrome;
            Border art = AlbumArt(vm, 64);
            TextBlock title = Text(vm.MediaTitle, 15, FontWeight.SemiBold, 200);
            TextBlock artist = Text(vm.MediaArtist, 12, FontWeight.Normal, 200, muted: true);
            TextBlock time = Text($"{FormatTime(vm.MediaPositionSeconds)} / {FormatTime(vm.MediaDurationSeconds)}", 11, FontWeight.Normal, 160, muted: true);
            time.Name = "TesseraMediaPos";
            Control heart = GlyphBtn(MaterialIconKind.Heart, () => { });
            heart.HorizontalAlignment = HorizontalAlignment.Left;
            TesseraLiveAmbient.RegisterMedia(art, title, artist, null, time, null, null);
            StackPanel body = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                Children =
                {
                    art,
                    new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center, Children = { TesseraMarqueeText.Wrap(title, 200, vm.AutoDismissMs), artist, time, heart } }
                }
            };

            return stackedOsAcrylic
                ? new Border
                {
                    Width = TesseraStackedPlacementSpec.CompactMediaWidthDip,
                    Height = TesseraStackedPlacementSpec.CompactMediaHeightDip,
                    CornerRadius = new CornerRadius(14),
                    Background = Brushes.Transparent,
                    Padding = new Thickness(12),
                    ClipToBounds = true,
                    Child = body
                }
                : TesseraChrome.Glass(body, 14, new Thickness(12), w: 320);
        }

        private static Control GnomePill(TesseraFlyoutViewModel vm)
        {
            bool stackedOsAcrylic = TesseraStackedBuildContext.IsActive && TesseraGlass.UseOsAcrylicChrome;
            Border art = AlbumArt(vm, 36);
            art.CornerRadius = new CornerRadius(18);
            TextBlock title = Text(vm.MediaTitle, 13, FontWeight.SemiBold, 140);
            TextBlock artist = Text(vm.MediaArtist, 11, FontWeight.Normal, 140, muted: true);
            MaterialIcon playIcon = PlayIcon(vm, 16);
            Control prev = GlyphBtn(MaterialIconKind.SkipPrevious, () => _ = vm.PreviousAsync(), size: 32);
            Control play = GlyphBtn(playIcon, () => _ = vm.PlayPauseAsync(), size: 32, playStyle: true);
            Control next = GlyphBtn(MaterialIconKind.SkipNext, () => _ = vm.NextAsync(), size: 32);
            TesseraLiveAmbient.RegisterMedia(art, title, artist, null, null, null, playIcon);
            StackPanel body = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    art,
                    new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center, Width = 140, Children = { TesseraMarqueeText.Wrap(title, 140, vm.AutoDismissMs), artist } },
                    prev,
                    play,
                    next
                }
            };

            return stackedOsAcrylic
                ? new Border
                {
                    Width = TesseraStackedPlacementSpec.GnomeMediaWidthDip,
                    Height = TesseraStackedPlacementSpec.GnomeMediaHeightDip,
                    CornerRadius = new CornerRadius(TesseraStackedPlacementSpec.GnomePillCornerRadiusDip),
                    Background = Brushes.Transparent,
                    Padding = new Thickness(10, 8),
                    ClipToBounds = true,
                    Child = body
                }
                : TesseraChrome.Glass(body, 28, new Thickness(10, 8), w: 340);
        }

        private static Control MeterCard(TesseraFlyoutViewModel vm)
        {
            bool stacked = TesseraStackedBuildContext.IsActive;
            bool stackedOsAcrylic = stacked && TesseraGlass.UseOsAcrylicChrome;
            int artSize = stacked
                ? (int)TesseraStackedPlacementSpec.MeterMediaArtDip
                : 120;
            Border art = AlbumArt(vm, artSize);
            art.HorizontalAlignment = HorizontalAlignment.Center;
            art.Margin = stacked
                ? new Thickness(0, 0, 0, TesseraStackedPlacementSpec.MeterMediaArtBottomMarginDip)
                : new Thickness(0, 8, 0, 8);
            TextBlock title = Text(vm.MediaTitle, 15, FontWeight.SemiBold, 160);
            Control titleDisplay = TesseraMarqueeText.Wrap(title, 160, vm.AutoDismissMs, centerWhenStatic: true);
            titleDisplay.Height = TesseraStackedPlacementSpec.MeterMediaTitleLineDip;
            TextBlock artist = Text(vm.MediaArtist, 12, FontWeight.Normal, 160, muted: true);
            artist.HorizontalAlignment = HorizontalAlignment.Center;
            artist.TextAlignment = TextAlignment.Center;
            artist.Height = TesseraStackedPlacementSpec.MeterMediaArtistLineDip;
            MaterialIcon playIcon = PlayIcon(vm);
            double transportBtn = stacked
                ? TesseraStackedPlacementSpec.MeterMediaTransportBtnDip
                : 32;
            (StackPanel? transport, MaterialIcon? _, MaterialIcon? _) = TransportRow(
                vm,
                playIcon,
                shuffleRepeat: false,
                spacing: stacked ? 16 : 20,
                btnSize: transportBtn);
            TesseraLiveAmbient.RegisterMedia(art, title, artist, null, null, null, playIcon);

            Control body;
            if (stacked)
            {
                transport.HorizontalAlignment = HorizontalAlignment.Center;
                transport.Margin = new Thickness(0, TesseraStackedPlacementSpec.MeterMediaTransportTopMarginDip, 0, 0);
                StackPanel header = new()
                {
                    Width = TesseraStackedPlacementSpec.MeterMediaBodyWidthDip,
                    Spacing = TesseraStackedPlacementSpec.MeterMediaHeaderSpacingDip,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Children = { art, titleDisplay, artist }
                };
                DockPanel dock = new()
                {
                    Width = TesseraStackedPlacementSpec.MeterMediaBodyWidthDip,
                    LastChildFill = true,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                DockPanel.SetDock(transport, Dock.Bottom);
                dock.Children.Add(transport);
                dock.Children.Add(header);
                body = dock;
            }
            else
            {
                transport.Margin = new Thickness(0, 10, 0, 4);
                body = new StackPanel
                {
                    Width = TesseraStackedPlacementSpec.MeterMediaBodyWidthDip,
                    Children = { art, titleDisplay, artist, transport }
                };
            }

            if (TesseraGlass.SuppressInnerSkiaGlass)
            {
                Thickness pad = stacked
                    ? new Thickness(
                        TesseraStackedPlacementSpec.MeterMediaPadHorizontalDip,
                        TesseraStackedPlacementSpec.MeterMediaPadTopDip,
                        TesseraStackedPlacementSpec.MeterMediaPadHorizontalDip,
                        TesseraStackedPlacementSpec.MeterMediaPadBottomDip)
                    : new Thickness(14, 10);
                Border border = new()
                {
                    CornerRadius = new CornerRadius(16),
                    Background = stackedOsAcrylic ? Brushes.Transparent : TesseraChrome.TileFace,
                    Padding = pad,
                    ClipToBounds = true,
                    Child = body
                };
                if (stacked)
                {
                    border.Width = TesseraStackedPlacementSpec.MeterMediaWidthDip;
                    border.Height = TesseraStackedPlacementSpec.MeterMediaHeightDip;
                }

                return border;
            }

            return TesseraChrome.Glass(body, 16, new Thickness(14, 10));
        }

        private static Control CoreUiBlock(TesseraFlyoutViewModel vm, double height = 150)
        {
            const double pad = TesseraStyleMetrics.CoreUiPad;
            double artSize = height - (pad * 2);
            Border art = AlbumArt(vm, artSize);
            art.Name = "TesseraMediaArt";
            TextBlock header = TesseraChrome.Mono("Media playing", 8, muted: true);
            TextBlock title = Text(vm.MediaTitle, 12, FontWeight.Bold, 220);
            title.FontFamily = new FontFamily("Poppins, Segoe UI");
            TextBlock artist = Text(vm.MediaArtist, 11, FontWeight.Normal, 220, muted: true);
            artist.FontFamily = new FontFamily("Poppins, Segoe UI");
            TextBlock time = TesseraChrome.Mono($"{FormatTime(vm.MediaPositionSeconds)} / {FormatTime(vm.MediaDurationSeconds)}", 9, muted: true);
            time.Name = "TesseraMediaPos";

            StackPanel textCol = new()
            {
                Spacing = 4,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, pad, 0),
                Children = { header, TesseraMarqueeText.Wrap(title, 220, vm.AutoDismissMs), artist, time }
            };

            Grid row = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ColumnDefinitions = new ColumnDefinitions($"{artSize},*")
            };
            Grid.SetColumn(art, 0);
            Grid.SetColumn(textCol, 1);
            row.Children.Add(art);
            row.Children.Add(textCol);

            TesseraLiveAmbient.RegisterMedia(art, title, artist, null, time, null, null);
            return TesseraChrome.CoreUiTile(row, h: height, pad: new Thickness(pad));
        }

        private static Control RadialSide(TesseraFlyoutViewModel vm)
        {
            TextBlock title = Text(vm.MediaTitle, 13, FontWeight.SemiBold, 220);
            title.Foreground = TesseraStylePalette.Radial.BrightBrush;
            TextBlock artist = Text(vm.MediaArtist, 11, FontWeight.Normal, 220, muted: true);
            artist.Foreground = TesseraStylePalette.Radial.AccentHiBrush;
            artist.TextTrimming = TextTrimming.CharacterEllipsis;
            // Same 220-wide box as the title's marquee viewport, anchored at the same left edge -
            // Center here means centered relative to the title, not the wider outer column.
            artist.HorizontalAlignment = HorizontalAlignment.Left;
            artist.TextAlignment = TextAlignment.Center;
            TextBlock pos = Text(FormatTime(vm.MediaPositionSeconds), 14, FontWeight.Bold, 48);
            pos.Foreground = TesseraStylePalette.Radial.BrightBrush;
            TextBlock dur = Text(FormatTime(vm.MediaDurationSeconds), 11, FontWeight.Normal, 44, muted: true);
            dur.Foreground = TesseraStylePalette.Radial.AccentHiBrush;
            TesseraTrack scrub = new()
            {
                IsVertical = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 16,
                Value = vm.MediaProgress,
                TrackThickness = 3,
                ShowThumb = false,
                AccentBrushOverride = TesseraStylePalette.Radial.AccentBrush,
                TrackBackBrushOverride = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255))
            };
            scrub.ValueChanged += (_, v) =>
            {
                double d = vm.Services.Media.Current?.DurationSeconds ?? vm.MediaDurationSeconds;
                if (d > 0)
                {
                    _ = vm.SeekAsync(v * d);
                }
            };
            MaterialIcon playIcon = PlayIcon(vm, 16);
            playIcon.Foreground = TesseraStylePalette.Radial.BrightBrush;
            MaterialIcon likeIcon = CreateLikeIcon(14, TesseraStylePalette.Radial.BrightBrush);
            MaterialIcon? dislikeIcon = null;
            MaterialIcon shuffleIcon = new()
            {
                Kind = MaterialIconKind.Shuffle,
                Width = 14,
                Height = 14,
                Foreground = TesseraStylePalette.Radial.AccentHiBrush
            };
            MaterialIcon repeatIcon = new()
            {
                Kind = MaterialIconKind.Repeat,
                Width = 14,
                Height = 14,
                Foreground = TesseraStylePalette.Radial.AccentHiBrush
            };
            List<Control> topIconKids = [];
            dislikeIcon = AppendRatingButtons(vm, topIconKids, likeIcon, 24, TesseraStylePalette.Radial.BrightBrush);
            topIconKids.Add(GlyphBtn(repeatIcon, () => _ = vm.ToggleRepeatAsync(repeatIcon), size: 24));
            topIconKids.Add(GlyphBtn(shuffleIcon, () => _ = vm.ToggleShuffleAsync(shuffleIcon), size: 24));
            TesseraLiveAmbient.RegisterMedia(
                new Border { Name = "TesseraMediaArt", Width = 1, Height = 1, IsVisible = false },
                title, artist, scrub, pos, dur, playIcon, likeIcon, dislikeIcon);

            StackPanel topIcons = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, 2),
                Children = { }
            };
            foreach (Control kid in topIconKids)
            {
                topIcons.Children.Add(kid);
            }

            Grid timeRow = new()
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

            Border transport = new()
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
                Children = { topIcons, timeRow, TesseraMarqueeText.Wrap(title, 220, vm.AutoDismissMs), artist, transport }
            };
        }

        private static (StackPanel Col, TesseraTrack Track, TextBlock Pos, TextBlock Dur) ScrubberStacked(
            TesseraFlyoutViewModel vm, double width)
        {
            TextBlock pos = Text(FormatTime(vm.MediaPositionSeconds), 10, FontWeight.SemiBold, 40, muted: true);
            TextBlock dur = Text(FormatTime(vm.MediaDurationSeconds), 10, FontWeight.SemiBold, 40, muted: true);
            TesseraTrack track = new() { IsVertical = false, Width = width, Height = 22, Value = vm.MediaProgress, TrackThickness = 3 };
            track.ValueChanged += (_, v) =>
            {
                double d = vm.Services.Media.Current?.DurationSeconds ?? vm.MediaDurationSeconds;
                if (d > 0)
                {
                    _ = vm.SeekAsync(v * d);
                }
            };
            Grid times = new() { Width = width, ColumnDefinitions = new ColumnDefinitions("*,*") };
            pos.HorizontalAlignment = HorizontalAlignment.Left;
            dur.HorizontalAlignment = HorizontalAlignment.Right;
            Grid.SetColumn(dur, 1);
            times.Children.Add(pos);
            times.Children.Add(dur);
            StackPanel col = new() { Spacing = 2, Children = { track, times } };
            return (col, track, pos, dur);
        }

        private static (StackPanel Row, MaterialIcon? LikeIcon, MaterialIcon? DislikeIcon) TransportRow(
            TesseraFlyoutViewModel vm, MaterialIcon playIcon, bool shuffleRepeat, double spacing, double btnSize = 32)
        {
            List<Control> kids = [];
            MaterialIcon? likeIcon = null;
            MaterialIcon? dislikeIcon = null;
            if (shuffleRepeat)
            {
                MaterialIcon shuffleIcon = new()
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
                likeIcon = CreateLikeIcon(btnSize * 0.5);
                List<Control> ratingKids = [];
                dislikeIcon = AppendRatingButtons(vm, ratingKids, likeIcon, btnSize);
                MaterialIcon repeatIcon = new()
                {
                    Kind = MaterialIconKind.Repeat,
                    Width = btnSize * 0.5,
                    Height = btnSize * 0.5,
                    Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255))
                };
                for (int i = ratingKids.Count - 1; i >= 0; i--)
                {
                    kids.Insert(0, ratingKids[i]);
                }

                kids.Add(GlyphBtn(repeatIcon, () => _ = vm.ToggleRepeatAsync(repeatIcon), btnSize));
            }
            StackPanel sp = new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = spacing
            };
            foreach (Control c in kids)
            {
                sp.Children.Add(c);
            }

            return (sp, likeIcon, dislikeIcon);
        }

        private static MaterialIcon CreateLikeIcon(double size, IBrush? foreground = null)
        {
            return new()
            {
                Kind = MaterialIconKind.HeartOutline,
                Width = size,
                Height = size,
                Foreground = foreground ?? TesseraPalette.FontBrush
            };
        }

        private static MaterialIcon CreateDislikeIcon(double size, IBrush? foreground = null)
        {
            return new()
            {
                Kind = MaterialIconKind.ThumbDownOutline,
                Width = size,
                Height = size,
                Foreground = foreground ?? TesseraPalette.FontBrush
            };
        }

        private static MaterialIcon? AppendRatingButtons(
            TesseraFlyoutViewModel vm,
            ICollection<Control> children,
            MaterialIcon likeIcon,
            double btnSize,
            IBrush? dislikeForeground = null)
        {
            children.Add(GlyphBtn(likeIcon, () => _ = vm.ToggleLikeAsync(likeIcon), btnSize));
            if (!vm.SupportsMediaDislike)
            {
                return null;
            }

            MaterialIcon dislikeIcon = CreateDislikeIcon(likeIcon.Width, dislikeForeground ?? likeIcon.Foreground);
            children.Add(GlyphBtn(dislikeIcon, () => _ = vm.ToggleDislikeAsync(dislikeIcon), btnSize));
            return dislikeIcon;
        }

        internal static void ApplyLikeIcon(MaterialIcon icon, int? likeRating)
        {
            bool liked = MediaLikePolicy.ShouldShowLikedHeart(likeRating);
            icon.Kind = liked ? MaterialIconKind.Heart : MaterialIconKind.HeartOutline;
            icon.Foreground = liked
                ? new SolidColorBrush(Color.FromRgb(255, 80, 100))
                : TesseraPalette.FontBrush;
        }

        internal static void ApplyDislikeIcon(MaterialIcon icon, int? likeRating)
        {
            bool disliked = MediaLikePolicy.ShouldShowDislikedThumb(likeRating);
            icon.Kind = disliked ? MaterialIconKind.ThumbDown : MaterialIconKind.ThumbDownOutline;
            icon.Foreground = disliked
                ? new SolidColorBrush(Color.FromRgb(160, 180, 220))
                : TesseraPalette.FontBrush;
        }

        private static MaterialIcon PlayIcon(TesseraFlyoutViewModel vm, double size = 20)
        {
            return new()
            {
                Kind = vm.IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play,
                Width = size,
                Height = size,
                Foreground = TesseraPalette.FontBrush
            };
        }

        public static Border AlbumArt(TesseraFlyoutViewModel vm, double size)
        {
            Border border = new()
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
            if (bytes is null || bytes.Length < 32)
            {
                return null;
            }

            try
            {
                string tmp = Path.Combine(Path.GetTempPath(), $"mosaic-art-{Guid.NewGuid():N}.img");
                File.WriteAllBytes(tmp, bytes);
                try
                {
                    using FileStream fs = File.OpenRead(tmp);
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
                using MemoryStream ms = new(bytes, writable: false);
                return Bitmap.DecodeToWidth(ms, Math.Max(32, decodeWidth));
            }
            catch
            {
                try
                {
                    using MemoryStream ms = new(bytes, writable: false);
                    return new Bitmap(ms);
                }
                catch { return null; }
            }
        }

        public static void ApplyArtToBorder(Border border, byte[]? bytes, bool fillHost = false)
        {
            if (bytes is null || bytes.Length < 32)
            {
                return;
            }

            int sig = bytes.Length ^ (bytes.Length > 16 ? (bytes[8] << 8) | bytes[16] : 0);
            if (border.Tag is int prev && prev == sig && border.Child is Image)
            {
                return;
            }

            double size = !fillHost && border.Width > 1 && !double.IsNaN(border.Width)
                ? border.Width
                : fillHost ? 128 : 64;
            if (TryCreateBitmap(bytes, (int)size) is not { } bmp)
            {
                return;
            }

            border.Tag = sig;
            border.ClipToBounds = true;
            if (border.Child is Image img)
            {
                IImage? old = img.Source;
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
            MaterialIcon icon = new()
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

        private static Control GlyphBtn(MaterialIcon icon, Action act, double size = 36, bool playStyle = false)
        {
            return TesseraChrome.IconButton(icon, act, size, circularHighlight: !playStyle);
        }

        private static TextBlock Text(string text, double size, FontWeight weight, double maxWidth, bool muted = false)
        {
            return new()
            {
                Text = string.IsNullOrWhiteSpace(text) ? " " : text,
                FontSize = size,
                FontWeight = weight,
                Foreground = muted ? TesseraPalette.FontMutedBrush : TesseraPalette.FontBrush,
                FontFamily = new FontFamily("Segoe UI Variable, Segoe UI"),
                MaxWidth = maxWidth,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
        }

        private static string FormatTime(double seconds)
        {
            if (seconds <= 0 || double.IsNaN(seconds))
            {
                return "0:00";
            }

            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
        }
    }
}
