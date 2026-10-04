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

        [Fact]
        public void Radius_scales_with_flyout_and_monitor_scale()
        {
            _ = TesseraFlyoutHwndRegionSpec.ResolveCornerRadiusPx(24, contentScale: 1.25, monitorScale: 1).Should().Be(30);
            _ = TesseraFlyoutHwndRegionSpec.ResolveCornerRadiusPx(24, contentScale: 1.25, monitorScale: 1.5).Should().Be(45);
            _ = TesseraFlyoutHwndRegionSpec.ResolveCornerRadiusPx(8, contentScale: 1, monitorScale: 1).Should().Be(8);
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
}
