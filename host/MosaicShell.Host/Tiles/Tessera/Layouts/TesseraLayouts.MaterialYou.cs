using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using Material.Icons.Avalonia;

namespace MosaicShell.Host.Tiles.Tessera
{
    internal static partial class TesseraLayouts
    {

        public static Control MaterialYou(TesseraFlyoutViewModel vm)
        {
            if (IsStatus(vm))
            {
                return StatusChip(vm, 24);
            }

            const double colW = TesseraStyleMetrics.MaterialYouColumnW;
            const double gap = TesseraStyleMetrics.MaterialYouGap;
            const double colH = TesseraStyleMetrics.MaterialYouColH;
            const double volH = (2 * colH) + gap - colW;
            const double pillR = colW / 2.0;
            const double hit = TesseraStyleMetrics.MaterialYouHitTarget;
            const double pillPadH = (colW - hit) / 2.0;

            MaterialIcon shuffleIcon = MaterialYouIcon(MaterialIconKind.Shuffle, muted: true);
            MaterialIcon heartIcon = MaterialYouIcon(MaterialIconKind.HeartOutline, muted: true);
            MaterialIcon repeatIcon = MaterialYouIcon(MaterialIconKind.Repeat, muted: true);

            List<Control> extrasChildren =
            [
                MaterialYouIconBtn(shuffleIcon, () => _ = MaterialYouToggleShuffleAsync(vm, shuffleIcon)),
                MaterialYouIconBtn(heartIcon, () => _ = MaterialYouToggleLikeAsync(vm, heartIcon)),
            ];
            if (vm.SupportsMediaDislike)
            {
                MaterialIcon dislikeIcon = MaterialYouIcon(MaterialIconKind.ThumbDownOutline, muted: true);
                extrasChildren.Add(MaterialYouIconBtn(dislikeIcon, () => _ = MaterialYouToggleDislikeAsync(vm, dislikeIcon)));
            }
            extrasChildren.Add(MaterialYouIconBtn(repeatIcon, () => _ = MaterialYouToggleRepeatAsync(vm, repeatIcon)));

            Border transport = TesseraChrome.SolidPill(new StackPanel
            {
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    MaterialYouIconBtn(MaterialIconKind.SkipPrevious, () => _ = vm.PreviousAsync()),
                    MaterialYouPlayPill(vm),
                    MaterialYouIconBtn(MaterialIconKind.SkipNext, () => _ = vm.NextAsync())
                }
            }, TesseraStylePalette.MaterialYou.ShellBrush, pillR, colW, colH, new Thickness(pillPadH, 12));

            Border extras = TesseraChrome.SolidPill(new StackPanel
            {
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { }
            }, TesseraStylePalette.MaterialYou.ShellBrush, pillR, colW, colH, new Thickness(pillPadH, 12));
            foreach (Control child in extrasChildren)
            {
                ((StackPanel)extras.Child!).Children.Add(child);
            }

            TesseraTrack track = new()
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
                AccentBrushOverride = TesseraStylePalette.MaterialYou.AccentBrush,
                TrackBackBrushOverride = TesseraStylePalette.MaterialYou.TrackInactiveBrush
            };
            track.ValueChanged += (_, v) => vm.ApplyPrimary(v);

            TextBlock percent = new()
            {
                Text = vm.PrimaryPercent,
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Foreground = TesseraStylePalette.MaterialYou.AccentBrush,
                Name = "TesseraPercent",
                Margin = new Thickness(0, 0, 0, 10),
                IsVisible = false
            };

            MaterialIcon? glyphIcon = null;
            Control glyphControl = TesseraVolumeGlyph.Create(vm, TesseraStyleMetrics.MaterialYouTrackIconSize);
            glyphControl.Name = "TesseraGlyph";
            if (glyphControl is MaterialIcon gi)
            {
                glyphIcon = gi;
                gi.HorizontalAlignment = HorizontalAlignment.Center;
                gi.VerticalAlignment = VerticalAlignment.Bottom;
                gi.Margin = new Thickness(0, 0, 0, TesseraStyleMetrics.MaterialYouTrackIconBottom);
                TesseraMaterialYouM3.ApplyVolumeGlyphTone(gi, vm.PrimaryValue, vm.IsMuted);
            }

            TesseraLiveAmbient.RegisterVolume(track, percent, glyphIcon, pixelVolumeGlyph: true, percentOnAdjustOnly: true);

            Grid trackHost = new() { VerticalAlignment = VerticalAlignment.Stretch, ClipToBounds = true };
            trackHost.Children.Add(track);
            if (glyphIcon is not null)
            {
                trackHost.Children.Add(glyphIcon);
            }

            Grid volBody = new() { VerticalAlignment = VerticalAlignment.Stretch };
            volBody.Children.Add(trackHost);
            volBody.Children.Add(percent);

            Border volPill = TesseraChrome.SolidPill(volBody, TesseraStylePalette.MaterialYou.ShellBrush, pillR, colW, volH,
                new Thickness(0));

            Border eq = MaterialYouIconTile(MaterialIconKind.TuneVertical, TesseraStyleMetrics.MaterialYouIconSize, colW);
            eq.PointerPressed += (_, e) =>
            {
                if (vm.HostUi is not null)
                {
                    _ = vm.HostUi.OpenOverlayAsync("mixdeck");
                }

                e.Handled = true;
            };

            Control left = new StackPanel { Spacing = gap, Children = { transport, extras } };
            if (vm.HasMediaSession)
            {
                left = TesseraRevealHostFactory.WrapMediaFromCatalog(vm, left);
            }
            StackPanel right = new() { Spacing = gap, Children = { volPill, eq } };
            StackPanel row = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = gap,
                Children = { left, right }
            };
            BindWheel(volPill, vm);
            return row;
        }
    }
}
