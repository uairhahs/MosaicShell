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

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.5, 0.5)]
    [InlineData(1, 1)]
    public void CoreUi_media_content_opacity_tracks_progress(double p, double expected)
    {
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaContentOpacityFactor(p, musicVisible: true)
            .Should().Be(expected);
    }

    [Fact]
    public void Media_overlay_is_container_mask_not_covering_white()
    {
        TesseraFlyoutAnimatedTargetSpec.MediaOverlayIsContainerMaskNotCover.Should().BeTrue();
        TesseraFlyoutAnimatedTargetSpec.MediaCoverOverlayMaxAlpha.Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.Win11VolumeControlsStayOpaque.Should().BeTrue();
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
    }

    [Fact]
    public void PlainText_slide_offset_matches_plainext_inc()
    {
        const double w = 320;
        const double scale = 1;
        TesseraFlyoutAnimatedTargetSpec.ResolvePlainTextSlideOffsetDip(w, scale, 0, true)
            .Should().BeApproximately(-295, 0.01);
        TesseraFlyoutAnimatedTargetSpec.ResolvePlainTextSlideOffsetDip(w, scale, 1, true)
            .Should().BeApproximately(0, 0.01);
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
        var layout = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaLayoutWidthDip(
            FluentMediaW, musicVisible: true);
        layout.Should().Be(FluentMediaW + TesseraFlyoutAnimatedTargetSpec.MediaClipWidthEpsilonDip);
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(FluentMediaW, 0, true)
            .Should().Be(1);
        layout.Should().BeGreaterThan(
            TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(FluentMediaW, 0, true));
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaLayoutWidthDip(FluentMediaW, false)
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
    }
}
