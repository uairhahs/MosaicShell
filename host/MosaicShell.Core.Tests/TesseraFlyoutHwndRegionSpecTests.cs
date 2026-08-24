using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutHwndRegionSpecTests
{
    [Fact]
    public void Phase2_animates_region_in_place_without_hwnd_resize()
    {
        TesseraFlyoutHwndRegionSpec.Phase2MustAnimateRegionHeightInPlace.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.Phase2MustNotResizeHwnd.Should().BeTrue();
        TesseraFlyoutHwndRegionSpec.RegionRectInclusivePaddingPx.Should().Be(1);
    }

    [Fact]
    public void Only_win11_with_media_needs_strokeB_region()
    {
        TesseraFlyoutHwndRegionSpec.StyleNeedsStrokeBRegion("Windows11", musicVisible: true)
            .Should().BeTrue();
        TesseraFlyoutHwndRegionSpec.StyleNeedsStrokeBRegion("Win11", musicVisible: true)
            .Should().BeTrue();
        TesseraFlyoutHwndRegionSpec.StyleNeedsStrokeBRegion("Windows11", musicVisible: false)
            .Should().BeFalse();
        TesseraFlyoutHwndRegionSpec.StyleNeedsStrokeBRegion("Fluent", musicVisible: true)
            .Should().BeFalse();
        TesseraFlyoutHwndRegionSpec.StyleNeedsStrokeBRegion("CoreUI", musicVisible: true)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(0, false, 50)]
    [InlineData(0, true, 50)]
    [InlineData(0.5, true, 137.5)]
    [InlineData(1, true, 225)]
    public void Region_height_tracks_strokeB_when_phase2_engaged(double p, bool engaged, double expected)
    {
        TesseraFlyoutHwndRegionSpec.ResolveRegionHeightDip(p, engaged, musicVisible: true)
            .Should().Be(expected);
    }

    [Fact]
    public void Region_height_stays_volume_when_phase1_even_at_rest_progress()
    {
        TesseraFlyoutHwndRegionSpec.ResolveRegionHeightDip(1, phase2Engaged: false, musicVisible: true)
            .Should().Be(TesseraStackedPlacementSpec.Win11VolumeHeightDip);
    }

    [Fact]
    public void Round_rect_physical_ceils_dips_and_clamps_radius()
    {
        var (w, h, r) = TesseraFlyoutHwndRegionSpec.ResolveRoundRectPhysical(320, 50, 12, 1);
        w.Should().Be(320);
        h.Should().Be(50);
        r.Should().Be(12);
        var scaled = TesseraFlyoutHwndRegionSpec.ResolveRoundRectPhysical(320, 50, 12, 1.5);
        scaled.WidthPx.Should().Be(480);
        scaled.HeightPx.Should().Be(75);
    }

    [Fact]
    public void Overlay_xor_must_not_paint_cover()
    {
        TesseraFlyoutAnimatedTargetSpec.Win11XorFrostMustNotPaintCoverOverlay.Should().BeTrue();
        TesseraFlyoutAnimatedTargetSpec.ResolveWin11MediaOverlayAlpha(0.5, true).Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveWin11MediaOverlayAlpha(1, true).Should().Be(0);
    }
}
