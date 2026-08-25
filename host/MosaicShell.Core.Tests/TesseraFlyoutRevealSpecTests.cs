using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutRevealSpecTests
{
    [Theory]
    [InlineData(TesseraFlyoutRevealSpec.StyleFluent, 0, 0)]
    [InlineData(TesseraFlyoutRevealSpec.StyleFluent, 0.5, 0.5)]
    [InlineData(TesseraFlyoutRevealSpec.StyleFluent, 1, 1)]
    public void Fluent_media_width_scales_with_reveal(string style, double p, double expected)
    {
        TesseraFlyoutRevealSpec.ResolveMediaWidthFactor(style, p, musicVisible: true)
            .Should().Be(expected);
    }

    [Theory]
    [InlineData(TesseraFlyoutRevealSpec.StyleWindows11, 0, 0)]
    [InlineData(TesseraFlyoutRevealSpec.StyleWindows11, 1, 1)]
    public void Win11_media_clip_height_scales(string style, double p, double expected)
    {
        TesseraFlyoutRevealSpec.ResolveMediaClipHeightFactor(style, p, musicVisible: true)
            .Should().Be(expected);
    }

    [Fact]
    public void Gnome_scale_uses_half_to_full_range()
    {
        TesseraFlyoutRevealSpec.ResolveContentScale(TesseraFlyoutRevealSpec.StyleGnome, 0, true)
            .Should().Be(0.5);
        TesseraFlyoutRevealSpec.ResolveContentScale(TesseraFlyoutRevealSpec.StyleGnome, 1, true)
            .Should().Be(1);
    }

    [Fact]
    public void PlainText_slide_factor_decreases_with_progress()
    {
        TesseraFlyoutRevealSpec.ResolvePlainTextSlideFactor(
                TesseraFlyoutRevealSpec.StylePlainText, 0, true)
            .Should().Be(1);
        TesseraFlyoutRevealSpec.ResolvePlainTextSlideFactor(
                TesseraFlyoutRevealSpec.StylePlainText, 1, true)
            .Should().Be(0);
    }

    [Fact]
    public void Smouti_is_phase2_no_op()
    {
        TesseraFlyoutRevealSpec.StyleIsPhase2NoOp(TesseraFlyoutRevealSpec.StyleSmouti).Should().BeTrue();
        TesseraFlyoutRevealSpec.StyleIsPhase2NoOp(StyleIds.Radial).Should().BeTrue();
        TesseraFlyoutRevealSpec.StyleSupportsPhase2(TesseraFlyoutRevealSpec.StyleSmouti).Should().BeFalse();
        TesseraFlyoutRevealSpec.StyleSupportsPhase2(StyleIds.Radial).Should().BeFalse();
    }

    [Fact]
    public void No_media_strip_returns_full_factors()
    {
        TesseraFlyoutRevealSpec.ResolveMediaWidthFactor(TesseraFlyoutRevealSpec.StyleFluent, 0, false)
            .Should().Be(1);
    }

    [Fact]
    public void Hide_reveal_is_zero_only_when_fancy_phase2_will_run()
    {
        TesseraFlyoutRevealSpec.ResolveHideRevealProgress(willRunPhase2: true)
            .Should().Be(TesseraFlyoutRevealSpec.FancyPhase2StartProgress);
        TesseraFlyoutRevealSpec.ResolveHidePhase2Engaged(true).Should().BeFalse();
        TesseraFlyoutRevealSpec.ResolveHideRevealProgress(willRunPhase2: false)
            .Should().Be(TesseraFlyoutRevealSpec.RestRevealProgress);
        TesseraFlyoutRevealSpec.ResolveHidePhase2Engaged(false).Should().BeTrue();
        TesseraFlyoutRevealSpec.ResolveShowRevealProgress(willRunPhase2: true)
            .Should().Be(TesseraFlyoutRevealSpec.FancyPhase2StartProgress);
        TesseraFlyoutRevealSpec.ResolveShowRevealProgress(willRunPhase2: false)
            .Should().Be(TesseraFlyoutRevealSpec.RestRevealProgress);
    }

    [Fact]
    public void Square_supports_phase2_without_media_strip()
    {
        TesseraFlyoutRevealSpec.StyleSupportsPhase2(TesseraFlyoutRevealSpec.StyleSquare).Should().BeTrue();
        TesseraFlyoutRevealSpec.StylePhase2WithoutMediaStrip(TesseraFlyoutRevealSpec.StyleSquare).Should().BeTrue();
        TesseraFlyoutRevealSpec.UsesDedicatedRevealBinder(TesseraFlyoutRevealSpec.StyleFluent).Should().BeTrue();
        TesseraFlyoutRevealSpec.UsesDedicatedRevealBinder("unknown").Should().BeFalse();
    }

    [Fact]
    public void Rest_reveal_is_fully_open()
    {
        TesseraFlyoutRevealSpec.RestRevealProgress.Should().Be(1);
        TesseraFlyoutRevealSpec.PreviewMustUseRestReveal.Should().BeTrue();
        TesseraFlyoutRevealSpec.FancyPhase2StartProgress.Should().Be(0);
    }

    [Theory]
    [InlineData(0, "Fluent", true, 1)]
    [InlineData(1, "Fluent", true, 1)]
    [InlineData(2, "Fluent", true, 0)]
    [InlineData(2, "Fluent", false, 1)]
    [InlineData(0, "Square", false, 1)]
    [InlineData(2, "Square", false, 0)]
    public void Hide_pose_follows_ani_and_style(int ani, string style, bool media, double expected)
    {
        var fancy = TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(ani, style, media);
        TesseraFlyoutRevealSpec.ResolveHideRevealProgress(fancy).Should().Be(expected);
    }

    [Theory]
    [InlineData(true, true, 1)]
    [InlineData(true, false, 1)]
    [InlineData(false, false, 1)]
    [InlineData(false, true, 0)]
    public void Initial_reveal_progress_is_rest_except_live_fancy(bool isPreview, bool willRunPhase2, double expected)
    {
        TesseraFlyoutRevealSpec.ResolveInitialRevealProgress(isPreview, willRunPhase2)
            .Should().Be(expected);
    }

    [Fact]
    public void Visible_rebuild_initial_reveal_is_rest_even_for_live_fancy()
    {
        TesseraFlyoutRevealSpec
            .ResolveInitialRevealProgress(isPreview: false, willRunPhase2: true, sessionAlreadyShowing: true)
            .Should().Be(TesseraFlyoutRevealSpec.RestRevealProgress);
        TesseraFlyoutRevealSpec
            .ResolveInitialPhase2Engaged(isPreview: false, willRunPhase2: true, sessionAlreadyShowing: true)
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    public void Initial_phase2_engaged_matches_rest_pose(bool isPreview, bool willRunPhase2, bool expected)
    {
        TesseraFlyoutRevealSpec.ResolveInitialPhase2Engaged(isPreview, willRunPhase2)
            .Should().Be(expected);
    }
}
