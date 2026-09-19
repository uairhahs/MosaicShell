using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;
using MosaicShell.Core.Styles;

namespace MosaicShell.Host.Tiles.Tessera
{
    internal static partial class TesseraLayouts
    {

        public static Control PlainText(TesseraFlyoutViewModel vm)
        {
            if (IsStatus(vm))
            {
                return StatusChip(vm, 4);
            }

            Control? stacked = TesseraStackedBuildContext.TryCreatePanel(
                vm,
                buildVolume: PlainTextVolumeCore,
                buildMedia: PlainTextMediaCore,
                wrapVolume: PlainTextVolumeWrap,
                wrapMedia: PlainTextMediaWrap);
            if (stacked is not null)
            {
                return stacked;
            }

            List<Control> kids = PlainTextVolumeChildren(vm);
            if (vm.ShowMediaStrip)
            {
                List<Control> mediaKids = PlainTextMediaChildren(vm);
                StackPanel mediaPanel = new() { Spacing = 4 };
                foreach (Control c in mediaKids)
                {
                    mediaPanel.Children.Add(c);
                }

                kids.Add(TesseraRevealHostFactory.WrapMediaFromCatalog(vm, mediaPanel));
            }

            StackPanel panel = new() { Spacing = 4 };
            foreach (Control c in kids)
            {
                panel.Children.Add(c);
            }

            return PlainTextShell(panel);
        }

        private static Control PlainTextVolumeCore(TesseraFlyoutViewModel vm)
        {
            StackPanel panel = new() { Spacing = 4 };
            foreach (Control c in PlainTextVolumeChildren(vm))
            {
                panel.Children.Add(c);
            }

            BindWheel(panel, vm);
            return panel;
        }

        private static Control PlainTextMediaCore(TesseraFlyoutViewModel vm)
        {
            StackPanel panel = new() { Spacing = 4 };
            foreach (Control c in PlainTextMediaChildren(vm))
            {
                panel.Children.Add(c);
            }

            return panel;
        }

        private static List<Control> PlainTextVolumeChildren(TesseraFlyoutViewModel vm)
        {
            int pct = VolumePercent.ToPercent(vm.PrimaryValue);
            TextBlock header = TesseraChrome.Mono($"Speakers: {pct}%", 14);
            header.Name = "TesseraPercent";
            TextBlock slash = TesseraChrome.Mono(TesseraChrome.SlashFill(vm.PrimaryValue), 14);
            TesseraLiveAmbient.RegisterSlash(slash);
            TesseraTrack track = new()
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
            string state = vm.IsPlaying ? "Playing" : "Paused";
            TextBlock title = TesseraChrome.Mono($"{vm.MediaTitle} > {state} <", 13);
            TextBlock artist = TesseraChrome.Mono(vm.MediaArtist, 12, muted: true);
            TextBlock prog = TesseraChrome.Mono(
                $"{FormatTime(vm.MediaPositionSeconds)} {TesseraChrome.SlashFill(vm.MediaProgress, 16)} {FormatTime(vm.MediaDurationSeconds)}",
                12);
            TesseraLiveAmbient.RegisterPlainTextMedia(title, artist, prog);
            // Narrower than the card's own top width (340, minus 32 of content padding) on
            // purpose: the card's right edge is a slant, not a straight edge, and this row's
            // actual clearance depends on its vertical position within it. The extra margin here
            // is the padding against that slant the title needs regardless of exactly where it lands.
            return
            [
                TesseraMarqueeText.Wrap(title, 260, vm.AutoDismissMs),
                artist,
                prog,
                TesseraChrome.Mono("Media playing | Heart: 0 Shuffle: 0 Repeat: 0", 10, muted: true)
            ];
        }

        private static Control PlainTextVolumeWrap(Control panel)
        {
            return PlainTextShell(panel, TesseraStackedBuildContext.Role == TesseraStackedPanelRole.Volume);
        }

        private static Control PlainTextMediaWrap(Control panel)
        {
            return PlainTextShell(panel, TesseraStackedBuildContext.Role == TesseraStackedPanelRole.Media);
        }

        private static Control PlainTextShell(Control panel, bool stackedSegment = false)
        {
            // Stacked OS acrylic already supplies the window's backing material for the styles
            // that actually run under H3 multi-window acrylic (Gnome/Compact/ModernFlyouts/Meter
            // skip theirs the same way) - a second flat background there doubles up on it instead
            // of the intended bare card. PlainText is not one of those styles
            // (TesseraStackedPlacementPolicy.SupportsStackedOsAcrylic is false for it), so
            // UseOsAcrylicChrome being true elsewhere in the process must not make this shell skip
            // its own background - nothing else would be painting one behind it, leaving a
            // transparent hole. The explicit eligibility check is what makes that always false.
            if (stackedSegment
                && TesseraGlass.UseOsAcrylicChrome
                && TesseraStackedPlacementPolicy.SupportsStackedOsAcrylic(StyleIds.PlainText))
            {
                return panel;
            }

            Grid shell = new()
            {
                Width = TesseraStackedPlacementSpec.PlainTextWidthDip,
                MinHeight = stackedSegment ? 0 : 80
            };

            // The card is a slanted quadrilateral, not a rectangle. Stroking a rectangular
            // Border and then Clip-ing the result to this shape drew the accent outline around
            // the rectangle first and cropped it afterward, so the diagonal edge had no stroke at
            // all - it just looked cut off. The fill is the full closed silhouette; the outline is
            // a separate, deliberately open path covering only top/left/bottom - the right slant
            // is a bare cut edge by design and was never meant to carry a stroke.
            PathFigures cardFillFigures =
            [
                new PathFigure
                {
                    StartPoint = new Point(0, 0),
                    IsClosed = true,
                    Segments =
                    [
                        new LineSegment { Point = new Point(340, 0) },
                        new LineSegment { Point = new Point(320, 200) },
                        new LineSegment { Point = new Point(0, 200) }
                    ]
                }
            ];
            Avalonia.Controls.Shapes.Path bg = new()
            {
                Data = new PathGeometry { Figures = cardFillFigures },
                Fill = new SolidColorBrush(Color.FromArgb(200, 0x11, 0x11, 0x1b))
            };
            PathFigures outlineFigures =
            [
                new PathFigure
                {
                    StartPoint = new Point(340, 0),
                    IsClosed = false,
                    Segments =
                    [
                        new LineSegment { Point = new Point(0, 0) },
                        new LineSegment { Point = new Point(0, 200) },
                        new LineSegment { Point = new Point(320, 200) }
                    ]
                }
            ];
            Avalonia.Controls.Shapes.Path outline = new()
            {
                Data = new PathGeometry { Figures = outlineFigures },
                Stroke = TesseraPalette.AccentBrush,
                StrokeThickness = 1,
                StrokeJoin = PenLineJoin.Miter
            };
            shell.Children.Add(bg);
            shell.Children.Add(outline);
            shell.Children.Add(new Border
            {
                Padding = new Thickness(16, 12),
                // Content must stay inside the same silhouette as the fill, or text near the
                // bottom (where the slant has already narrowed the card) renders past the visible
                // edge into the transparent gap instead of being cropped to it. A fresh geometry
                // instance, not the fill's own Data, so the two Path elements each own theirs.
                Clip = new PathGeometry
                {
                    Figures =
                    [
                        new PathFigure
                        {
                            StartPoint = new Point(0, 0),
                            IsClosed = true,
                            Segments =
                            [
                                new LineSegment { Point = new Point(340, 0) },
                                new LineSegment { Point = new Point(320, 200) },
                                new LineSegment { Point = new Point(0, 200) }
                            ]
                        }
                    ]
                },
                Child = panel
            });
            return shell;
        }
    }
}
