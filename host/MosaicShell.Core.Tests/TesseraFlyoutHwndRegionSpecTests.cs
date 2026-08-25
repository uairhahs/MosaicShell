using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;

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

    [Fact]
    public void Fancy_must_clip_media_chrome_to_reveal_not_rest_hwnd()
    {
        TesseraFlyoutHwndRegionSpec.Phase2MustClipMediaChromeToRevealProgress.Should().BeTrue();
        TesseraFlyoutHwndRegionSpec.CollapsedRevealRegionMustHideRestChrome.Should().BeTrue();
        TesseraFlyoutHwndRegionSpec.StyleNeedsRevealRegion("Fluent", musicVisible: true, stackedRole: null)
            .Should().BeTrue();
        TesseraFlyoutHwndRegionSpec.StyleNeedsRevealRegion(
                "Fluent", musicVisible: true, TesseraStackedPanelRole.Media)
            .Should().BeTrue();
        TesseraFlyoutHwndRegionSpec.StyleNeedsRevealRegion(
                "Fluent", musicVisible: true, TesseraStackedPanelRole.Volume)
            .Should().BeFalse();
        TesseraFlyoutHwndRegionSpec.CollapsedShowRegionMustKeepWindowVisible.Should().BeTrue();
        TesseraFlyoutHwndRegionSpec.ShouldZeroWindowOpacityForCollapsedRegion(
                stackedMedia: true, hideChrome: true, showMotionActive: true)
            .Should().BeFalse();
        TesseraFlyoutHwndRegionSpec.ShouldZeroWindowOpacityForCollapsedRegion(
                stackedMedia: true, hideChrome: true, showMotionActive: false)
            .Should().BeTrue();
        TesseraFlyoutHwndRegionSpec.ShouldZeroWindowOpacityForCollapsedRegion(
                stackedMedia: false, hideChrome: true, showMotionActive: false)
            .Should().BeFalse();
        TesseraFlyoutHwndRegionSpec.ShowStackedMediaMustWipeRegionOverRestLayout.Should().BeFalse();
        TesseraFlyoutHwndRegionSpec.ShowLayoutMustFollowRevealProgress.Should().BeTrue();
        TesseraFlyoutHwndRegionSpec.ShowRegionWipeMustNotRedrawClient.Should().BeTrue();
        TesseraFlyoutHwndRegionSpec.ShouldWipeShowRegionOverRestLayout(
                entrance: true, TesseraStackedPanelRole.Media, willRunPhase2: true)
            .Should().BeFalse();
        TesseraFlyoutHwndRegionSpec.ShouldWipeShowRegionOverRestLayout(
                entrance: false, TesseraStackedPanelRole.Media, willRunPhase2: true)
            .Should().BeFalse();
        TesseraFlyoutHwndRegionSpec.ShouldWipeShowRegionOverRestLayout(
                entrance: true, TesseraStackedPanelRole.Volume, willRunPhase2: true)
            .Should().BeFalse();
        TesseraFlyoutHwndRegionSpec.ResolveShowLayoutRevealProgress(wipeRegionOverRest: true)
            .Should().Be(TesseraFlyoutRevealSpec.RestRevealProgress);
        TesseraFlyoutHwndRegionSpec.ResolveShowLayoutRevealProgress(wipeRegionOverRest: false)
            .Should().Be(TesseraFlyoutRevealSpec.FancyPhase2StartProgress);
    }

    [Fact]
    public void Fluent_collapsed_region_is_volume_column_not_black_media_bay()
    {
        var collapsed = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
            "Fluent",
            stackedRole: null,
            progress: 0,
            phase2Engaged: false,
            musicVisible: true,
            restWidthDip: 415,
            restHeightDip: 176);
        collapsed.WidthDip.Should().Be(
            TesseraFluentLayoutSpec.VolumeWidthDip + TesseraFluentLayoutSpec.DividerColumnWidthDip);
        collapsed.HideChrome.Should().BeFalse();

        var mediaCollapsed = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
            "Fluent",
            TesseraStackedPanelRole.Media,
            progress: 0,
            phase2Engaged: false,
            musicVisible: true,
            restWidthDip: 340,
            restHeightDip: 176);
        mediaCollapsed.HideChrome.Should().BeFalse();
        mediaCollapsed.WidthDip.Should().Be(TesseraFlyoutHwndRegionSpec.MinRenderableRegionDip);
        mediaCollapsed.HeightDip.Should().Be(176);
        TesseraFlyoutHwndRegionSpec.CollapsedMediaRegionMustStayRenderable.Should().BeTrue();
        var phys = TesseraFlyoutHwndRegionSpec.ResolveRenderableRoundRectPhysical(
            mediaCollapsed.WidthDip, mediaCollapsed.HeightDip, 10, 1);
        phys.WidthPx.Should().BeGreaterThanOrEqualTo(TesseraFlyoutHwndRegionSpec.MinRenderableRegionPx);
        phys.HeightPx.Should().Be(176);

        var open = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
            "Fluent",
            stackedRole: null,
            progress: 1,
            phase2Engaged: true,
            musicVisible: true,
            restWidthDip: 415,
            restHeightDip: 176);
        open.WidthDip.Should().Be(415);
        open.HideChrome.Should().BeFalse();
    }

    [Fact]
    public void Fluent_region_reaches_rest_width_at_progress_one()
    {
        const double restW = 430;
        const double restH = 180;
        var mid = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
            "Fluent", null, 0.5, true, true, restW, restH);
        var open = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
            "Fluent", null, 1, true, true, restW, restH);
        var mediaOpen = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
            "Fluent", TesseraStackedPanelRole.Media, 1, true, true, 360, restH);

        open.WidthDip.Should().Be(restW);
        mid.WidthDip.Should().BeApproximately(
            TesseraFlyoutAnimatedTargetSpec.ResolveFluentCollapsedShellWidthDip(true)
            + (restW - TesseraFlyoutAnimatedTargetSpec.ResolveFluentCollapsedShellWidthDip(true)) * 0.5,
            0.01);
        mediaOpen.WidthDip.Should().Be(360);
        mediaOpen.HideChrome.Should().BeFalse();
    }

    [Fact]
    public void Fluent_media_region_matches_at_time_reversed_OutQuart()
    {
        const int steps = 20;
        const double restW = 340;
        const double restH = 176;
        var ease = TesseraFlyoutAnimationPolicy.EaseOutQuart;

        for (var s = 0; s <= steps; s++)
        {
            var showP = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                0, 1, s, steps, ease, entrance: true);
            var hideP = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                1, 0, steps - s, steps, ease, entrance: false);
            var showRegion = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                "Fluent", TesseraStackedPanelRole.Media, showP, true, true, restW, restH);
            var hideRegion = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                "Fluent", TesseraStackedPanelRole.Media, hideP, true, true, restW, restH);

            showRegion.WidthDip.Should().BeApproximately(hideRegion.WidthDip, 0.5);
            showRegion.HideChrome.Should().Be(hideRegion.HideChrome);
            TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(148, showP, true)
                .Should().BeApproximately(
                    TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(148, hideP, true),
                    0.5);
            TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaContentOpacity(showP, true)
                .Should().BeApproximately(
                    TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaContentOpacity(hideP, true),
                    0.02);
        }

        var firstShowTick = TesseraFlyoutAnimationPolicy.InterpolateStepped(
            0, 1, 1, steps, ease, entrance: true);
        var firstShowRegion = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
            "Fluent", TesseraStackedPanelRole.Media, firstShowTick, true, true, restW, restH);
        firstShowRegion.HideChrome.Should().BeFalse();
        firstShowTick.Should().BeGreaterThan(0.15);
    }

    [Fact]
    public void Fluent_divider_height_is_zero_when_collapsed()
    {
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(148, 0, musicVisible: true)
            .Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerOpacity(0, musicVisible: true)
            .Should().Be(TesseraFlyoutAnimatedTargetSpec.FluentDividerRestOpacity);
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(148, 0.5, musicVisible: true)
            .Should().Be(74);
    }

    [Fact]
    public void Idle_showing_session_forces_rest_progress_not_a_stale_host()
    {
        TesseraFlyoutHwndRegionSpec.RevealRegionProgressMustUseMaxHost.Should().BeTrue();
        TesseraFlyoutHwndRegionSpec.RestRevealMustUseSignedClientRegion.Should().BeTrue();
        TesseraFlyoutHwndRegionSpec.ShouldForceRestRevealRegion(
                motionAnimating: false, phase2Animating: false, sessionShowing: true)
            .Should().BeTrue();
        TesseraFlyoutHwndRegionSpec.ShouldForceRestRevealRegion(
                motionAnimating: true, phase2Animating: false, sessionShowing: true)
            .Should().BeFalse();
        TesseraFlyoutHwndRegionSpec.ShouldForceRestRevealRegion(
                motionAnimating: false, phase2Animating: true, sessionShowing: true)
            .Should().BeFalse();
        TesseraFlyoutHwndRegionSpec.ShouldForceRestRevealRegion(
                motionAnimating: false, phase2Animating: false, sessionShowing: false)
            .Should().BeFalse();

        TesseraFlyoutHwndRegionSpec.ResolveRegionProgress(
                [0.2, 0],
                motionAnimating: false,
                phase2Animating: false,
                sessionShowing: true)
            .Should().Be(TesseraFlyoutRevealSpec.RestRevealProgress);
    }

    [Fact]
    public void Motion_uses_max_host_progress_not_first()
    {
        TesseraFlyoutHwndRegionSpec.ResolveRegionProgress(
                [0, 0.8],
                motionAnimating: true,
                phase2Animating: true,
                sessionShowing: true)
            .Should().BeApproximately(0.8, 0.001);
        TesseraFlyoutHwndRegionSpec.ResolveRegionProgress(
                [0.15],
                motionAnimating: true,
                phase2Animating: false,
                sessionShowing: true)
            .Should().BeApproximately(0.15, 0.001);
        TesseraFlyoutHwndRegionSpec.ResolveRegionProgress(
                [],
                motionAnimating: true,
                phase2Animating: false,
                sessionShowing: false)
            .Should().Be(TesseraFlyoutRevealSpec.FancyPhase2StartProgress);
    }

    [Fact]
    public void Signed_rest_must_not_shrink_to_mid_tween_bounds()
    {
        const double signedRest = 340;
        const double restH = 176;
        var mid = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
            "Fluent",
            TesseraStackedPanelRole.Media,
            0.2,
            phase2Engaged: true,
            musicVisible: true,
            restWidthDip: signedRest,
            restHeightDip: restH);
        mid.WidthDip.Should().BeApproximately(signedRest * 0.2, 0.5);
        mid.WidthDip.Should().BeLessThan(100);

        var frozen = TesseraFlyoutHwndRegionSpec.ResolveRestExtentDip(
            signedRest, liveBoundsDip: mid.WidthDip);
        frozen.Should().Be(signedRest);

        var open = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
            "Fluent",
            TesseraStackedPanelRole.Media,
            TesseraFlyoutHwndRegionSpec.ResolveRegionProgress(
                [0.2],
                motionAnimating: false,
                phase2Animating: false,
                sessionShowing: true),
            phase2Engaged: true,
            musicVisible: true,
            restWidthDip: frozen,
            restHeightDip: restH);
        open.WidthDip.Should().Be(signedRest);
        open.HideChrome.Should().BeFalse();
    }

    [Fact]
    public void Rest_progress_never_hides_fluent_media_chrome()
    {
        TesseraFlyoutHwndRegionSpec.IsRestRevealProgress(1).Should().BeTrue();
        TesseraFlyoutHwndRegionSpec.IsRestRevealProgress(0.2).Should().BeFalse();
        var rest = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
            "Fluent",
            TesseraStackedPanelRole.Media,
            TesseraFlyoutRevealSpec.RestRevealProgress,
            phase2Engaged: true,
            musicVisible: true,
            restWidthDip: 340,
            restHeightDip: 176);
        rest.HideChrome.Should().BeFalse();
        rest.WidthDip.Should().Be(340);
    }

    [Fact]
    public void Fallback_rest_sizes_read_style_profile()
    {
        var meterVol = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
            StyleIds.Meter,
            TesseraStackedPanelRole.Volume,
            progress: 1,
            phase2Engaged: true,
            musicVisible: true,
            restWidthDip: 0,
            restHeightDip: 0);
        var profile = TesseraFlyoutTweenTargetCatalog.ResolveProfile(StyleIds.Meter);
        meterVol.WidthDip.Should().Be(profile.VolumeWidthDip);
        meterVol.HeightDip.Should().Be(profile.VolumeHeightDip);
    }

    [Fact]
    public void CoreUi_single_hwnd_keeps_rest_region_while_media_clip_animates()
    {
        TesseraFlyoutHwndRegionSpec.CoreUiSingleHwndMustKeepRestRegion.Should().BeTrue();
        const double restW = TesseraCoreUiLayoutSpec.WidthDip;
        const double restH = 226;
        foreach (var p in new[] { 0.0, 0.35, 1.0 })
        {
            var region = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                StyleIds.CoreUI,
                stackedRole: null,
                progress: p,
                phase2Engaged: true,
                musicVisible: true,
                restWidthDip: restW,
                restHeightDip: restH);
            region.HideChrome.Should().BeFalse($"p={p}");
            region.WidthDip.Should().Be(restW, $"p={p}");
            region.HeightDip.Should().Be(restH, $"p={p}");
            TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaClipWidthDip(
                    TesseraCoreUiLayoutSpec.InnerRowWidthDip, p, true)
                .Should().BeApproximately(TesseraCoreUiLayoutSpec.InnerRowWidthDip * p, 0.01);
        }
    }

    [Fact]
    public void CoreUi_media_slot_still_clips_hwnd_width()
    {
        var collapsed = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
            StyleIds.CoreUI,
            TesseraStackedPanelRole.Media,
            progress: 0,
            phase2Engaged: true,
            musicVisible: true,
            restWidthDip: TesseraCoreUiLayoutSpec.InnerRowWidthDip,
            restHeightDip: TesseraCoreUiLayoutSpec.MediaHeightDip);
        collapsed.HideChrome.Should().BeFalse();
        collapsed.WidthDip.Should().Be(TesseraFlyoutHwndRegionSpec.MinRenderableRegionDip);
        collapsed.HeightDip.Should().Be(TesseraCoreUiLayoutSpec.MediaHeightDip);

        var open = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
            StyleIds.CoreUI,
            TesseraStackedPanelRole.Media,
            progress: 1,
            phase2Engaged: true,
            musicVisible: true,
            restWidthDip: TesseraCoreUiLayoutSpec.InnerRowWidthDip,
            restHeightDip: TesseraCoreUiLayoutSpec.MediaHeightDip);
        open.HideChrome.Should().BeFalse();
        open.WidthDip.Should().Be(TesseraCoreUiLayoutSpec.InnerRowWidthDip);
    }

    [Fact]
    public void CoreUi_phase2_clip_and_scale_follow_the_same_progress()
    {
        const int steps = 20;
        var showEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
            TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
        var hideEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
            TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false);
        var row = TesseraCoreUiLayoutSpec.InnerRowWidthDip;
        var showMid = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
            entrance: true, 10, steps, showEase);
        var hideEarly = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
            entrance: false, 5, steps, hideEase);
        var showClip = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaClipWidthDip(row, showMid, true);
        var hideClip = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaClipWidthDip(row, hideEarly, true);
        var showScale = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiVolumeBarScaleFactor(showMid, true);
        showEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseInOutQuart);
        hideEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseOutQuart);
        showMid.Should().BeApproximately(0.5, 0.05);
        showClip.Should().BeApproximately(row * showMid, 0.5);
        showScale.Should().BeApproximately(showMid, 0.02);
        hideClip.Should().BeGreaterThan(row * 0.9);
        hideEarly.Should().BeGreaterThan(0.9);
    }
}
