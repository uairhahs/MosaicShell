using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutHwndRegionSpecTests
    {
        [Fact]
        public void Phase2_animates_region_in_place_without_hwnd_resize()
        {
            _ = TesseraFlyoutHwndRegionSpec.Phase2MustAnimateRegionHeightInPlace.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.Phase2MustNotResizeHwnd.Should().BeTrue();
            _ = TesseraFlyoutHwndRegionSpec.RegionRectInclusivePaddingPx.Should().Be(1);
        }

        [Fact]
        public void Only_win11_with_media_needs_strokeB_region()
        {
            _ = TesseraFlyoutHwndRegionSpec.StyleNeedsStrokeBRegion("Windows11", musicVisible: true)
                .Should().BeTrue();
            _ = TesseraFlyoutHwndRegionSpec.StyleNeedsStrokeBRegion("Win11", musicVisible: true)
                .Should().BeTrue();
            _ = TesseraFlyoutHwndRegionSpec.StyleNeedsStrokeBRegion("Windows11", musicVisible: false)
                .Should().BeFalse();
            _ = TesseraFlyoutHwndRegionSpec.StyleNeedsStrokeBRegion("Fluent", musicVisible: true)
                .Should().BeFalse();
            _ = TesseraFlyoutHwndRegionSpec.StyleNeedsStrokeBRegion("CoreUI", musicVisible: true)
                .Should().BeFalse();
        }

        [Theory]
        [InlineData(0, false, 50)]
        [InlineData(0, true, 50)]
        [InlineData(0.5, true, 137.5)]
        [InlineData(1, true, 225)]
        public void Region_height_tracks_strokeB_when_phase2_engaged(double p, bool engaged, double expected)
        {
            _ = TesseraFlyoutHwndRegionSpec.ResolveRegionHeightDip(p, engaged, musicVisible: true)
                .Should().Be(expected);
        }

        [Fact]
        public void Region_height_stays_volume_when_phase1_even_at_rest_progress()
        {
            _ = TesseraFlyoutHwndRegionSpec.ResolveRegionHeightDip(1, phase2Engaged: false, musicVisible: true)
                .Should().Be(TesseraStackedPlacementSpec.Win11VolumeHeightDip);
        }

        [Fact]
        public void Round_rect_physical_ceils_dips_and_clamps_radius()
        {
            (int w, int h, int r) = TesseraFlyoutHwndRegionSpec.ResolveRoundRectPhysical(320, 50, 12, 1);
            _ = w.Should().Be(320);
            _ = h.Should().Be(50);
            _ = r.Should().Be(12);
            (int WidthPx, int HeightPx, _) = TesseraFlyoutHwndRegionSpec.ResolveRoundRectPhysical(320, 50, 12, 1.5);
            _ = WidthPx.Should().Be(480);
            _ = HeightPx.Should().Be(75);
        }

        [Fact]
        public void Overlay_xor_must_not_paint_cover()
        {
            _ = TesseraFlyoutAnimatedTargetSpec.Win11XorFrostMustNotPaintCoverOverlay.Should().BeTrue();
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveWin11MediaOverlayAlpha(0.5, true).Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveWin11MediaOverlayAlpha(1, true).Should().Be(0);
        }

        [Fact]
        public void Fancy_must_clip_media_chrome_to_reveal_not_rest_hwnd()
        {
            _ = TesseraFlyoutHwndRegionSpec.Phase2MustClipMediaChromeToRevealProgress.Should().BeTrue();
            _ = TesseraFlyoutHwndRegionSpec.CollapsedRevealRegionMustHideRestChrome.Should().BeTrue();
            _ = TesseraFlyoutHwndRegionSpec.StyleNeedsRevealRegion("Fluent", musicVisible: true, stackedRole: null)
                .Should().BeTrue();
            _ = TesseraFlyoutHwndRegionSpec.StyleNeedsRevealRegion(
                    "Fluent", musicVisible: true, TesseraStackedPanelRole.Media)
                .Should().BeTrue();
            _ = TesseraFlyoutHwndRegionSpec.StyleNeedsRevealRegion(
                    "Fluent", musicVisible: true, TesseraStackedPanelRole.Volume)
                .Should().BeFalse();
            _ = TesseraFlyoutHwndRegionSpec.CollapsedShowRegionMustKeepWindowVisible.Should().BeTrue();
            _ = TesseraFlyoutHwndRegionSpec.ShouldZeroWindowOpacityForCollapsedRegion(
                    stackedMedia: true, hideChrome: true, showMotionActive: true)
                .Should().BeFalse();
            _ = TesseraFlyoutHwndRegionSpec.ShouldZeroWindowOpacityForCollapsedRegion(
                    stackedMedia: true, hideChrome: true, showMotionActive: false)
                .Should().BeTrue();
            _ = TesseraFlyoutHwndRegionSpec.ShouldZeroWindowOpacityForCollapsedRegion(
                    stackedMedia: false, hideChrome: true, showMotionActive: false)
                .Should().BeFalse();
            _ = TesseraFlyoutHwndRegionSpec.ShowStackedMediaMustWipeRegionOverRestLayout.Should().BeFalse();
            _ = TesseraFlyoutHwndRegionSpec.ShowLayoutMustFollowRevealProgress.Should().BeTrue();
            _ = TesseraFlyoutHwndRegionSpec.ShowRegionWipeMustNotRedrawClient.Should().BeTrue();
            _ = TesseraFlyoutHwndRegionSpec.ShouldWipeShowRegionOverRestLayout(
                    entrance: true, TesseraStackedPanelRole.Media, willRunPhase2: true)
                .Should().BeFalse();
            _ = TesseraFlyoutHwndRegionSpec.ShouldWipeShowRegionOverRestLayout(
                    entrance: false, TesseraStackedPanelRole.Media, willRunPhase2: true)
                .Should().BeFalse();
            _ = TesseraFlyoutHwndRegionSpec.ShouldWipeShowRegionOverRestLayout(
                    entrance: true, TesseraStackedPanelRole.Volume, willRunPhase2: true)
                .Should().BeFalse();
            _ = TesseraFlyoutHwndRegionSpec.ResolveShowLayoutRevealProgress(wipeRegionOverRest: true)
                .Should().Be(TesseraFlyoutRevealSpec.RestRevealProgress);
            _ = TesseraFlyoutHwndRegionSpec.ResolveShowLayoutRevealProgress(wipeRegionOverRest: false)
                .Should().Be(TesseraFlyoutRevealSpec.FancyPhase2StartProgress);
        }

        [Fact]
        public void Fluent_collapsed_region_is_volume_column_not_black_media_bay()
        {
            TesseraFlyoutHwndRegionSpec.RevealRegionDip collapsed = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                "Fluent",
                stackedRole: null,
                progress: 0,
                phase2Engaged: false,
                musicVisible: true,
                restWidthDip: 415,
                restHeightDip: 176);
            _ = collapsed.WidthDip.Should().Be(
                TesseraFluentLayoutSpec.VolumeWidthDip + TesseraFluentLayoutSpec.DividerColumnWidthDip);
            _ = collapsed.HideChrome.Should().BeFalse();

            TesseraFlyoutHwndRegionSpec.RevealRegionDip mediaCollapsed = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                "Fluent",
                TesseraStackedPanelRole.Media,
                progress: 0,
                phase2Engaged: false,
                musicVisible: true,
                restWidthDip: 340,
                restHeightDip: 176);
            _ = mediaCollapsed.HideChrome.Should().BeFalse();
            _ = mediaCollapsed.WidthDip.Should().Be(TesseraFlyoutHwndRegionSpec.MinRenderableRegionDip);
            _ = mediaCollapsed.HeightDip.Should().Be(176);
            _ = TesseraFlyoutHwndRegionSpec.CollapsedMediaRegionMustStayRenderable.Should().BeTrue();
            (int WidthPx, int HeightPx, _) = TesseraFlyoutHwndRegionSpec.ResolveRenderableRoundRectPhysical(
                mediaCollapsed.WidthDip, mediaCollapsed.HeightDip, 10, 1);
            _ = WidthPx.Should().BeGreaterThanOrEqualTo(TesseraFlyoutHwndRegionSpec.MinRenderableRegionPx);
            _ = HeightPx.Should().Be(176);

            TesseraFlyoutHwndRegionSpec.RevealRegionDip open = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                "Fluent",
                stackedRole: null,
                progress: 1,
                phase2Engaged: true,
                musicVisible: true,
                restWidthDip: 415,
                restHeightDip: 176);
            _ = open.WidthDip.Should().Be(415);
            _ = open.HideChrome.Should().BeFalse();
        }

        [Fact]
        public void Fluent_region_reaches_rest_width_at_progress_one()
        {
            const double restW = 430;
            const double restH = 180;
            TesseraFlyoutHwndRegionSpec.RevealRegionDip mid = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                "Fluent", null, 0.5, true, true, restW, restH);
            TesseraFlyoutHwndRegionSpec.RevealRegionDip open = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                "Fluent", null, 1, true, true, restW, restH);
            TesseraFlyoutHwndRegionSpec.RevealRegionDip mediaOpen = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                "Fluent", TesseraStackedPanelRole.Media, 1, true, true, 360, restH);

            _ = open.WidthDip.Should().Be(restW);
            _ = mid.WidthDip.Should().BeApproximately(
                TesseraFlyoutAnimatedTargetSpec.ResolveFluentCollapsedShellWidthDip(true)
                + ((restW - TesseraFlyoutAnimatedTargetSpec.ResolveFluentCollapsedShellWidthDip(true)) * 0.5),
                0.01);
            _ = mediaOpen.WidthDip.Should().Be(360);
            _ = mediaOpen.HideChrome.Should().BeFalse();
        }

        [Fact]
        public void Fluent_media_region_matches_at_time_reversed_OutQuart()
        {
            const int steps = 20;
            const double restW = 340;
            const double restH = 176;
            string ease = TesseraFlyoutAnimationPolicy.EaseOutQuart;

            for (int s = 0; s <= steps; s++)
            {
                double showP = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                    0, 1, s, steps, ease, entrance: true);
                double hideP = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                    1, 0, steps - s, steps, ease, entrance: false);
                TesseraFlyoutHwndRegionSpec.RevealRegionDip showRegion = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                    "Fluent", TesseraStackedPanelRole.Media, showP, true, true, restW, restH);
                TesseraFlyoutHwndRegionSpec.RevealRegionDip hideRegion = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                    "Fluent", TesseraStackedPanelRole.Media, hideP, true, true, restW, restH);

                _ = showRegion.WidthDip.Should().BeApproximately(hideRegion.WidthDip, 0.5);
                _ = showRegion.HideChrome.Should().Be(hideRegion.HideChrome);
                _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(148, showP, true)
                    .Should().BeApproximately(
                        TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(148, hideP, true),
                        0.5);
                _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaContentOpacity(showP, true)
                    .Should().BeApproximately(
                        TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaContentOpacity(hideP, true),
                        0.02);
            }

            double firstShowTick = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                0, 1, 1, steps, ease, entrance: true);
            TesseraFlyoutHwndRegionSpec.RevealRegionDip firstShowRegion = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                "Fluent", TesseraStackedPanelRole.Media, firstShowTick, true, true, restW, restH);
            _ = firstShowRegion.HideChrome.Should().BeFalse();
            _ = firstShowTick.Should().BeGreaterThan(0.15);
        }

        [Fact]
        public void Fluent_divider_height_is_zero_when_collapsed()
        {
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(148, 0, musicVisible: true)
                .Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerOpacity(0, musicVisible: true)
                .Should().Be(TesseraFlyoutAnimatedTargetSpec.FluentDividerRestOpacity);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(148, 0.5, musicVisible: true)
                .Should().Be(74);
        }

        [Fact]
        public void Idle_showing_session_forces_rest_progress_not_a_stale_host()
        {
            _ = TesseraFlyoutHwndRegionSpec.RevealRegionProgressMustUseMaxHost.Should().BeTrue();
            _ = TesseraFlyoutHwndRegionSpec.RestRevealMustUseSignedClientRegion.Should().BeTrue();
            _ = TesseraFlyoutHwndRegionSpec.ShouldForceRestRevealRegion(
                    motionAnimating: false, phase2Animating: false, sessionShowing: true)
                .Should().BeTrue();
            _ = TesseraFlyoutHwndRegionSpec.ShouldForceRestRevealRegion(
                    motionAnimating: true, phase2Animating: false, sessionShowing: true)
                .Should().BeFalse();
            _ = TesseraFlyoutHwndRegionSpec.ShouldForceRestRevealRegion(
                    motionAnimating: false, phase2Animating: true, sessionShowing: true)
                .Should().BeFalse();
            _ = TesseraFlyoutHwndRegionSpec.ShouldForceRestRevealRegion(
                    motionAnimating: false, phase2Animating: false, sessionShowing: false)
                .Should().BeFalse();

            _ = TesseraFlyoutHwndRegionSpec.ResolveRegionProgress(
                    [0.2, 0],
                    motionAnimating: false,
                    phase2Animating: false,
                    sessionShowing: true)
                .Should().Be(TesseraFlyoutRevealSpec.RestRevealProgress);
        }

        [Fact]
        public void Motion_uses_max_host_progress_not_first()
        {
            _ = TesseraFlyoutHwndRegionSpec.ResolveRegionProgress(
                    [0, 0.8],
                    motionAnimating: true,
                    phase2Animating: true,
                    sessionShowing: true)
                .Should().BeApproximately(0.8, 0.001);
            _ = TesseraFlyoutHwndRegionSpec.ResolveRegionProgress(
                    [0.15],
                    motionAnimating: true,
                    phase2Animating: false,
                    sessionShowing: true)
                .Should().BeApproximately(0.15, 0.001);
            _ = TesseraFlyoutHwndRegionSpec.ResolveRegionProgress(
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
            TesseraFlyoutHwndRegionSpec.RevealRegionDip mid = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                "Fluent",
                TesseraStackedPanelRole.Media,
                0.2,
                phase2Engaged: true,
                musicVisible: true,
                restWidthDip: signedRest,
                restHeightDip: restH);
            _ = mid.WidthDip.Should().BeApproximately(signedRest * 0.2, 0.5);
            _ = mid.WidthDip.Should().BeLessThan(100);

            double frozen = TesseraFlyoutHwndRegionSpec.ResolveRestExtentDip(
                signedRest, liveBoundsDip: mid.WidthDip);
            _ = frozen.Should().Be(signedRest);

            TesseraFlyoutHwndRegionSpec.RevealRegionDip open = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
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
            _ = open.WidthDip.Should().Be(signedRest);
            _ = open.HideChrome.Should().BeFalse();
        }

        [Fact]
        public void Rest_progress_never_hides_fluent_media_chrome()
        {
            _ = TesseraFlyoutHwndRegionSpec.IsRestRevealProgress(1).Should().BeTrue();
            _ = TesseraFlyoutHwndRegionSpec.IsRestRevealProgress(0.2).Should().BeFalse();
            TesseraFlyoutHwndRegionSpec.RevealRegionDip rest = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                "Fluent",
                TesseraStackedPanelRole.Media,
                TesseraFlyoutRevealSpec.RestRevealProgress,
                phase2Engaged: true,
                musicVisible: true,
                restWidthDip: 340,
                restHeightDip: 176);
            _ = rest.HideChrome.Should().BeFalse();
            _ = rest.WidthDip.Should().Be(340);
        }

        [Fact]
        public void Fallback_rest_sizes_read_style_profile()
        {
            TesseraFlyoutHwndRegionSpec.RevealRegionDip meterVol = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                StyleIds.Meter,
                TesseraStackedPanelRole.Volume,
                progress: 1,
                phase2Engaged: true,
                musicVisible: true,
                restWidthDip: 0,
                restHeightDip: 0);
            TesseraFlyoutStyleProfile profile = TesseraFlyoutTweenTargetCatalog.ResolveProfile(StyleIds.Meter);
            _ = meterVol.WidthDip.Should().Be(profile.VolumeWidthDip);
            _ = meterVol.HeightDip.Should().Be(profile.VolumeHeightDip);
        }

        [Fact]
        public void CoreUi_single_hwnd_keeps_rest_region_while_media_clip_animates()
        {
            _ = TesseraFlyoutHwndRegionSpec.CoreUiSingleHwndMustKeepRestRegion.Should().BeTrue();
            const double restW = TesseraCoreUiLayoutSpec.WidthDip;
            const double restH = 226;
            foreach (double p in new[] { 0.0, 0.35, 1.0 })
            {
                TesseraFlyoutHwndRegionSpec.RevealRegionDip region = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                    StyleIds.CoreUI,
                    stackedRole: null,
                    progress: p,
                    phase2Engaged: true,
                    musicVisible: true,
                    restWidthDip: restW,
                    restHeightDip: restH);
                _ = region.HideChrome.Should().BeFalse($"p={p}");
                _ = region.WidthDip.Should().Be(restW, $"p={p}");
                _ = region.HeightDip.Should().Be(restH, $"p={p}");
                _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaClipWidthDip(
                        TesseraCoreUiLayoutSpec.InnerRowWidthDip, p, true)
                    .Should().BeApproximately(TesseraCoreUiLayoutSpec.InnerRowWidthDip * p, 0.01);
            }
        }

        [Fact]
        public void CoreUi_media_slot_still_clips_hwnd_width()
        {
            TesseraFlyoutHwndRegionSpec.RevealRegionDip collapsed = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                StyleIds.CoreUI,
                TesseraStackedPanelRole.Media,
                progress: 0,
                phase2Engaged: true,
                musicVisible: true,
                restWidthDip: TesseraCoreUiLayoutSpec.InnerRowWidthDip,
                restHeightDip: TesseraCoreUiLayoutSpec.MediaHeightDip);
            _ = collapsed.HideChrome.Should().BeFalse();
            _ = collapsed.WidthDip.Should().Be(TesseraFlyoutHwndRegionSpec.MinRenderableRegionDip);
            _ = collapsed.HeightDip.Should().Be(TesseraCoreUiLayoutSpec.MediaHeightDip);

            TesseraFlyoutHwndRegionSpec.RevealRegionDip open = TesseraFlyoutHwndRegionSpec.ResolveRevealRegionDip(
                StyleIds.CoreUI,
                TesseraStackedPanelRole.Media,
                progress: 1,
                phase2Engaged: true,
                musicVisible: true,
                restWidthDip: TesseraCoreUiLayoutSpec.InnerRowWidthDip,
                restHeightDip: TesseraCoreUiLayoutSpec.MediaHeightDip);
            _ = open.HideChrome.Should().BeFalse();
            _ = open.WidthDip.Should().Be(TesseraCoreUiLayoutSpec.InnerRowWidthDip);
        }

        [Fact]
        public void CoreUi_phase2_clip_and_scale_follow_the_same_progress()
        {
            const int steps = 20;
            string showEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
                TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
            string hideEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
                TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false);
            double row = TesseraCoreUiLayoutSpec.InnerRowWidthDip;
            double showMid = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
                entrance: true, 10, steps, showEase);
            double hideEarly = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
                entrance: false, 5, steps, hideEase);
            double showClip = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaClipWidthDip(row, showMid, true);
            double hideClip = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaClipWidthDip(row, hideEarly, true);
            double showScale = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiVolumeBarScaleFactor(showMid, true);
            _ = showEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseInOutQuart);
            _ = hideEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseOutQuart);
            _ = showMid.Should().BeApproximately(0.5, 0.05);
            _ = showClip.Should().BeApproximately(row * showMid, 0.5);
            _ = showScale.Should().BeApproximately(showMid, 0.02);
            _ = hideClip.Should().BeGreaterThan(row * 0.9);
            _ = hideEarly.Should().BeGreaterThan(0.9);
        }
    }
}
