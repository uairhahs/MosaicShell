using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutAnimationPolicyTests
    {
        [Theory]
        [InlineData(0, 10, 250)]
        [InlineData(0, 20, 250)]
        [InlineData(0, 40, 250)]
        [InlineData(1, 10, 160)]
        [InlineData(1, 20, 320)]
        [InlineData(2, 20, 320)]
        [InlineData(2, 40, 640)]
        public void Duration_matches_yourflyouts_step_timing(int ani, int steps, int ms)
        {
            _ = TesseraFlyoutAnimationPolicy.ResolveDurationMs(ani, steps).Should().Be(ms);
        }

        [Fact]
        public void Ani0_uses_rainmeter_fade_duration_and_ignores_ani_steps()
        {
            _ = TesseraFlyoutAnimationPolicy.AniNoneUsesBuiltInFade.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.AniNoneIgnoresAniSteps.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.FadeDurationMs.Should().Be(250);
            _ = TesseraFlyoutAnimationPolicy.ResolveDurationMs(0, 10)
                .Should().Be(TesseraFlyoutAnimationPolicy.ResolveDurationMs(0, 40));
            _ = TesseraFlyoutAnimationPolicy.ResolveFadeSampleCount()
                .Should().BeGreaterThanOrEqualTo(TesseraFlyoutAnimationPolicy.MinAniSteps);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        public void Slide_enabled_for_fast_and_fancy(int ani, bool slide)
        {
            _ = TesseraFlyoutAnimationPolicy.ShouldSlide(ani).Should().Be(slide);
        }

        [Theory]
        [InlineData(null, TesseraFlyoutAnimationPolicy.EaseOutQuart)]
        [InlineData("OutQuart", TesseraFlyoutAnimationPolicy.EaseOutQuart)]
        [InlineData("CubicInOut", TesseraFlyoutAnimationPolicy.EaseInOutCubic)]
        [InlineData("inCubic", TesseraFlyoutAnimationPolicy.EaseInCubic)]
        [InlineData("OutElastic", TesseraFlyoutAnimationPolicy.EaseOutElastic)]
        public void Ease_normalizes(string? raw, string expected)
        {
            _ = TesseraFlyoutAnimationPolicy.NormalizeEase(raw).Should().Be(expected);
        }

        [Fact]
        public void All_ease_types_count_matches_jaxcore_catalog()
        {
            _ = TesseraFlyoutAnimationPolicy.AllEaseTypes.Should().HaveCount(31);
        }

        [Fact]
        public void Defaults_match_yourflyouts_vars_inc()
        {
            _ = TesseraFlyoutAnimationPolicy.DefaultAniSteps.Should().Be(20);
            _ = TesseraFlyoutAnimationPolicy.DefaultDisplacementPx.Should().Be(30);
            _ = TesseraFlyoutAnimationPolicy.DefaultEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseOutQuart);
            _ = TesseraFlyoutAnimationPolicy.FancyPauseMs.Should().Be(100);
            _ = TesseraFlyoutAnimationPolicy.MustAnimateRenderTransform.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.MustAnimateWindowPosition.Should().BeFalse();
            _ = TesseraFlyoutAnimationPolicy.Phase1MustUseWindowPosition.Should().BeTrue();
        }

        [Fact]
        public void OutQuart_easing_reaches_endpoints()
        {
            _ = TesseraFlyoutAnimationPolicy.SampleEase(0, TesseraFlyoutAnimationPolicy.EaseOutQuart).Should().Be(0);
            _ = TesseraFlyoutAnimationPolicy.SampleEase(1, TesseraFlyoutAnimationPolicy.EaseOutQuart).Should().Be(1);
        }

        [Fact]
        public void OutQuart_at_half_matches_stepped_tween()
        {
            double stepped = TesseraFlyoutAnimationPolicy.ResolveTweenNode(
                10, 20, TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true) / 100.0;
            _ = TesseraFlyoutAnimationPolicy.SampleEase(0.5, TesseraFlyoutAnimationPolicy.EaseOutQuart)
                .Should().BeApproximately(stepped, 0.02);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(10, 50)]
        [InlineData(20, 100)]
        public void TweenNode_entrance_endpoints(int step, double expected)
        {
            _ = TesseraFlyoutAnimationPolicy.ResolveTweenNode(
                    step, 20, TesseraFlyoutAnimationPolicy.EaseLinear, entrance: true)
                .Should().Be(expected);
        }

        [Theory]
        [InlineData("Left", 0, -30, 0)]
        [InlineData("Left", 100, 0, 0)]
        [InlineData("Right", 0, 30, 0)]
        [InlineData("Top", 0, 0, -30)]
        [InlineData("Bottom", 0, 0, 30)]
        public void Slide_offset_at_tween_node_matches_func_lua(
            string dir, double tn, int dx, int dy)
        {
            (int x, int y) = TesseraFlyoutAnimationPolicy.ResolveSlideOffsetPxAtTweenNode(
                dir, 30, tn, entrance: true);
            _ = x.Should().Be(dx);
            _ = y.Should().Be(dy);
        }

        [Theory]
        [InlineData("Left", -30, 0)]
        [InlineData("Right", 30, 0)]
        [InlineData("Top", 0, -30)]
        [InlineData("Bottom", 0, 30)]
        public void Slide_offset_px_matches_yourflyouts_directions(string dir, int dx, int dy)
        {
            (int x, int y) = TesseraFlyoutAnimationPolicy.ResolveSlideOffsetPx(dir, 30);
            _ = x.Should().Be(dx);
            _ = y.Should().Be(dy);
        }

        [Fact]
        public void Fancy_entrance_sequence_includes_pause_and_phase2()
        {
            IReadOnlyList<TesseraFlyoutAnimationPolicy.FlyoutAnimationPhase> seq = TesseraFlyoutAnimationPolicy.ResolveEntranceSequence(2, showMediaStrip: true);
            _ = seq.Should().Contain(p => p.Kind == TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase1In);
            _ = seq.Should().Contain(p => p.Kind == TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Wait
                                      && p.WaitMs == 100);
            _ = seq.Should().Contain(p => p.Kind == TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase2In);
        }

        [Fact]
        public void Fancy_exit_sequence_reverses_phase_order()
        {
            IReadOnlyList<TesseraFlyoutAnimationPolicy.FlyoutAnimationPhase> seq = TesseraFlyoutAnimationPolicy.ResolveExitSequence(2, showMediaStrip: true);
            _ = seq[0].Kind.Should().Be(TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase2Out);
            _ = seq[^1].Kind.Should().Be(TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase1Out);
        }

        [Fact]
        public void Phase2_skipped_without_media_strip()
        {
            IReadOnlyList<TesseraFlyoutAnimationPolicy.FlyoutAnimationPhase> seq = TesseraFlyoutAnimationPolicy.ResolveEntranceSequence(2, showMediaStrip: false);
            _ = seq.Should().NotContain(p => p.Kind == TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase2In);
        }

        [Theory]
        [InlineData("OutQuart", "Quart", "Out")]
        [InlineData("InOutCubic", "Cubic", "InOut")]
        [InlineData("Linear", "Linear", "Out")]
        public void Split_and_compose_ease_round_trip(string ease, string family, string variant)
        {
            _ = TesseraFlyoutAnimationPolicy.SplitEase(ease).Should().Be((family, variant));
            _ = TesseraFlyoutAnimationPolicy.ComposeEase(family, variant).Should().Be(
                TesseraFlyoutAnimationPolicy.NormalizeEase(
                    ease.Equals("Linear", StringComparison.OrdinalIgnoreCase) ? "Linear" : ease));
        }

        [Fact]
        public void Fancy_entrance_duration_uses_presentation_interval()
        {
            _ = TesseraFlyoutAnimationPolicy.EncodedActionTimerIntervalMs.Should().Be(2);
            _ = TesseraFlyoutAnimationPolicy.StepPresentationIntervalMs.Should().Be(16);
            _ = TesseraFlyoutAnimationPolicy.ResolveFancyEntranceDurationMs(20).Should().Be(
                TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(20)
                + TesseraFlyoutAnimationPolicy.ResolveInterPhaseWaitMs(entrance: true)
                + TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(20)
                - TesseraFlyoutAnimationPolicy.ResolvePhase2ShowOverlapMs(
                    TesseraFlyoutAnimationPolicy.DefaultEase, 20));
        }

        [Fact]
        public void Default_phase_is_long_enough_to_tell_easing_families_apart()
        {
            int encoded = TesseraFlyoutAnimationPolicy.DefaultAniSteps
                          * TesseraFlyoutAnimationPolicy.EncodedActionTimerIntervalMs;
            _ = encoded.Should().Be(40);
            _ = encoded.Should().BeLessThan(TesseraFlyoutAnimationPolicy.MinPerceptiblePhaseDurationMs);
            _ = TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(TesseraFlyoutAnimationPolicy.DefaultAniSteps)
                .Should().BeGreaterThanOrEqualTo(TesseraFlyoutAnimationPolicy.MinPerceptiblePhaseDurationMs);
            _ = TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(20).Should().Be(320);
        }

        [Fact]
        public void InQuart_and_OutQuart_differ_at_mid_phase()
        {
            double mid = TesseraFlyoutAnimationPolicy.ResolveTweenNode(
                10, 20, TesseraFlyoutAnimationPolicy.EaseInQuart, entrance: true);
            double outMid = TesseraFlyoutAnimationPolicy.ResolveTweenNode(
                10, 20, TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
            _ = mid.Should().BeApproximately(6.25, 0.01);
            _ = outMid.Should().BeApproximately(93.75, 0.01);
            _ = mid.Should().BeLessThan(outMid);
        }

        [Theory]
        [InlineData("OutQuart", "InOutQuart")]
        [InlineData("InQuart", "InOutQuart")]
        [InlineData("InOutCubic", "InOutCubic")]
        [InlineData("Linear", "Linear")]
        [InlineData("OutCubic", "InOutCubic")]
        [InlineData("InSine", "InOutSine")]
        public void Entrance_uses_inout_so_arrival_settles(string selected, string entranceEase)
        {
            _ = TesseraFlyoutAnimationPolicy.Phase1EntranceMustUseInOutFamily.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.ResolveInOutEase(selected).Should().Be(entranceEase);
            _ = TesseraFlyoutAnimationPolicy.ResolvePhase1MotionEase(selected, entrance: true)
                .Should().Be(entranceEase);
            _ = TesseraFlyoutAnimationPolicy.ResolvePhase1MotionEase(selected, entrance: false)
                .Should().Be(TesseraFlyoutAnimationPolicy.NormalizeEase(selected));
            _ = TesseraFlyoutAnimationPolicy.ResolvePhase1OpacityEase(selected, entrance: true)
                .Should().Be(entranceEase);
            _ = TesseraFlyoutAnimationPolicy.ResolvePhase1OpacityEase(selected, entrance: false)
                .Should().Be(TesseraFlyoutAnimationPolicy.NormalizeEase(selected));
        }

        [Fact]
        public void OutQuart_show_is_quiet_early_and_settles_late()
        {
            const int steps = 20;
            double offset = 30d;
            string showEase = TesseraFlyoutAnimationPolicy.ResolvePhase1MotionEase(
                TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
            string hideEase = TesseraFlyoutAnimationPolicy.ResolvePhase1MotionEase(
                TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false);

            double showEarly = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                offset, 0, 5, steps, showEase, entrance: true);
            double showMid = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                offset, 0, 10, steps, showEase, entrance: true);
            double showLate = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                offset, 0, 15, steps, showEase, entrance: true);
            double hideX = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                0, offset, 10, steps, hideEase, entrance: false);

            _ = showEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseInOutQuart);
            _ = hideEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseOutQuart);
            _ = showEarly.Should().BeGreaterThan(offset * 0.9, "first quarter stays near the hide pose");
            _ = showMid.Should().BeApproximately(offset * 0.5, 1.0);
            _ = showLate.Should().BeLessThan(offset * 0.1, "last quarter is a settle into rest");
            _ = hideX.Should().BeLessThan(offset * 0.2, "OutQuart hide is still near rest at mid phase");
        }

        [Fact]
        public void InQuart_entrance_still_has_most_travel_in_the_last_quarter()
        {
            double remaining = 1 - TesseraFlyoutAnimationPolicy.InterpolateStepped(
                0, 1, 15, 20, TesseraFlyoutAnimationPolicy.EaseInQuart, entrance: true);
            _ = remaining.Should().BeGreaterThan(0.5);
        }

        [Fact]
        public void Entrance_last_quarter_settles_instead_of_slamming()
        {
            string showEase = TesseraFlyoutAnimationPolicy.ResolvePhase1MotionEase(
                TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
            double late = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                0, 1, 15, 20, showEase, entrance: true);
            _ = (1 - late).Should().BeLessThan(0.1);
        }

        [Fact]
        public void OutQuart_fade_in_settles_late_fade_out_stays_opaque_at_mid()
        {
            const int steps = 20;
            string showEase = TesseraFlyoutAnimationPolicy.ResolvePhase1OpacityEase(
                TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
            string hideEase = TesseraFlyoutAnimationPolicy.ResolvePhase1OpacityEase(
                TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false);

            double fadeEarly = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                0, 1, 5, steps, showEase, entrance: true);
            double fadeIn = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                0, 1, 10, steps, showEase, entrance: true);
            double fadeLate = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                0, 1, 15, steps, showEase, entrance: true);
            double fadeOut = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                1, 0, 10, steps, hideEase, entrance: false);

            _ = fadeEarly.Should().BeLessThan(0.1);
            _ = fadeIn.Should().BeApproximately(0.5, 0.05);
            _ = fadeLate.Should().BeGreaterThan(0.9);
            _ = fadeOut.Should().BeGreaterThan(0.8);
        }

        [Fact]
        public void Invert_ease_round_trips_every_catalog_id()
        {
            foreach (string ease in TesseraFlyoutAnimationPolicy.AllEaseTypes)
            {
                string inverted = TesseraFlyoutAnimationPolicy.InvertEaseVariant(ease);
                _ = TesseraFlyoutAnimationPolicy.InvertEaseVariant(inverted)
                    .Should().Be(ease, because: ease);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(20)]
        public void Linear_fade_in_plus_fade_out_is_one(int step)
        {
            double fadeIn = TesseraFlyoutAnimationPolicy.InterpolateForward(
                0, 1, step, 20, TesseraFlyoutAnimationPolicy.EaseLinear);
            double fadeOut = TesseraFlyoutAnimationPolicy.InterpolateForward(
                1, 0, step, 20, TesseraFlyoutAnimationPolicy.EaseLinear);
            _ = (fadeIn + fadeOut).Should().BeApproximately(1, 0.001);
        }

        [Fact]
        public void Single_step_fade_reaches_both_endpoints()
        {
            _ = TesseraFlyoutAnimationPolicy.InterpolateForward(
                    0, 1, 1, 1, TesseraFlyoutAnimationPolicy.EaseLinear)
                .Should().BeApproximately(1, 0.01);
            _ = TesseraFlyoutAnimationPolicy.InterpolateForward(
                    1, 0, 1, 1, TesseraFlyoutAnimationPolicy.EaseLinear)
                .Should().BeApproximately(0, 0.01);
        }

        [Fact]
        public void Exit_phase1_starts_at_full_tween_node()
        {
            _ = TesseraFlyoutAnimationPolicy.ResolveTweenNode(
                    0, 20, TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false)
                .Should().BeApproximately(100, 0.01);
        }

        [Fact]
        public void Exit_must_mirror_entrance()
        {
            _ = TesseraFlyoutAnimationPolicy.ExitMustMirrorEntrance.Should().BeTrue();
        }

        [Fact]
        public void SampleEaseContinuous_distinguishes_in_from_out_at_midpoint()
        {
            double inMid = TesseraFlyoutAnimationPolicy.SampleEaseContinuous(
                0.5, TesseraFlyoutAnimationPolicy.EaseInQuart);
            double outMid = TesseraFlyoutAnimationPolicy.SampleEaseContinuous(
                0.5, TesseraFlyoutAnimationPolicy.EaseOutQuart);
            _ = inMid.Should().BeApproximately(0.0625, 0.001);
            _ = outMid.Should().BeApproximately(0.9375, 0.001);
            _ = inMid.Should().BeLessThan(outMid);
        }

        [Theory]
        [InlineData(20)]
        [InlineData(10)]
        [InlineData(40)]
        public void SampleEaseContinuous_aligns_with_stepped_ResolveTweenNode_at_step_boundaries(int aniSteps)
        {
            foreach (string ease in TesseraFlyoutAnimationPolicy.AllEaseTypes)
            {
                for (int step = 0; step <= aniSteps; step++)
                {
                    double t = (double)step / aniSteps;
                    double continuous = TesseraFlyoutAnimationPolicy.SampleEaseContinuous(t, ease);
                    double stepped = TesseraFlyoutAnimationPolicy.ResolveTweenNode(
                        step, aniSteps, ease, entrance: true) / 100.0;
                    _ = continuous.Should().BeApproximately(
                        stepped, 1e-9,
                        $"ease '{ease}' at step {step}/{aniSteps} should agree between continuous and stepped");
                }
            }
        }

        [Fact]
        public void Stepped_progress_differs_for_in_vs_out_at_same_step()
        {
            double inMid = TesseraFlyoutAnimationPolicy.ResolveNormalizedTweenProgress(
                10, 20, TesseraFlyoutAnimationPolicy.EaseInQuart, entrance: true);
            double outMid = TesseraFlyoutAnimationPolicy.ResolveNormalizedTweenProgress(
                10, 20, TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
            _ = inMid.Should().BeLessThan(outMid);
            _ = (outMid - inMid).Should().BeGreaterThan(0.3);
        }

        [Fact]
        public void Exit_opacity_interpolation_starts_at_full()
        {
            _ = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                    1, 0, 0, 20, TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false)
                .Should().BeApproximately(1, 0.01);
            _ = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                    1, 0, 20, 20, TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false)
                .Should().BeApproximately(0, 0.01);
        }

        [Fact]
        public void Fancy_entrance_sequence_matches_ani2_inc()
        {
            IReadOnlyList<TesseraFlyoutAnimationPolicy.FlyoutAnimationPhase> seq = TesseraFlyoutAnimationPolicy.ResolveEntranceSequence(ani: 2, showMediaStrip: true);
            _ = seq.Select(p => p.Kind).Should().Equal([
                TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Show,
                TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase1In,
                TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Wait,
                TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase2In,
            ]);
            _ = seq[1].StepCount.Should().Be(20);
            _ = seq[2].WaitMs.Should().Be(TesseraFlyoutAnimationPolicy.FancyPauseMs);
            _ = seq[3].StepCount.Should().Be(20);
        }

        [Fact]
        public void Fancy_exit_sequence_reverses_entrance()
        {
            IReadOnlyList<TesseraFlyoutAnimationPolicy.FlyoutAnimationPhase> seq = TesseraFlyoutAnimationPolicy.ResolveExitSequence(ani: 2, showMediaStrip: true);
            _ = seq.Select(p => p.Kind).Should().Equal([
                TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase2Out,
                TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Wait,
                TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase1Out,
            ]);
            _ = seq[0].StepCount.Should().Be(20);
            _ = seq[1].WaitMs.Should().Be(TesseraFlyoutAnimationPolicy.FancyPauseMs);
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(true, true, true)]
        [InlineData(false, true, false)]
        public void Relayout_deferred_for_entire_motion_including_phase2(bool motion, bool phase2, bool defer)
        {
            _ = TesseraFlyoutAnimationPolicy.Phase2MustNotResizeHwnd.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.RelayoutAllowedDuringPhase2.Should().BeFalse();
            _ = TesseraFlyoutAnimationPolicy.Phase2MustNotMutateLayoutMeasure.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.ShouldDeferRelayoutDuringMotion(motion, phase2)
                .Should().Be(defer);
        }

        [Fact]
        public void Cancelled_entrance_must_snap_to_rest_and_clear_motion_on_supersede()
        {
            _ = TesseraFlyoutAnimationPolicy.CancelledEntranceMustSnapToRest.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.MotionAnimatingMustClearOnSupersede.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.SupersededMotionMustCancelInFlightTweens.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.SteppedKeyframesMustUseLinearInterpolation.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.IdleShowingSessionMustSnapRevealToRest.Should().BeTrue();
        }

        [Fact]
        public void Square_phase2_runs_without_media_strip()
        {
            _ = TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(2, "Square", showMediaStrip: false)
                .Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(2, "Fluent", showMediaStrip: false)
                .Should().BeFalse();
            _ = TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(1, "Square", showMediaStrip: false)
                .Should().BeFalse();
        }

        [Theory]
        [InlineData("OutQuart", "InOutQuart", "OutQuart")]
        [InlineData("OutCubic", "InOutCubic", "OutCubic")]
        [InlineData("OutExpo", "InOutExpo", "OutExpo")]
        [InlineData("InCubic", "InCubic", "OutCubic")]
        [InlineData("InQuart", "InQuart", "OutQuart")]
        [InlineData("InOutSine", "InOutSine", "InOutSine")]
        [InlineData("Linear", "Linear", "Linear")]
        public void Phase2_show_uses_in_or_inout_hide_uses_out_or_inout(
            string selected, string showEase, string hideEase)
        {
            _ = TesseraFlyoutAnimationPolicy.Phase2ShowMustUseInOrInOutFamily.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.Phase2HideMustUseOutOrInOutFamily.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.Phase2ShowMustNotInvertToInEase.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(selected, entrance: true)
                .Should().Be(showEase);
            _ = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(selected, entrance: false)
                .Should().Be(hideEase);
        }

        [Fact]
        public void Phase2_directional_variants_cover_every_ease_family()
        {
            foreach (string selected in TesseraFlyoutAnimationPolicy.AllEaseTypes)
            {
                string show = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(selected, entrance: true);
                string hide = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(selected, entrance: false);
                if (show.Equals(TesseraFlyoutAnimationPolicy.EaseLinear, StringComparison.OrdinalIgnoreCase))
                {
                    _ = hide.Should().Be(TesseraFlyoutAnimationPolicy.EaseLinear);
                    continue;
                }

                (string _, string? showVariant) = TesseraFlyoutAnimationPolicy.SplitEase(show);
                (string _, string? hideVariant) = TesseraFlyoutAnimationPolicy.SplitEase(hide);
                (string _, string? selectedVariant) = TesseraFlyoutAnimationPolicy.SplitEase(selected);
                _ = showVariant.Should().BeOneOf("In", "InOut");
                _ = hideVariant.Should().BeOneOf("Out", "InOut");
                if (selectedVariant.Equals("Out", StringComparison.OrdinalIgnoreCase))
                {
                    _ = show.Should().Be(TesseraFlyoutAnimationPolicy.ResolveInOutEase(selected));
                    _ = show.Should().NotBe(TesseraFlyoutAnimationPolicy.InvertEaseVariant(selected));
                    _ = hide.Should().Be(TesseraFlyoutAnimationPolicy.NormalizeEase(selected));
                }
                else if (selectedVariant.Equals("In", StringComparison.OrdinalIgnoreCase))
                {
                    _ = show.Should().Be(TesseraFlyoutAnimationPolicy.NormalizeEase(selected));
                    _ = hide.Should().Be(TesseraFlyoutAnimationPolicy.ResolveOutEase(selected));
                }
                else
                {
                    _ = show.Should().Be(hide);
                    _ = show.Should().Be(TesseraFlyoutAnimationPolicy.NormalizeEase(selected));
                }
            }
        }

        [Fact]
        public void Phase2_In_keep_differs_from_phase1_InOut_remap()
        {
            _ = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase("InCubic", entrance: true)
                .Should().Be(TesseraFlyoutAnimationPolicy.EaseInCubic);
            _ = TesseraFlyoutAnimationPolicy.ResolvePhase1MotionEase("InCubic", entrance: true)
                .Should().Be(TesseraFlyoutAnimationPolicy.EaseInOutCubic);
            _ = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase("OutQuart", entrance: true)
                .Should().Be(TesseraFlyoutAnimationPolicy.ResolvePhase1MotionEase("OutQuart", entrance: true));
        }

        [Fact]
        public void Phase2_OutQuart_show_spends_time_in_mid_band_while_hide_lingers()
        {
            const int steps = 20;
            string showEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
                TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
            string hideEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
                TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false);

            _ = showEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseInOutQuart);
            _ = hideEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseOutQuart);

            double showEarly = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
                entrance: true, 5, steps, showEase);
            double outQuartEarly = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                0, 1, 5, steps, TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
            _ = showEarly.Should().BeLessThan(0.5, "Out* forward is a start-slam; show remaps to InOut");
            _ = outQuartEarly.Should().BeGreaterThan(0.5);

            int midBand = 0;
            for (int s = 0; s <= steps; s++)
            {
                double p = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
                    entrance: true, s, steps, showEase);
                if (p is >= TesseraFlyoutAnimationPolicy.Phase2ShowMidBandMin
                    and <= TesseraFlyoutAnimationPolicy.Phase2ShowMidBandMax)
                {
                    midBand++;
                }
            }

            _ = (midBand / (double)(steps + 1)).Should().BeGreaterThanOrEqualTo(
                TesseraFlyoutAnimationPolicy.Phase2ShowMidBandMinDurationFraction);

            double hideEarly = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
                entrance: false, 5, steps, hideEase);
            _ = hideEarly.Should().BeGreaterThan(0.9);
        }

        [Fact]
        public void Show_phase2_overlaps_phase1_until_mid_band_and_skips_pause()
        {
            _ = TesseraFlyoutAnimationPolicy.ShowMustSkipFancyPauseBeforePhase2.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.ShowPhase2MustOverlapUntilMidBand.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.ResolveInterPhaseWaitMs(entrance: true).Should().Be(0);
            _ = TesseraFlyoutAnimationPolicy.ResolveInterPhaseWaitMs(entrance: false)
                .Should().Be(TesseraFlyoutAnimationPolicy.FancyPauseMs);

            const int steps = 25;
            int overlap = TesseraFlyoutAnimationPolicy.ResolvePhase2ShowOverlapMs(
                TesseraFlyoutAnimationPolicy.EaseOutQuint, steps);
            int lead = TesseraFlyoutAnimationPolicy.ResolveShowPhase2LeadDelayMs(
                TesseraFlyoutAnimationPolicy.EaseOutQuint, steps);
            int phaseMs = TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(steps);
            string showEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
                TesseraFlyoutAnimationPolicy.EaseOutQuint, entrance: true);
            int overlapSteps = overlap / TesseraFlyoutAnimationPolicy.StepPresentationIntervalMs;
            double p = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
                entrance: true, overlapSteps, steps, showEase);

            _ = overlap.Should().BeGreaterThan(100);
            _ = lead.Should().Be(phaseMs - overlap);
            _ = p.Should().BeGreaterThanOrEqualTo(TesseraFlyoutAnimationPolicy.Phase2ShowMidBandMin);
        }

        [Theory]
        [InlineData(TesseraStackedPanelRole.Volume, true, true)]
        [InlineData(TesseraStackedPanelRole.Device, true, true)]
        [InlineData(TesseraStackedPanelRole.Media, true, false)]
        [InlineData(TesseraStackedPanelRole.Volume, false, true)]
        [InlineData(TesseraStackedPanelRole.Device, false, true)]
        [InlineData(TesseraStackedPanelRole.Media, false, true)]
        public void Stacked_phase1_skips_media_on_show_only(
            TesseraStackedPanelRole role, bool entrance, bool expected)
        {
            _ = TesseraFlyoutAnimationPolicy.StackedMediaMustSkipPhase1OnShow.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.StackedShowMustFinishVolumePhase1BeforeMediaPhase2
                .Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.StackedHideMustFinishPhase2BeforeAnySlotPhase1
                .Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.ShouldRunPhase1(
                    ani: 2,
                    styleId: "Fluent",
                    showMediaStrip: true,
                    stackedRole: role,
                    entrance: entrance)
                .Should().Be(expected);
        }

        [Fact]
        public void Single_hwnd_and_ani0_media_still_run_phase1_on_show()
        {
            _ = TesseraFlyoutAnimationPolicy.ShouldRunPhase1(
                    ani: 2, "Fluent", showMediaStrip: true, stackedRole: null, entrance: true)
                .Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.ShouldRunPhase1(
                    ani: 0,
                    "Fluent",
                    showMediaStrip: true,
                    stackedRole: TesseraStackedPanelRole.Media,
                    entrance: true)
                .Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.ShouldHoldStackedMediaHiddenThroughShowPhase1(
                    ani: 2, "Fluent", showMediaStrip: true, TesseraStackedPanelRole.Media)
                .Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.ShouldHoldStackedMediaHiddenThroughShowPhase1(
                    ani: 2, "Fluent", showMediaStrip: true, TesseraStackedPanelRole.Volume)
                .Should().BeFalse();
            _ = TesseraFlyoutAnimationPolicy.PreparePhase2ShowMustUnhideStackedMedia.Should().BeTrue();
            _ = TesseraFlyoutHwndRegionSpec.ShouldZeroWindowOpacityForCollapsedRegion(
                    stackedMedia: true, hideChrome: true, showMotionActive: true)
                .Should().BeFalse();
        }

        [Fact]
        public void Phase2_OutQuart_hide_lingers_on_the_reverse_clock()
        {
            const int steps = 20;
            string showEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
                TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
            string hideEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
                TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false);
            _ = showEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseInOutQuart);
            _ = hideEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseOutQuart);
            _ = TesseraFlyoutAnimationPolicy.Phase2MustSyncRevealRegionBeforeTweenBothWays.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.Phase2HideMustStartFromRestReveal.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.Phase2HideMustNotSnapMissingHostsToRest.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.Phase2ShowMustNotSnapMissingHostsToRest.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.Phase2MustPumpEachAniStepOnDispatcher.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.PreparePhase2ShowMustYieldForRender.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.ShouldSnapMissingPhase2HostsToRest(entrance: true).Should().BeFalse();
            _ = TesseraFlyoutAnimationPolicy.ShouldSnapMissingPhase2HostsToRest(entrance: false).Should().BeFalse();

            int distinctHideTicks = 0;
            double previous = double.NaN;
            for (int s = 0; s <= steps; s++)
            {
                double hide = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
                    entrance: false, s, steps, hideEase);
                if (double.IsNaN(previous) || Math.Abs(hide - previous) > 0.01)
                {
                    distinctHideTicks++;
                }

                previous = hide;
            }

            double hideEarly = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
                entrance: false, 5, steps, hideEase);
            _ = hideEarly.Should().BeGreaterThan(0.9);
            _ = distinctHideTicks.Should().BeGreaterThan(10, "skipping to collapsed is not a wipe");
        }

        [Fact]
        public void Stacked_phase2_still_runs_on_volume_for_gnome_fill()
        {
            _ = TesseraFlyoutAnimationPolicy.StackedPhase2MustNotBeMediaSlotOnly.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.ShouldRunPhase2Reveal(
                    2, StyleIds.Gnome, showMediaStrip: true, TesseraStackedPanelRole.Volume)
                .Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.ShouldRunPhase2Reveal(
                    2, StyleIds.Gnome, showMediaStrip: true, TesseraStackedPanelRole.Media)
                .Should().BeTrue();
        }

        [Fact]
        public void MaterialYou_phase2_only_runs_on_volume_slot_for_in_layout_media()
        {
            _ = TesseraFlyoutTweenTargetCatalog.StylePhase2UsesInLayoutMedia(StyleIds.MaterialYou).Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.ShouldRunPhase2Reveal(
                    2, StyleIds.MaterialYou, showMediaStrip: true, TesseraStackedPanelRole.Volume)
                .Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.ShouldRunPhase2Reveal(
                    2, StyleIds.MaterialYou, showMediaStrip: true, TesseraStackedPanelRole.Media)
                .Should().BeFalse();
        }
    }
}
