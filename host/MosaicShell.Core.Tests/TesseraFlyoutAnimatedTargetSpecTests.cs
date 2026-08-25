using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

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
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(FluentInnerH, p, musicVisible: true)
            .Should().Be(expectedDip);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(0.5, 171)]
    [InlineData(1, 341)]
    public void Fluent_media_clip_width_includes_epsilon(double p, double expectedDip)
    {
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(FluentMediaW, p, musicVisible: true)
            .Should().Be(expectedDip);
    }

    [Fact]
    public void Fluent_visible_shell_keeps_divider_column_when_collapsed()
    {
        TesseraFlyoutAnimatedTargetSpec.FluentDividerMustTweenInPlaceOnShow.Should().BeTrue();
        var collapsed = TesseraFlyoutAnimatedTargetSpec.ResolveFluentCollapsedShellWidthDip(true);
        collapsed.Should().Be(
            TesseraFluentLayoutSpec.VolumeWidthDip + TesseraFluentLayoutSpec.DividerColumnWidthDip);
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentVisibleShellWidthDip(0, musicVisible: true)
            .Should().Be(collapsed);
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentStackedMediaRegionWidthDip(0, musicVisible: true)
            .Should().Be(0);
        var rest = TesseraFluentLayoutSpec.VolumeWidthDip + TesseraFluentLayoutSpec.MediaWidthDip;
        collapsed.Should().BeLessThan(rest * 0.3);
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentVisibleShellWidthDip(1, musicVisible: true)
            .Should().BeGreaterThan(rest * 0.9);
    }

    [Fact]
    public void Fluent_stacked_volume_owns_divider_column_and_media_abuts()
    {
        TesseraFlyoutAnimatedTargetSpec.FluentDividerMustTweenInPlaceOnShow.Should().BeTrue();
        var collapsed = TesseraFlyoutAnimatedTargetSpec.ResolveFluentCollapsedShellWidthDip(true);
        collapsed.Should().Be(TesseraFlyoutAnimatedTargetSpec.ResolveFluentStackedMediaOffsetXDip(true));

        var placements = TesseraStackedPlacementPolicy.EstimatePlacements("Fluent");
        var volume = placements.Single(p => p.Role == TesseraStackedPanelRole.Volume);
        var media = placements.Single(p => p.Role == TesseraStackedPanelRole.Media);
        volume.WidthDip.Should().Be(collapsed);
        media.OffsetXDip.Should().Be(collapsed);
        media.OffsetXDip.Should().Be(volume.OffsetXDip + volume.WidthDip);

        var computed = TesseraStackedPlacementPolicy.ComputePlacements(
            "Fluent",
            volumeWidthDip: collapsed,
            volumeHeightDip: TesseraFluentLayoutSpec.HeightDip,
            mediaWidthDip: 324,
            mediaHeightDip: TesseraFluentLayoutSpec.HeightDip);
        computed.Single(p => p.Role == TesseraStackedPanelRole.Media).OffsetXDip.Should().Be(collapsed);
    }

    [Fact]
    public void Fluent_phase2_show_wipes_in_with_the_same_ease_hide_uses()
    {
        const int steps = 20;
        var showEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
            TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
        var hideEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
            TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false);
        var pShow = TesseraFlyoutAnimationPolicy.InterpolateStepped(
            0, 1, 5, steps, showEase, entrance: true);
        var pHide = TesseraFlyoutAnimationPolicy.InterpolateStepped(
            1, 0, 5, steps, hideEase, entrance: false);

        var mediaShow = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(
            FluentMediaW, pShow, musicVisible: true);
        var mediaHide = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(
            FluentMediaW, pHide, musicVisible: true);
        var dividerShow = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(
            FluentInnerH, pShow, musicVisible: true);

        showEase.Should().Be(hideEase);
        mediaShow.Should().BeGreaterThan(FluentMediaW * 0.5);
        dividerShow.Should().BeGreaterThan(FluentInnerH * 0.5);
        mediaHide.Should().BeGreaterThan(FluentMediaW * 0.9);
    }

    [Fact]
    public void Fluent_no_music_keeps_divider_and_clip_at_zero()
    {
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(FluentInnerH, 1, musicVisible: false)
            .Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(FluentMediaW, 1, musicVisible: false)
            .Should().Be(0);
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(0.5, 137.5)]
    [InlineData(1, 225)]
    public void Win11_border_height_grows_volume_plus_partial_media(double p, double expectedDip)
    {
        TesseraFlyoutAnimatedTargetSpec.ResolveWin11BorderHeightDip(50, 175, p, musicVisible: true)
            .Should().Be(expectedDip);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 175)]
    public void Win11_media_clip_height_scales_with_progress(double p, double expectedDip)
    {
        TesseraFlyoutAnimatedTargetSpec.ResolveWin11MediaClipHeightDip(175, p, musicVisible: true)
            .Should().Be(expectedDip);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.5, 112.5)]
    [InlineData(1, 225)]
    public void Win11_shell_clip_height_matches_xor_formula(double p, double expectedDip)
    {
        TesseraFlyoutAnimatedTargetSpec.ResolveWin11ShellClipHeightDip(50, 175, p, musicVisible: true)
            .Should().Be(expectedDip);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.5, 0.5)]
    [InlineData(1, 1)]
    public void Win11_volume_fill_opacity_tracks_tweenNode1(double p, double expected)
    {
        TesseraFlyoutAnimatedTargetSpec.ResolveWin11VolumeFillOpacityFactor(p, musicVisible: true)
            .Should().Be(expected);
    }

    [Fact]
    public void Fluent_shell_width_static_when_music_visible()
    {
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentShellWidthDip(72, 340, musicVisible: true)
            .Should().Be(412);
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentShellWidthDip(72, 340, musicVisible: false)
            .Should().Be(72);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.5, 0.5)]
    [InlineData(1, 1)]
    public void CoreUi_volume_bar_scale_tracks_progress(double p, double expected)
    {
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiVolumeBarScaleFactor(p, musicVisible: true)
            .Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 280)]
    public void CoreUi_media_clip_width_scales(double p, double expected)
    {
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaClipWidthDip(280, p, musicVisible: true)
            .Should().Be(expected);
    }

    [Fact]
    public void CoreUi_media_clip_only_no_leaf_slide_or_fade()
    {
        TesseraFlyoutAnimatedTargetSpec.CoreUiMediaMustClipOnly.Should().BeTrue();
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaSlideOffsetDip(388, 0, true)
            .Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaSlideOffsetDip(388, 0.5, true)
            .Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaContentOpacityFactor(0, true)
            .Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaContentOpacityFactor(0.5, true)
            .Should().Be(0.5);
    }

    [Fact]
    public void CoreUi_rest_clip_host_width_is_wrapped_row_not_art_column()
    {
        TesseraCoreUiLayoutSpec.MediaLayoutMustUseWrappedRowWidth.Should().BeTrue();
        var row = TesseraCoreUiLayoutSpec.InnerRowWidthDip;
        var art = TesseraCoreUiLayoutSpec.ArtColumnWidthDip;
        row.Should().Be(388);
        art.Should().Be(324);
        row.Should().BeGreaterThan(art);
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiWrappedRowLayoutWidthDip(true)
            .Should().Be(row);
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaLayoutWidthDip(row, true)
            .Should().Be(row);
    }

    [Fact]
    public void Media_overlay_is_container_mask_not_covering_white()
    {
        TesseraFlyoutAnimatedTargetSpec.MediaOverlayIsContainerMaskNotCover.Should().BeTrue();
        TesseraFlyoutAnimatedTargetSpec.MediaCoverOverlayMaxAlpha.Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.Win11VolumeControlsStayOpaque.Should().BeTrue();
        TesseraFlyoutAnimatedTargetSpec.Win11XorFrostMustNotPaintCoverOverlay.Should().BeTrue();
        TesseraFlyoutAnimatedTargetSpec.GnomeVolumeMustNotUseContentScale.Should().BeTrue();
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaOverlayAlpha(1, musicVisible: true)
            .Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveWin11MediaOverlayAlpha(1, musicVisible: true)
            .Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveGnomeOverlayAlpha(1, musicVisible: true)
            .Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaOverlayAlpha(1, musicVisible: true)
            .Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(0.5, musicVisible: true)
            .Should().Be(0.5);
        TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(0, musicVisible: true)
            .Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(1, musicVisible: true)
            .Should().Be(1);
    }

    [Fact]
    public void Fluent_animated_meters_dissolve_with_tween_node1()
    {
        TesseraFlyoutAnimatedTargetSpec.AnimatedMediaMustDissolveWithTweenNode1.Should().BeTrue();
        TesseraFlyoutAnimatedTargetSpec.FluentVolumeStaysOpaqueDuringPhase2.Should().BeTrue();
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaContentOpacity(0, musicVisible: true)
            .Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaContentOpacity(0.5, musicVisible: true)
            .Should().Be(0.5);
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaContentOpacity(1, musicVisible: true)
            .Should().Be(1);
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerOpacity(0, musicVisible: true)
            .Should().Be(TesseraFlyoutAnimatedTargetSpec.FluentDividerRestOpacity);
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerOpacity(1, musicVisible: true)
            .Should().Be(TesseraFlyoutAnimatedTargetSpec.FluentDividerRestOpacity);
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerOpacity(0.5, musicVisible: true)
            .Should().Be(TesseraFlyoutAnimatedTargetSpec.FluentDividerRestOpacity);
        TesseraFlyoutAnimatedTargetSpec.FluentDividerOpacityMustStayRestWhileMusicVisible.Should().BeTrue();
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerOpacity(1, musicVisible: false)
            .Should().Be(0);
    }

    [Fact]
    public void PlainText_slide_offset_matches_plainext_inc()
    {
        var w = TesseraStackedPlacementSpec.PlainTextWidthDip;
        const double scale = 1;
        TesseraFlyoutAnimatedTargetSpec.ResolvePlainTextSlideOffsetDip(w, scale, 0, true)
            .Should().BeApproximately(-(w - 25), 0.01);
        TesseraFlyoutAnimatedTargetSpec.ResolvePlainTextSlideOffsetDip(w, scale, 1, true)
            .Should().BeApproximately(0, 0.01);
    }

    [Fact]
    public void Gnome_volume_fill_is_opacity_not_scale()
    {
        TesseraFlyoutAnimatedTargetSpec.ResolveGnomeVolumeFillOpacity(0, true).Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveGnomeVolumeFillOpacity(1, true).Should().Be(1);
        TesseraFlyoutAnimatedTargetSpec.ResolveGnomeContentScale(0, true).Should().Be(0.5);
    }

    [Fact]
    public void Square_label_scale_tracks_tweenNode1()
    {
        TesseraFlyoutAnimatedTargetSpec.ResolveSquareLabelScale(0).Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveSquareLabelScale(1).Should().Be(1);
    }

    [Fact]
    public void Stepped_tweenNode1_aligns_with_continuous_progress_at_midpoint()
    {
        var stepped = TesseraFlyoutAnimationPolicy.ResolveTweenNode(
                10, 20, TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true)
            / 100.0;
        var clipW = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(
            FluentMediaW, stepped, musicVisible: true);
        clipW.Should().BeApproximately(340 * stepped + 1, 0.5);
    }

    [Fact]
    public void Fluent_layout_width_stays_rest_while_clip_tracks_progress()
    {
        var mediaW = TesseraFluentLayoutSpec.MediaWidthDip;
        var layout = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaLayoutWidthDip(
            mediaW, musicVisible: true);
        layout.Should().Be(mediaW + TesseraFlyoutAnimatedTargetSpec.MediaClipWidthEpsilonDip);
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(mediaW, 0, true)
            .Should().Be(1);
        layout.Should().BeGreaterThan(
            TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(mediaW, 0, true));
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaLayoutWidthDip(mediaW, false)
            .Should().Be(0);
    }

    [Fact]
    public void Win11_layout_height_stays_rest_while_clip_tracks_progress()
    {
        var vol = TesseraStackedPlacementSpec.Win11VolumeHeightDip;
        var media = TesseraStackedPlacementSpec.Win11MediaHeightDip;
        var layout = TesseraFlyoutAnimatedTargetSpec.ResolveWin11LayoutHeightDip(vol, media, true);
        layout.Should().Be(vol + media);
        TesseraFlyoutAnimatedTargetSpec.ResolveWin11BorderHeightDip(vol, media, 0, true)
            .Should().Be(vol);
        layout.Should().BeGreaterThan(
            TesseraFlyoutAnimatedTargetSpec.ResolveWin11BorderHeightDip(vol, media, 0, true));
        TesseraFlyoutAnimatedTargetSpec.ResolveWin11LayoutHeightDip(vol, media, false)
            .Should().Be(vol);
    }

    [Fact]
    public void CoreUi_layout_thickness_and_media_width_stay_rest()
    {
        const double baseThickness = 8;
        const double panelW = 280;
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiLayoutTrackThicknessDip(baseThickness)
            .Should().Be(baseThickness);
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiVolumeBarScaleFactor(0, true)
            .Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaLayoutWidthDip(panelW, true)
            .Should().Be(panelW);
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaClipWidthDip(panelW, 0, true)
            .Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaLayoutWidthDip(panelW, false)
            .Should().Be(panelW);
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiWrappedRowLayoutWidthDip(true)
            .Should().Be(TesseraCoreUiLayoutSpec.InnerRowWidthDip);
    }
}
