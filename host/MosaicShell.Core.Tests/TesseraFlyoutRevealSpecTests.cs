using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutRevealSpecTests
    {
        /// <summary>
        /// Radial (upstream "Smouti") is phase-2 capable in Host: the volume ring arc and the
        /// side media panel are both addressable, even though Smouti.inc comments its binders out.
        /// </summary>
        [Fact]
        public void Smouti_supports_phase2_via_ring_and_side_media()
        {
            _ = TesseraFlyoutRevealSpec.StyleIsPhase2NoOp(TesseraFlyoutRevealSpec.StyleSmouti).Should().BeFalse();
            _ = TesseraFlyoutRevealSpec.StyleIsPhase2NoOp(StyleIds.Radial).Should().BeFalse();
            _ = TesseraFlyoutRevealSpec.StyleSupportsPhase2(TesseraFlyoutRevealSpec.StyleSmouti).Should().BeTrue();
            _ = TesseraFlyoutRevealSpec.StyleSupportsPhase2(StyleIds.Radial).Should().BeTrue();

            // An unrecognised style still has nothing to drive.
            _ = TesseraFlyoutRevealSpec.StyleIsPhase2NoOp("unknown-style").Should().BeTrue();
        }

        [Fact]
        public void Hide_reveal_is_zero_only_when_fancy_phase2_will_run()
        {
            _ = TesseraFlyoutRevealSpec.ResolveHideRevealProgress(willRunPhase2: true)
                .Should().Be(TesseraFlyoutRevealSpec.FancyPhase2StartProgress);
            _ = TesseraFlyoutRevealSpec.ResolveHidePhase2Engaged(true).Should().BeFalse();
            _ = TesseraFlyoutRevealSpec.ResolveHideRevealProgress(willRunPhase2: false)
                .Should().Be(TesseraFlyoutRevealSpec.RestRevealProgress);
            _ = TesseraFlyoutRevealSpec.ResolveHidePhase2Engaged(false).Should().BeTrue();
            _ = TesseraFlyoutRevealSpec.ResolveShowRevealProgress(willRunPhase2: true)
                .Should().Be(TesseraFlyoutRevealSpec.FancyPhase2StartProgress);
            _ = TesseraFlyoutRevealSpec.ResolveShowRevealProgress(willRunPhase2: false)
                .Should().Be(TesseraFlyoutRevealSpec.RestRevealProgress);
            _ = TesseraFlyoutRevealSpec.ResolveMotionPhase2Engaged(0, willRunPhase2: true, entrance: true)
                .Should().BeTrue();
            _ = TesseraFlyoutRevealSpec.ResolveMotionPhase2Engaged(0, willRunPhase2: true, entrance: false)
                .Should().BeFalse();
            _ = TesseraFlyoutRevealSpec.ResolveMotionPhase2Engaged(1, willRunPhase2: true, entrance: false)
                .Should().BeTrue();
        }

        [Fact]
        public void Square_supports_phase2_without_media_strip()
        {
            _ = TesseraFlyoutRevealSpec.StyleSupportsPhase2(TesseraFlyoutRevealSpec.StyleSquare).Should().BeTrue();
            _ = TesseraFlyoutRevealSpec.StylePhase2WithoutMediaStrip(TesseraFlyoutRevealSpec.StyleSquare).Should().BeTrue();
            _ = TesseraFlyoutRevealSpec.UsesDedicatedRevealBinder(TesseraFlyoutRevealSpec.StyleFluent).Should().BeTrue();
            _ = TesseraFlyoutRevealSpec.UsesDedicatedRevealBinder("unknown").Should().BeFalse();
        }

        [Fact]
        public void Rest_reveal_is_fully_open()
        {
            _ = TesseraFlyoutRevealSpec.RestRevealProgress.Should().Be(1);
            _ = TesseraFlyoutRevealSpec.PreviewMustUseRestReveal.Should().BeTrue();
            _ = TesseraFlyoutRevealSpec.FancyPhase2StartProgress.Should().Be(0);
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
            bool fancy = TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(ani, style, media);
            _ = TesseraFlyoutRevealSpec.ResolveHideRevealProgress(fancy).Should().Be(expected);
        }

        [Theory]
        [InlineData(true, true, 1)]
        [InlineData(true, false, 1)]
        [InlineData(false, false, 1)]
        [InlineData(false, true, 0)]
        public void Initial_reveal_progress_is_rest_except_live_fancy(bool isPreview, bool willRunPhase2, double expected)
        {
            _ = TesseraFlyoutRevealSpec.ResolveInitialRevealProgress(isPreview, willRunPhase2)
                .Should().Be(expected);
        }

        [Fact]
        public void Visible_rebuild_initial_reveal_is_rest_even_for_live_fancy()
        {
            _ = TesseraFlyoutRevealSpec
                .ResolveInitialRevealProgress(isPreview: false, willRunPhase2: true, sessionAlreadyShowing: true)
                .Should().Be(TesseraFlyoutRevealSpec.RestRevealProgress);
            _ = TesseraFlyoutRevealSpec
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
            _ = TesseraFlyoutRevealSpec.ResolveInitialPhase2Engaged(isPreview, willRunPhase2)
                .Should().Be(expected);
        }
    }
}
