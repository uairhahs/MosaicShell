using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutAnimatedTargetSpecTests
    {
        private const double FluentInnerH = 148; // H=176, P=14 -> H-P*2
        private const double FluentMediaW = 340;

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0.5, 74)]
        [InlineData(1, 148)]
        public void Fluent_divider_height_matches_mediaB_line_formula(double p, double expectedDip)
        {
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(FluentInnerH, p, musicVisible: true)
                .Should().Be(expectedDip);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(0.5, 171)]
        [InlineData(1, 341)]
        public void Fluent_media_clip_width_includes_epsilon(double p, double expectedDip)
        {
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(FluentMediaW, p, musicVisible: true)
                .Should().Be(expectedDip);
        }

        [Fact]
        public void Fluent_visible_shell_keeps_divider_column_when_collapsed()
        {
            _ = TesseraFlyoutAnimatedTargetSpec.FluentDividerMustTweenInPlaceOnShow.Should().BeTrue();
            double collapsed = TesseraFlyoutAnimatedTargetSpec.ResolveFluentCollapsedShellWidthDip(true);
            _ = collapsed.Should().Be(
                TesseraFluentLayoutSpec.VolumeWidthDip + TesseraFluentLayoutSpec.DividerColumnWidthDip);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentVisibleShellWidthDip(0, musicVisible: true)
                .Should().Be(collapsed);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentStackedMediaRegionWidthDip(0, musicVisible: true)
                .Should().Be(0);
            double rest = TesseraFluentLayoutSpec.VolumeWidthDip + TesseraFluentLayoutSpec.MediaWidthDip;
            _ = collapsed.Should().BeLessThan(rest * 0.3);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentVisibleShellWidthDip(1, musicVisible: true)
                .Should().BeGreaterThan(rest * 0.9);
        }

        [Fact]
        public void Fluent_stacked_volume_owns_divider_column_and_media_abuts()
        {
            _ = TesseraFlyoutAnimatedTargetSpec.FluentDividerMustTweenInPlaceOnShow.Should().BeTrue();
            double collapsed = TesseraFlyoutAnimatedTargetSpec.ResolveFluentCollapsedShellWidthDip(true);
            _ = collapsed.Should().Be(TesseraFlyoutAnimatedTargetSpec.ResolveFluentStackedMediaOffsetXDip(true));

            IReadOnlyList<TesseraStackedPanelPlacement> placements = TesseraStackedPlacementPolicy.EstimatePlacements("Fluent");
            TesseraStackedPanelPlacement volume = placements.Single(p => p.Role == TesseraStackedPanelRole.Volume);
            TesseraStackedPanelPlacement media = placements.Single(p => p.Role == TesseraStackedPanelRole.Media);
            _ = volume.WidthDip.Should().Be(collapsed);
            _ = media.OffsetXDip.Should().Be(collapsed);
            _ = media.OffsetXDip.Should().Be(volume.OffsetXDip + volume.WidthDip);

            IReadOnlyList<TesseraStackedPanelPlacement> computed = TesseraStackedPlacementPolicy.ComputePlacements(
                "Fluent",
                volumeWidthDip: collapsed,
                volumeHeightDip: TesseraFluentLayoutSpec.HeightDip,
                mediaWidthDip: 324,
                mediaHeightDip: TesseraFluentLayoutSpec.HeightDip);
            _ = computed.Single(p => p.Role == TesseraStackedPanelRole.Media).OffsetXDip.Should().Be(collapsed);
        }

        [Fact]
        public void Fluent_phase2_divider_and_media_share_one_progress()
        {
            _ = TesseraFlyoutAnimationPolicy.Phase2MustShareOneVsyncProgressAcrossStackedSlots.Should().BeTrue();
            const int steps = 20;
            string showEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
                TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
            string hideEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
                TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false);
            double pShowMid = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
                entrance: true, 10, steps, showEase);
            double pHideEarly = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
                entrance: false, 5, steps, hideEase);

            double mediaShow = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(
                FluentMediaW, pShowMid, musicVisible: true);
            double dividerShow = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(
                FluentInnerH, pShowMid, musicVisible: true);
            double mediaHide = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(
                FluentMediaW, pHideEarly, musicVisible: true);

            _ = showEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseInOutQuart);
            _ = hideEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseOutQuart);
            _ = pShowMid.Should().BeApproximately(0.5, 0.05);
            _ = (dividerShow / FluentInnerH).Should().BeApproximately(pShowMid, 0.01);
            _ = ((mediaShow - TesseraFlyoutAnimatedTargetSpec.MediaClipWidthEpsilonDip) / FluentMediaW)
                .Should().BeApproximately(pShowMid, 0.01);
            _ = mediaHide.Should().BeGreaterThan(FluentMediaW * 0.9);
        }

        [Fact]
        public void Fluent_no_music_keeps_divider_and_clip_at_zero()
        {
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(FluentInnerH, 1, musicVisible: false)
                .Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(FluentMediaW, 1, musicVisible: false)
                .Should().Be(0);
        }

        [Theory]
        [InlineData(0, 50)]
        [InlineData(0.5, 137.5)]
        [InlineData(1, 225)]
        public void Win11_border_height_grows_volume_plus_partial_media(double p, double expectedDip)
        {
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveWin11BorderHeightDip(50, 175, p, musicVisible: true)
                .Should().Be(expectedDip);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 175)]
        public void Win11_media_clip_height_scales_with_progress(double p, double expectedDip)
        {
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveWin11MediaClipHeightDip(175, p, musicVisible: true)
                .Should().Be(expectedDip);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0.5, 112.5)]
        [InlineData(1, 225)]
        public void Win11_shell_clip_height_matches_xor_formula(double p, double expectedDip)
        {
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveWin11ShellClipHeightDip(50, 175, p, musicVisible: true)
                .Should().Be(expectedDip);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0.5, 0.5)]
        [InlineData(1, 1)]
        public void Win11_volume_fill_opacity_tracks_tweenNode1(double p, double expected)
        {
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveWin11VolumeFillOpacityFactor(p, musicVisible: true)
                .Should().Be(expected);
        }

        [Fact]
        public void Fluent_shell_width_static_when_music_visible()
        {
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentShellWidthDip(72, 340, musicVisible: true)
                .Should().Be(412);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentShellWidthDip(72, 340, musicVisible: false)
                .Should().Be(72);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0.5, 0.5)]
        [InlineData(1, 1)]
        public void CoreUi_volume_bar_scale_tracks_progress(double p, double expected)
        {
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiVolumeBarScaleFactor(p, musicVisible: true)
                .Should().Be(expected);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 280)]
        public void CoreUi_media_clip_width_scales(double p, double expected)
        {
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaClipWidthDip(280, p, musicVisible: true)
                .Should().Be(expected);
        }

        [Fact]
        public void CoreUi_media_clip_only_no_leaf_slide_or_fade()
        {
            _ = TesseraFlyoutAnimatedTargetSpec.CoreUiMediaMustClipOnly.Should().BeTrue();
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaSlideOffsetDip(388, 0, true)
                .Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaSlideOffsetDip(388, 0.5, true)
                .Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaContentOpacityFactor(0, true)
                .Should().Be(1);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaContentOpacityFactor(0.5, true)
                .Should().Be(1);
        }

        [Fact]
        public void CoreUi_rest_clip_host_width_is_wrapped_row_not_art_column()
        {
            _ = TesseraCoreUiLayoutSpec.MediaLayoutMustUseWrappedRowWidth.Should().BeTrue();
            _ = TesseraCoreUiLayoutSpec.PlayButtonCornerRadiusDip.Should().Be(TesseraCoreUiLayoutSpec.PlayButtonDip / 2);
            double row = TesseraCoreUiLayoutSpec.InnerRowWidthDip;
            double art = TesseraCoreUiLayoutSpec.ArtColumnWidthDip;
            _ = row.Should().Be(388);
            _ = art.Should().Be(324);
            _ = row.Should().BeGreaterThan(art);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiWrappedRowLayoutWidthDip(true)
                .Should().Be(row);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaLayoutWidthDip(row, true)
                .Should().Be(row);
        }

        [Fact]
        public void Media_overlay_is_container_mask_not_covering_white()
        {
            _ = TesseraFlyoutAnimatedTargetSpec.MediaOverlayIsContainerMaskNotCover.Should().BeTrue();
            _ = TesseraFlyoutAnimatedTargetSpec.MediaCoverOverlayMaxAlpha.Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.Win11VolumeControlsStayOpaque.Should().BeTrue();
            _ = TesseraFlyoutAnimatedTargetSpec.Win11XorFrostMustNotPaintCoverOverlay.Should().BeTrue();
            _ = TesseraFlyoutAnimatedTargetSpec.GnomeVolumeMustNotUseContentScale.Should().BeTrue();
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaOverlayAlpha(1, musicVisible: true)
                .Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveWin11MediaOverlayAlpha(1, musicVisible: true)
                .Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveGnomeOverlayAlpha(1, musicVisible: true)
                .Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaOverlayAlpha(1, musicVisible: true)
                .Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(0.5, musicVisible: true)
                .Should().Be(0.5);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(0, musicVisible: true)
                .Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(1, musicVisible: true)
                .Should().Be(1);
        }

        [Fact]
        public void Fluent_animated_meters_dissolve_with_tween_node1()
        {
            _ = TesseraFlyoutAnimatedTargetSpec.AnimatedMediaMustDissolveWithTweenNode1.Should().BeTrue();
            _ = TesseraFlyoutAnimatedTargetSpec.ClippedMediaLeafMustStayOpaque.Should().BeTrue();
            _ = TesseraFlyoutAnimatedTargetSpec.FluentVolumeStaysOpaqueDuringPhase2.Should().BeTrue();
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaContentOpacity(0, musicVisible: true)
                .Should().Be(1);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaContentOpacity(0.5, musicVisible: true)
                .Should().Be(1);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaContentOpacity(1, musicVisible: true)
                .Should().Be(1);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerOpacity(0, musicVisible: true)
                .Should().Be(TesseraFlyoutAnimatedTargetSpec.FluentDividerRestOpacity);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerOpacity(1, musicVisible: true)
                .Should().Be(TesseraFlyoutAnimatedTargetSpec.FluentDividerRestOpacity);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerOpacity(0.5, musicVisible: true)
                .Should().Be(TesseraFlyoutAnimatedTargetSpec.FluentDividerRestOpacity);
            _ = TesseraFlyoutAnimatedTargetSpec.FluentDividerOpacityMustStayRestWhileMusicVisible.Should().BeTrue();
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerOpacity(1, musicVisible: false)
                .Should().Be(0);
        }

        [Fact]
        public void Gnome_volume_fill_is_opacity_not_scale()
        {
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveGnomeVolumeFillOpacity(0, true).Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveGnomeVolumeFillOpacity(1, true).Should().Be(1);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveGnomeContentScale(0, true).Should().Be(0.5);
        }

        [Fact]
        public void Square_label_scale_tracks_tweenNode1()
        {
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveSquareLabelScale(0).Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveSquareLabelScale(1).Should().Be(1);
        }

        [Fact]
        public void Stepped_tweenNode1_aligns_with_continuous_progress_at_midpoint()
        {
            double stepped = TesseraFlyoutAnimationPolicy.ResolveTweenNode(
                    10, 20, TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true)
                / 100.0;
            double clipW = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(
                FluentMediaW, stepped, musicVisible: true);
            _ = clipW.Should().BeApproximately((340 * stepped) + 1, 0.5);
        }

        [Fact]
        public void Fluent_layout_width_stays_rest_while_clip_tracks_progress()
        {
            double mediaW = TesseraFluentLayoutSpec.MediaWidthDip;
            double layout = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaLayoutWidthDip(
                mediaW, musicVisible: true);
            _ = layout.Should().Be(mediaW + TesseraFlyoutAnimatedTargetSpec.MediaClipWidthEpsilonDip);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(mediaW, 0, true)
                .Should().Be(1);
            _ = layout.Should().BeGreaterThan(
                TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(mediaW, 0, true));
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaLayoutWidthDip(mediaW, false)
                .Should().Be(0);
        }

        [Fact]
        public void Win11_layout_height_stays_rest_while_clip_tracks_progress()
        {
            double vol = TesseraStackedPlacementSpec.Win11VolumeHeightDip;
            double media = TesseraStackedPlacementSpec.Win11MediaHeightDip;
            double layout = TesseraFlyoutAnimatedTargetSpec.ResolveWin11LayoutHeightDip(vol, media, true);
            _ = layout.Should().Be(vol + media);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveWin11BorderHeightDip(vol, media, 0, true)
                .Should().Be(vol);
            _ = layout.Should().BeGreaterThan(
                TesseraFlyoutAnimatedTargetSpec.ResolveWin11BorderHeightDip(vol, media, 0, true));
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveWin11LayoutHeightDip(vol, media, false)
                .Should().Be(vol);
        }

        [Fact]
        public void CoreUi_layout_thickness_and_media_width_stay_rest()
        {
            const double baseThickness = 8;
            const double panelW = 280;
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiLayoutTrackThicknessDip(baseThickness)
                .Should().Be(baseThickness);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiVolumeBarScaleFactor(0, true)
                .Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaLayoutWidthDip(panelW, true)
                .Should().Be(panelW);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaClipWidthDip(panelW, 0, true)
                .Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaLayoutWidthDip(panelW, false)
                .Should().Be(panelW);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiWrappedRowLayoutWidthDip(true)
                .Should().Be(TesseraCoreUiLayoutSpec.InnerRowWidthDip);
        }
    }
}
