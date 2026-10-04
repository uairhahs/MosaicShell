using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Desktop report 2026-10-04 (flyout scale 125%, OS acrylic on): Windows 11 and Modern Flyouts lost
    /// the transport row at the bottom, Square showed translucent acrylic outside its rounded corners,
    /// and CoreUI's corners showed a faint outline. The region trace explained all of it: the HWND
    /// region mixed measured widths (already scaled by the flyout scale) with unscaled style heights
    /// (Windows 11: 400x245 region on a 400x307 window; Modern media: 400x190 on 400x238), and every
    /// single-shell window used a fixed 12 dip radius, also unscaled, whatever the card's own radius.
    /// </summary>
    public class TesseraWindowRegionScaleTests
    {
        private const double Scale = 1.25;

        [Theory]
        [InlineData(StyleIds.Square, TesseraStackedPlacementSpec.SquareCornerRadiusDip)]
        [InlineData(StyleIds.CoreUI, TesseraStackedPlacementSpec.CoreUiShellCornerRadiusDip)]
        [InlineData(StyleIds.Radial, TesseraStackedPlacementSpec.RadialShellCornerRadiusDip)]
        [InlineData(StyleIds.PlainText, TesseraStackedPlacementSpec.PlainTextShellCornerRadiusDip)]
        [InlineData(StyleIds.Windows11, TesseraStackedPlacementSpec.Win11CornerRadiusDip)]
        [InlineData(StyleIds.Fluent, 12)]
        [InlineData(StyleIds.ModernFlyouts, 12)]
        [InlineData(StyleIds.Compact, 12)]
        public void Single_shell_region_radius_is_the_cards_own_radius(string styleId, double expected)
        {
            _ = TesseraFlyoutHwndRegionSpec.ResolveSingleShellCornerRadiusDip(styleId).Should().Be(expected);
        }

        [Fact]
        public void Shell_radii_match_the_layout_constants()
        {
            _ = TesseraStackedPlacementSpec.SquareCornerRadiusDip.Should().Be(24);
            _ = TesseraStackedPlacementSpec.CoreUiShellCornerRadiusDip.Should().Be(8);
            _ = TesseraStackedPlacementSpec.RadialShellCornerRadiusDip.Should().Be(10);
            _ = TesseraStackedPlacementSpec.PlainTextShellCornerRadiusDip.Should().Be(0, "the slanted card has square corners; a rounded region cut its outline");
        }

        [Fact]
        public void Windows11_region_at_rest_covers_the_scaled_volume_and_media()
        {
            double layoutH = TesseraStackedPlacementSpec.Win11VolumeHeightDip + TesseraStackedPlacementSpec.Win11MediaHeightDip;
            double windowW = TesseraStackedPlacementSpec.Win11WidthDip * Scale;

            (int w, int h, int r) = TesseraFlyoutHwndRegionSpec.ResolveWindowRegionPhysical(
                StyleIds.Windows11, stackedRole: null, progress: 1, phase2Engaged: true, musicVisible: true,
                restWidthWindowDip: windowW, restHeightWindowDip: layoutH * Scale,
                cornerRadiusDip: TesseraStackedPlacementSpec.Win11CornerRadiusDip, contentScale: Scale, monitorScale: 1);

            _ = h.Should().BeGreaterThanOrEqualTo((int)Math.Floor(layoutH * Scale), "the region must not cut off the transport row");
            _ = w.Should().BeGreaterThanOrEqualTo((int)Math.Floor(windowW));
            _ = r.Should().Be((int)Math.Round(TesseraStackedPlacementSpec.Win11CornerRadiusDip * Scale));
        }

        [Fact]
        public void Modern_stacked_media_region_at_rest_covers_the_scaled_card()
        {
            double windowW = TesseraStackedPlacementSpec.ModernFlyoutsMediaWidthDip * Scale;
            double windowH = TesseraStackedPlacementSpec.ModernFlyoutsMediaHeightDip * Scale;

            (int _, int h, int _) = TesseraFlyoutHwndRegionSpec.ResolveWindowRegionPhysical(
                StyleIds.ModernFlyouts, TesseraStackedPanelRole.Media, progress: 1, phase2Engaged: true, musicVisible: true,
                restWidthWindowDip: windowW, restHeightWindowDip: windowH,
                cornerRadiusDip: 12, contentScale: Scale, monitorScale: 1);

            _ = h.Should().BeGreaterThanOrEqualTo((int)Math.Floor(windowH));
        }

        [Theory]
        [InlineData(StyleIds.ModernFlyouts, TesseraStackedPanelRole.Media)]
        [InlineData(StyleIds.Fluent, TesseraStackedPanelRole.Media)]
        public void A_fully_collapsed_media_reveal_is_an_empty_region_not_a_sliver(
            string styleId, TesseraStackedPanelRole role)
        {
            // Modern Flyouts exit, 2026-10-04 (run frames: rgn 400x3 for the whole phase 1 fade):
            // the collapsed media region was floored at a 2 px renderable strip, so a thin acrylic bar
            // stayed under the volume card until the flyout hid (and before phase 2 on entrance).
            _ = TesseraFlyoutHwndRegionSpec.IsCollapsedWindowRegion(
                    styleId, role, progress: 0, phase2Engaged: true, musicVisible: true,
                    restWidthWindowDip: 400, restHeightWindowDip: 238, contentScale: Scale)
                .Should().BeTrue();
        }

        [Theory]
        [InlineData(0.1)]
        [InlineData(1.0)]
        public void A_partly_or_fully_revealed_media_region_is_not_collapsed(double progress)
        {
            _ = TesseraFlyoutHwndRegionSpec.IsCollapsedWindowRegion(
                    StyleIds.ModernFlyouts, TesseraStackedPanelRole.Media, progress, phase2Engaged: true, musicVisible: true,
                    restWidthWindowDip: 400, restHeightWindowDip: 238, contentScale: Scale)
                .Should().BeFalse();
        }

        [Fact]
        public void The_volume_panel_and_a_single_shell_never_collapse()
        {
            _ = TesseraFlyoutHwndRegionSpec.IsCollapsedWindowRegion(
                    StyleIds.ModernFlyouts, TesseraStackedPanelRole.Volume, progress: 0, phase2Engaged: true, musicVisible: true,
                    restWidthWindowDip: 400, restHeightWindowDip: 65, contentScale: Scale)
                .Should().BeFalse();
            _ = TesseraFlyoutHwndRegionSpec.IsCollapsedWindowRegion(
                    StyleIds.Windows11, stackedRole: null, progress: 0, phase2Engaged: true, musicVisible: true,
                    restWidthWindowDip: 400, restHeightWindowDip: 307, contentScale: Scale)
                .Should().BeFalse();
        }

        [Fact]
        public void Radius_scales_with_flyout_and_monitor_scale()
        {
            _ = TesseraFlyoutHwndRegionSpec.ResolveCornerRadiusPx(24, contentScale: 1.25, monitorScale: 1).Should().Be(30);
            _ = TesseraFlyoutHwndRegionSpec.ResolveCornerRadiusPx(24, contentScale: 1.25, monitorScale: 1.5).Should().Be(45);
            _ = TesseraFlyoutHwndRegionSpec.ResolveCornerRadiusPx(8, contentScale: 1, monitorScale: 1).Should().Be(8);
            _ = TesseraFlyoutHwndRegionSpec.ResolveCornerRadiusPx(0, contentScale: 1.25, monitorScale: 1).Should().BeLessThanOrEqualTo(1, "square corners stay square");
        }

        [Fact]
        public void Square_corners_produce_a_square_region()
        {
            (int _, int _, int r) = TesseraFlyoutHwndRegionSpec.ResolveWindowRegionPhysical(
                StyleIds.PlainText, stackedRole: null, progress: 1, phase2Engaged: true, musicVisible: true,
                restWidthWindowDip: 450, restHeightWindowDip: 250,
                cornerRadiusDip: TesseraFlyoutHwndRegionSpec.ResolveSingleShellCornerRadiusDip(StyleIds.PlainText),
                contentScale: Scale, monitorScale: 1);

            _ = r.Should().BeLessThanOrEqualTo(1);
        }

        [Theory]
        [InlineData(StyleIds.Fluent, null)]
        [InlineData(StyleIds.Windows11, null)]
        [InlineData(StyleIds.ModernFlyouts, TesseraStackedPanelRole.Media)]
        [InlineData(StyleIds.CoreUI, null)]
        public void At_100_percent_the_region_is_unchanged(string styleId, TesseraStackedPanelRole? role)
        {
            TesseraFlyoutHwndRegionSpec.RevealRegionDip before = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                styleId, role, 0.6, true, true, 320, 240);
            (int w0, int h0, int r0) = TesseraFlyoutHwndRegionSpec.ResolveRenderableRoundRectPhysical(before.WidthDip, before.HeightDip, 12, 1.25);

            (int w, int h, int r) = TesseraFlyoutHwndRegionSpec.ResolveWindowRegionPhysical(
                styleId, role, 0.6, true, true, 320, 240, 12, contentScale: 1, monitorScale: 1.25);

            _ = (w, h, r).Should().Be((w0, h0, r0));
        }
    }

    /// <summary>
    /// Two writers set one HWND region: the layout clip (card at rest) and the reveal (collapsed
    /// card growing in phase 2). Modern Flyouts cold summon, 2026-10-04: the layout clip was posted,
    /// so on the media panel it landed between reveal writes (the "jiggle"), and on the volume panel
    /// it landed after the first rendered frame, which showed a square acrylic pane (motion frame
    /// trace, run ff725208: <c>rgn=none</c> at t=47). One owner per window: where the reveal owns
    /// the region the layout clip writes nothing, and elsewhere it writes synchronously.
    /// </summary>
    public class TesseraBackdropClipWriteTests
    {
        [Fact]
        public void The_layout_clip_writes_nothing_where_the_reveal_owns_the_region()
        {
            _ = TesseraFlyoutHwndRegionSpec.ResolveBackdropClipWrite(
                    StyleIds.ModernFlyouts, musicVisible: true, TesseraStackedPanelRole.Media, statusKind: false)
                .Should().Be(TesseraBackdropClipWrite.None);
            _ = TesseraFlyoutHwndRegionSpec.ResolveBackdropClipWrite(
                    StyleIds.Windows11, musicVisible: true, stackedRole: null, statusKind: false)
                .Should().Be(TesseraBackdropClipWrite.None);
        }

        [Fact]
        public void A_panel_without_a_reveal_gets_its_clip_before_the_first_frame()
        {
            _ = TesseraFlyoutHwndRegionSpec.ResolveBackdropClipWrite(
                    StyleIds.ModernFlyouts, musicVisible: true, TesseraStackedPanelRole.Volume, statusKind: false)
                .Should().Be(TesseraBackdropClipWrite.Synchronous);
            _ = TesseraFlyoutHwndRegionSpec.ResolveBackdropClipWrite(
                    StyleIds.Fluent, musicVisible: false, stackedRole: null, statusKind: true)
                .Should().Be(TesseraBackdropClipWrite.Synchronous);
        }

        [Theory]
        [InlineData(StyleIds.Fluent)]
        [InlineData(StyleIds.ModernFlyouts)]
        [InlineData(StyleIds.Windows11)]
        [InlineData(StyleIds.Square)]
        [InlineData(StyleIds.MaterialYou)]
        public void Exactly_one_writer_owns_each_window_region(string styleId)
        {
            foreach (bool music in new[] { false, true })
            {
                foreach (TesseraStackedPanelRole? role in new TesseraStackedPanelRole?[] { null, TesseraStackedPanelRole.Volume, TesseraStackedPanelRole.Media })
                {
                    bool revealOwns = TesseraFlyoutHwndRegionSpec.RevealOwnsWindowRegion(styleId, music, role);
                    bool clipWrites = TesseraFlyoutHwndRegionSpec.ResolveBackdropClipWrite(styleId, music, role, statusKind: false)
                        != TesseraBackdropClipWrite.None;
                    _ = (revealOwns ^ clipWrites).Should().BeTrue($"{styleId} music={music} role={role}");
                }
            }
        }
    }
}
