using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Every style's stacked cluster, computed from measured panel sizes, must be a sane layout: both panels present at
    /// the measured size, on screen, and not on top of each other. The layout kind says which panel leads and along which
    /// axis, so each kind is checked against that rather than against a table of pixel offsets.
    /// </summary>
    public class TesseraStackedPlacementLayoutTests
    {
        private const double VolW = 280;
        private const double VolH = 70;
        private const double MediaW = 340;
        private const double MediaH = 120;

        private static readonly string[] AllStyles =
        [
            StyleIds.MaterialYou, StyleIds.Fluent, StyleIds.Windows11, StyleIds.Gnome, StyleIds.CoreUI,
            StyleIds.ModernFlyouts, StyleIds.Meter, StyleIds.Square, StyleIds.Compact, StyleIds.PlainText, StyleIds.Radial,
        ];

        public static TheoryData<string> Styles()
        {
            TheoryData<string> data = [];
            foreach (string style in AllStyles)
            {
                data.Add(style);
            }

            return data;
        }

        private static (TesseraStackedPanelPlacement Volume, TesseraStackedPanelPlacement Media) Place(
            string style, double volW = VolW, double volH = VolH, double mediaW = MediaW, double mediaH = MediaH)
        {
            IReadOnlyList<TesseraStackedPanelPlacement> placements =
                TesseraStackedPlacementPolicy.ComputePlacementsFromMeasured(style, volW, volH, mediaW, mediaH);
            placements.Should().HaveCount(2);
            return (
                placements.Single(p => p.Role == TesseraStackedPanelRole.Volume),
                placements.Single(p => p.Role == TesseraStackedPanelRole.Media));
        }

        private static bool Overlaps(TesseraStackedPanelPlacement a, TesseraStackedPanelPlacement b)
        {
            return a.OffsetXDip < b.OffsetXDip + b.WidthDip && b.OffsetXDip < a.OffsetXDip + a.WidthDip
                && a.OffsetYDip < b.OffsetYDip + b.HeightDip && b.OffsetYDip < a.OffsetYDip + a.HeightDip;
        }

        [Theory]
        [MemberData(nameof(Styles))]
        public void Both_panels_keep_their_measured_size_sit_on_screen_and_do_not_overlap(string style)
        {
            (TesseraStackedPanelPlacement volume, TesseraStackedPanelPlacement media) = Place(style);

            (volume.WidthDip, volume.HeightDip).Should().Be((VolW, VolH));
            (media.WidthDip, media.HeightDip).Should().Be((MediaW, MediaH));
            new[] { volume.OffsetXDip, volume.OffsetYDip, media.OffsetXDip, media.OffsetYDip }.Should().OnlyContain(v => v >= 0);
            Overlaps(volume, media).Should().BeFalse($"{style} must not park the volume panel under the media card");
        }

        [Theory]
        [MemberData(nameof(Styles))]
        public void The_leading_panel_comes_first_along_the_layouts_axis(string style)
        {
            (TesseraStackedPanelPlacement volume, TesseraStackedPanelPlacement media) = Place(style);
            TesseraStackedLayoutKind kind = TesseraStackedPlacementPolicy.ResolveLayoutKind(style);

            switch (kind)
            {
                case TesseraStackedLayoutKind.HorizontalVolumeFirst:
                case TesseraStackedLayoutKind.HorizontalRadial:
                    media.OffsetXDip.Should().BeGreaterThanOrEqualTo(volume.OffsetXDip + volume.WidthDip);
                    break;
                case TesseraStackedLayoutKind.HorizontalMediaFirst:
                    volume.OffsetXDip.Should().BeGreaterThanOrEqualTo(media.OffsetXDip + media.WidthDip);
                    break;
                case TesseraStackedLayoutKind.VerticalVolumeFirst:
                case TesseraStackedLayoutKind.VerticalWin11:
                case TesseraStackedLayoutKind.PlainTextColumn:
                    volume.OffsetYDip.Should().Be(0);
                    media.OffsetYDip.Should().BeGreaterThanOrEqualTo(volume.HeightDip);
                    break;
                case TesseraStackedLayoutKind.VerticalMediaFirst:
                    media.OffsetYDip.Should().Be(0);
                    volume.OffsetYDip.Should().BeGreaterThanOrEqualTo(media.HeightDip);
                    break;
                default:
                    Assert.Fail($"{style} resolves to {kind}, which this test does not know how to check");
                    break;
            }
        }

        [Theory]
        [MemberData(nameof(Styles))]
        public void Panels_of_different_size_are_aligned_within_the_cluster_not_pushed_off_it(string style)
        {
            (TesseraStackedPanelPlacement volume, TesseraStackedPanelPlacement media) = Place(style);

            double clusterW = Math.Max(volume.OffsetXDip + volume.WidthDip, media.OffsetXDip + media.WidthDip);
            double clusterH = Math.Max(volume.OffsetYDip + volume.HeightDip, media.OffsetYDip + media.HeightDip);

            clusterW.Should().BeGreaterThanOrEqualTo(Math.Max(VolW, MediaW));
            clusterH.Should().BeGreaterThanOrEqualTo(Math.Max(VolH, MediaH));
        }

        [Theory]
        [MemberData(nameof(Styles))]
        public void A_panel_that_has_not_been_measured_yet_is_treated_as_one_dip_not_zero_or_negative(string style)
        {
            (TesseraStackedPanelPlacement volume, TesseraStackedPanelPlacement media) = Place(style, volW: 0, volH: -5, mediaW: 0, mediaH: 0);

            (volume.WidthDip, volume.HeightDip).Should().Be((1, 1));
            (media.WidthDip, media.HeightDip).Should().Be((1, 1));
            Overlaps(volume, media).Should().BeFalse();
        }

        [Fact]
        public void A_horizontal_row_centres_the_shorter_panel_on_the_taller_one()
        {
            string style = AllStyles.First(s =>
                TesseraStackedPlacementPolicy.ResolveLayoutKind(s) == TesseraStackedLayoutKind.HorizontalVolumeFirst);

            (TesseraStackedPanelPlacement volume, TesseraStackedPanelPlacement media) = Place(style);

            volume.OffsetYDip.Should().Be((MediaH - VolH) / 2);
            media.OffsetYDip.Should().Be(0);
        }

        [Fact]
        public void Meter_places_media_then_volume_aligned_to_the_top_with_the_meter_gap()
        {
            (TesseraStackedPanelPlacement volume, TesseraStackedPanelPlacement media) = Place(StyleIds.Meter);

            media.OffsetXDip.Should().Be(0);
            volume.OffsetXDip.Should().Be(MediaW + TesseraStackedPlacementSpec.MeterGapDip);
            (media.OffsetYDip, volume.OffsetYDip).Should().Be((0, 0), "Meter aligns its panels to the top, not the middle");
        }

        [Fact]
        public void Radial_never_lets_the_cluster_get_narrower_than_its_minimum()
        {
            (TesseraStackedPanelPlacement volume, TesseraStackedPanelPlacement media) = Place(StyleIds.Radial, volW: 40, mediaW: 40);

            (media.OffsetXDip + media.WidthDip).Should().BeGreaterThanOrEqualTo(TesseraStackedPlacementSpec.RadialClusterWidthDip);
            media.OffsetXDip.Should().BeGreaterThanOrEqualTo(volume.OffsetXDip + volume.WidthDip + TesseraStackedPlacementSpec.RadialColumnGapDip - 0.001
                , "a wide-enough cluster still keeps the column gap");
        }
    }
}
