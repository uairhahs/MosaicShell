using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

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
    public void Duration_matches_yourflyouts_step_timing(int ani, int steps, int ms) =>
        TesseraFlyoutAnimationPolicy.ResolveDurationMs(ani, steps).Should().Be(ms);

    [Fact]
    public void Ani0_uses_rainmeter_fade_duration_and_ignores_ani_steps()
    {
        TesseraFlyoutAnimationPolicy.AniNoneUsesBuiltInFade.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.AniNoneIgnoresAniSteps.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.FadeDurationMs.Should().Be(250);
        TesseraFlyoutAnimationPolicy.ResolveDurationMs(0, 10)
            .Should().Be(TesseraFlyoutAnimationPolicy.ResolveDurationMs(0, 40));
        TesseraFlyoutAnimationPolicy.ResolveFadeSampleCount()
            .Should().BeGreaterThanOrEqualTo(TesseraFlyoutAnimationPolicy.MinAniSteps);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void Slide_enabled_for_fast_and_fancy(int ani, bool slide) =>
        TesseraFlyoutAnimationPolicy.ShouldSlide(ani).Should().Be(slide);

    [Theory]
    [InlineData(null, TesseraFlyoutAnimationPolicy.EaseOutQuart)]
    [InlineData("OutQuart", TesseraFlyoutAnimationPolicy.EaseOutQuart)]
    [InlineData("CubicInOut", TesseraFlyoutAnimationPolicy.EaseInOutCubic)]
    [InlineData("inCubic", TesseraFlyoutAnimationPolicy.EaseInCubic)]
    [InlineData("OutElastic", TesseraFlyoutAnimationPolicy.EaseOutElastic)]
    public void Ease_normalizes(string? raw, string expected) =>
        TesseraFlyoutAnimationPolicy.NormalizeEase(raw).Should().Be(expected);

    [Fact]
    public void All_ease_types_count_matches_jaxcore_catalog() =>
        TesseraFlyoutAnimationPolicy.AllEaseTypes.Should().HaveCount(31);

    [Fact]
    public void Defaults_match_yourflyouts_vars_inc()
    {
        TesseraFlyoutAnimationPolicy.DefaultAniSteps.Should().Be(20);
        TesseraFlyoutAnimationPolicy.DefaultDisplacementPx.Should().Be(30);
        TesseraFlyoutAnimationPolicy.DefaultEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseOutQuart);
        TesseraFlyoutAnimationPolicy.FancyPauseMs.Should().Be(100);
        TesseraFlyoutAnimationPolicy.MustAnimateRenderTransform.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.MustAnimateWindowPosition.Should().BeFalse();
        TesseraFlyoutAnimationPolicy.Phase1MustUseWindowPosition.Should().BeTrue();
    }

    [Fact]
    public void OutQuart_easing_reaches_endpoints()
    {
        TesseraFlyoutAnimationPolicy.SampleEase(0, TesseraFlyoutAnimationPolicy.EaseOutQuart).Should().Be(0);
        TesseraFlyoutAnimationPolicy.SampleEase(1, TesseraFlyoutAnimationPolicy.EaseOutQuart).Should().Be(1);
    }

    [Fact]
    public void OutQuart_at_half_matches_stepped_tween()
    {
        var stepped = TesseraFlyoutAnimationPolicy.ResolveTweenNode(
            10, 20, TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true) / 100.0;
        TesseraFlyoutAnimationPolicy.SampleEase(0.5, TesseraFlyoutAnimationPolicy.EaseOutQuart)
            .Should().BeApproximately(stepped, 0.02);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(10, 50)]
    [InlineData(20, 100)]
    public void TweenNode_entrance_endpoints(int step, double expected)
    {
        TesseraFlyoutAnimationPolicy.ResolveTweenNode(
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
        var (x, y) = TesseraFlyoutAnimationPolicy.ResolveSlideOffsetPxAtTweenNode(
            dir, 30, tn, entrance: true);
        x.Should().Be(dx);
        y.Should().Be(dy);
    }

    [Theory]
    [InlineData("Left", -30, 0)]
    [InlineData("Right", 30, 0)]
    [InlineData("Top", 0, -30)]
    [InlineData("Bottom", 0, 30)]
    public void Slide_offset_px_matches_yourflyouts_directions(string dir, int dx, int dy)
    {
        var (x, y) = TesseraFlyoutAnimationPolicy.ResolveSlideOffsetPx(dir, 30);
        x.Should().Be(dx);
        y.Should().Be(dy);
    }

    [Fact]
    public void Fancy_entrance_sequence_includes_pause_and_phase2()
    {
        var seq = TesseraFlyoutAnimationPolicy.ResolveEntranceSequence(2, showMediaStrip: true);
        seq.Should().Contain(p => p.Kind == TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase1In);
        seq.Should().Contain(p => p.Kind == TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Wait
                                  && p.WaitMs == 100);
        seq.Should().Contain(p => p.Kind == TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase2In);
    }

    [Fact]
    public void Fancy_exit_sequence_reverses_phase_order()
    {
        var seq = TesseraFlyoutAnimationPolicy.ResolveExitSequence(2, showMediaStrip: true);
        seq[0].Kind.Should().Be(TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase2Out);
        seq[^1].Kind.Should().Be(TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase1Out);
    }

    [Fact]
    public void Phase2_skipped_without_media_strip()
    {
        var seq = TesseraFlyoutAnimationPolicy.ResolveEntranceSequence(2, showMediaStrip: false);
        seq.Should().NotContain(p => p.Kind == TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase2In);
    }

    [Theory]
    [InlineData("OutQuart", "Quart", "Out")]
    [InlineData("InOutCubic", "Cubic", "InOut")]
    [InlineData("Linear", "Linear", "Out")]
    public void Split_and_compose_ease_round_trip(string ease, string family, string variant)
    {
        TesseraFlyoutAnimationPolicy.SplitEase(ease).Should().Be((family, variant));
        TesseraFlyoutAnimationPolicy.ComposeEase(family, variant).Should().Be(
            TesseraFlyoutAnimationPolicy.NormalizeEase(
                ease.Equals("Linear", StringComparison.OrdinalIgnoreCase) ? "Linear" : ease));
    }

    [Fact]
    public void Fancy_entrance_duration_uses_presentation_interval()
    {
        TesseraFlyoutAnimationPolicy.EncodedActionTimerIntervalMs.Should().Be(2);
        TesseraFlyoutAnimationPolicy.StepPresentationIntervalMs.Should().Be(16);
        TesseraFlyoutAnimationPolicy.ResolveFancyEntranceDurationMs(20).Should().Be(
            TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(20)
            + TesseraFlyoutAnimationPolicy.ResolveInterPhaseWaitMs(entrance: true)
            + TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(20)
            - TesseraFlyoutAnimationPolicy.ResolvePhase2ShowOverlapMs(
                TesseraFlyoutAnimationPolicy.DefaultEase, 20));
    }

    [Fact]
    public void Default_phase_is_long_enough_to_tell_easing_families_apart()
    {
        var encoded = TesseraFlyoutAnimationPolicy.DefaultAniSteps
                      * TesseraFlyoutAnimationPolicy.EncodedActionTimerIntervalMs;
        encoded.Should().Be(40);
        encoded.Should().BeLessThan(TesseraFlyoutAnimationPolicy.MinPerceptiblePhaseDurationMs);
        TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(TesseraFlyoutAnimationPolicy.DefaultAniSteps)
            .Should().BeGreaterThanOrEqualTo(TesseraFlyoutAnimationPolicy.MinPerceptiblePhaseDurationMs);
        TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(20).Should().Be(320);
    }

    [Fact]
    public void InQuart_and_OutQuart_differ_at_mid_phase()
    {
        var mid = TesseraFlyoutAnimationPolicy.ResolveTweenNode(
            10, 20, TesseraFlyoutAnimationPolicy.EaseInQuart, entrance: true);
        var outMid = TesseraFlyoutAnimationPolicy.ResolveTweenNode(
            10, 20, TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
        mid.Should().BeApproximately(6.25, 0.01);
        outMid.Should().BeApproximately(93.75, 0.01);
        mid.Should().BeLessThan(outMid);
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
        TesseraFlyoutAnimationPolicy.Phase1EntranceMustUseInOutFamily.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.ResolveInOutEase(selected).Should().Be(entranceEase);
        TesseraFlyoutAnimationPolicy.ResolvePhase1MotionEase(selected, entrance: true)
            .Should().Be(entranceEase);
        TesseraFlyoutAnimationPolicy.ResolvePhase1MotionEase(selected, entrance: false)
            .Should().Be(TesseraFlyoutAnimationPolicy.NormalizeEase(selected));
        TesseraFlyoutAnimationPolicy.ResolvePhase1OpacityEase(selected, entrance: true)
            .Should().Be(entranceEase);
        TesseraFlyoutAnimationPolicy.ResolvePhase1OpacityEase(selected, entrance: false)
            .Should().Be(TesseraFlyoutAnimationPolicy.NormalizeEase(selected));
    }

    [Fact]
    public void OutQuart_show_is_quiet_early_and_settles_late()
    {
        const int steps = 20;
        var offset = 30d;
        var showEase = TesseraFlyoutAnimationPolicy.ResolvePhase1MotionEase(
            TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
        var hideEase = TesseraFlyoutAnimationPolicy.ResolvePhase1MotionEase(
            TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false);

        var showEarly = TesseraFlyoutAnimationPolicy.InterpolateStepped(
            offset, 0, 5, steps, showEase, entrance: true);
        var showMid = TesseraFlyoutAnimationPolicy.InterpolateStepped(
            offset, 0, 10, steps, showEase, entrance: true);
        var showLate = TesseraFlyoutAnimationPolicy.InterpolateStepped(
            offset, 0, 15, steps, showEase, entrance: true);
        var hideX = TesseraFlyoutAnimationPolicy.InterpolateStepped(
            0, offset, 10, steps, hideEase, entrance: false);

        showEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseInOutQuart);
        hideEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseOutQuart);
        showEarly.Should().BeGreaterThan(offset * 0.9, "first quarter stays near the hide pose");
        showMid.Should().BeApproximately(offset * 0.5, 1.0);
        showLate.Should().BeLessThan(offset * 0.1, "last quarter is a settle into rest");
        hideX.Should().BeLessThan(offset * 0.2, "OutQuart hide is still near rest at mid phase");
    }

    [Fact]
    public void InQuart_entrance_still_has_most_travel_in_the_last_quarter()
    {
        var remaining = 1 - TesseraFlyoutAnimationPolicy.InterpolateStepped(
            0, 1, 15, 20, TesseraFlyoutAnimationPolicy.EaseInQuart, entrance: true);
        remaining.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void Entrance_last_quarter_settles_instead_of_slamming()
    {
        var showEase = TesseraFlyoutAnimationPolicy.ResolvePhase1MotionEase(
            TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
        var late = TesseraFlyoutAnimationPolicy.InterpolateStepped(
            0, 1, 15, 20, showEase, entrance: true);
        (1 - late).Should().BeLessThan(0.1);
    }

    [Fact]
    public void OutQuart_fade_in_settles_late_fade_out_stays_opaque_at_mid()
    {
        const int steps = 20;
        var showEase = TesseraFlyoutAnimationPolicy.ResolvePhase1OpacityEase(
            TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
        var hideEase = TesseraFlyoutAnimationPolicy.ResolvePhase1OpacityEase(
            TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false);

        var fadeEarly = TesseraFlyoutAnimationPolicy.InterpolateStepped(
            0, 1, 5, steps, showEase, entrance: true);
        var fadeIn = TesseraFlyoutAnimationPolicy.InterpolateStepped(
            0, 1, 10, steps, showEase, entrance: true);
        var fadeLate = TesseraFlyoutAnimationPolicy.InterpolateStepped(
            0, 1, 15, steps, showEase, entrance: true);
        var fadeOut = TesseraFlyoutAnimationPolicy.InterpolateStepped(
            1, 0, 10, steps, hideEase, entrance: false);

        fadeEarly.Should().BeLessThan(0.1);
        fadeIn.Should().BeApproximately(0.5, 0.05);
        fadeLate.Should().BeGreaterThan(0.9);
        fadeOut.Should().BeGreaterThan(0.8);
    }

    [Fact]
    public void Invert_ease_round_trips_every_catalog_id()
    {
        foreach (var ease in TesseraFlyoutAnimationPolicy.AllEaseTypes)
        {
            var inverted = TesseraFlyoutAnimationPolicy.InvertEaseVariant(ease);
            TesseraFlyoutAnimationPolicy.InvertEaseVariant(inverted)
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
        var fadeIn = TesseraFlyoutAnimationPolicy.InterpolateForward(
            0, 1, step, 20, TesseraFlyoutAnimationPolicy.EaseLinear);
        var fadeOut = TesseraFlyoutAnimationPolicy.InterpolateForward(
            1, 0, step, 20, TesseraFlyoutAnimationPolicy.EaseLinear);
        (fadeIn + fadeOut).Should().BeApproximately(1, 0.001);
    }

    [Fact]
    public void Single_step_fade_reaches_both_endpoints()
    {
        TesseraFlyoutAnimationPolicy.InterpolateForward(
                0, 1, 1, 1, TesseraFlyoutAnimationPolicy.EaseLinear)
            .Should().BeApproximately(1, 0.01);
        TesseraFlyoutAnimationPolicy.InterpolateForward(
                1, 0, 1, 1, TesseraFlyoutAnimationPolicy.EaseLinear)
            .Should().BeApproximately(0, 0.01);
    }

    [Fact]
    public void Exit_phase1_starts_at_full_tween_node()
    {
        TesseraFlyoutAnimationPolicy.ResolveTweenNode(
                0, 20, TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false)
            .Should().BeApproximately(100, 0.01);
    }

    [Fact]
    public void Exit_must_mirror_entrance() =>
        TesseraFlyoutAnimationPolicy.ExitMustMirrorEntrance.Should().BeTrue();

    [Fact]
    public void SampleEaseContinuous_distinguishes_in_from_out_at_midpoint()
    {
        var inMid = TesseraFlyoutAnimationPolicy.SampleEaseContinuous(
            0.5, TesseraFlyoutAnimationPolicy.EaseInQuart);
        var outMid = TesseraFlyoutAnimationPolicy.SampleEaseContinuous(
            0.5, TesseraFlyoutAnimationPolicy.EaseOutQuart);
        inMid.Should().BeApproximately(0.0625, 0.001);
        outMid.Should().BeApproximately(0.9375, 0.001);
        inMid.Should().BeLessThan(outMid);
    }

    [Fact]
    public void Stepped_progress_differs_for_in_vs_out_at_same_step()
    {
        var inMid = TesseraFlyoutAnimationPolicy.ResolveNormalizedTweenProgress(
            10, 20, TesseraFlyoutAnimationPolicy.EaseInQuart, entrance: true);
        var outMid = TesseraFlyoutAnimationPolicy.ResolveNormalizedTweenProgress(
            10, 20, TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
        inMid.Should().BeLessThan(outMid);
        (outMid - inMid).Should().BeGreaterThan(0.3);
    }

    [Fact]
    public void Exit_opacity_interpolation_starts_at_full()
    {
        TesseraFlyoutAnimationPolicy.InterpolateStepped(
                1, 0, 0, 20, TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false)
            .Should().BeApproximately(1, 0.01);
        TesseraFlyoutAnimationPolicy.InterpolateStepped(
                1, 0, 20, 20, TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false)
            .Should().BeApproximately(0, 0.01);
    }

    [Fact]
    public void Fancy_entrance_sequence_matches_ani2_inc()
    {
        var seq = TesseraFlyoutAnimationPolicy.ResolveEntranceSequence(ani: 2, showMediaStrip: true);
        seq.Select(p => p.Kind).Should().Equal([
            TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Show,
            TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase1In,
            TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Wait,
            TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase2In,
        ]);
        seq[1].StepCount.Should().Be(20);
        seq[2].WaitMs.Should().Be(TesseraFlyoutAnimationPolicy.FancyPauseMs);
        seq[3].StepCount.Should().Be(20);
    }

    [Fact]
    public void Fancy_exit_sequence_reverses_entrance()
    {
        var seq = TesseraFlyoutAnimationPolicy.ResolveExitSequence(ani: 2, showMediaStrip: true);
        seq.Select(p => p.Kind).Should().Equal([
            TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase2Out,
            TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Wait,
            TesseraFlyoutAnimationPolicy.FlyoutAnimationPhaseKind.Phase1Out,
        ]);
        seq[0].StepCount.Should().Be(20);
        seq[1].WaitMs.Should().Be(TesseraFlyoutAnimationPolicy.FancyPauseMs);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    public void Relayout_deferred_for_entire_motion_including_phase2(bool motion, bool phase2, bool defer)
    {
        TesseraFlyoutAnimationPolicy.Phase2MustNotResizeHwnd.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.RelayoutAllowedDuringPhase2.Should().BeFalse();
        TesseraFlyoutAnimationPolicy.Phase2MustNotMutateLayoutMeasure.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.ShouldDeferRelayoutDuringMotion(motion, phase2)
            .Should().Be(defer);
    }

    [Fact]
    public void Cancelled_entrance_must_snap_to_rest_and_clear_motion_on_supersede()
    {
        TesseraFlyoutAnimationPolicy.CancelledEntranceMustSnapToRest.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.MotionAnimatingMustClearOnSupersede.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.SupersededMotionMustCancelInFlightTweens.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.SteppedKeyframesMustUseLinearInterpolation.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.IdleShowingSessionMustSnapRevealToRest.Should().BeTrue();
    }

    [Fact]
    public void Square_phase2_runs_without_media_strip()
    {
        TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(2, "Square", showMediaStrip: false)
            .Should().BeTrue();
        TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(2, "Fluent", showMediaStrip: false)
            .Should().BeFalse();
        TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(1, "Square", showMediaStrip: false)
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
        TesseraFlyoutAnimationPolicy.Phase2ShowMustUseInOrInOutFamily.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.Phase2HideMustUseOutOrInOutFamily.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.Phase2ShowMustNotInvertToInEase.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(selected, entrance: true)
            .Should().Be(showEase);
        TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(selected, entrance: false)
            .Should().Be(hideEase);
    }

    [Fact]
    public void Phase2_directional_variants_cover_every_ease_family()
    {
        foreach (var selected in TesseraFlyoutAnimationPolicy.AllEaseTypes)
        {
            var show = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(selected, entrance: true);
            var hide = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(selected, entrance: false);
            if (show.Equals(TesseraFlyoutAnimationPolicy.EaseLinear, StringComparison.OrdinalIgnoreCase))
            {
                hide.Should().Be(TesseraFlyoutAnimationPolicy.EaseLinear);
                continue;
            }

            var (_, showVariant) = TesseraFlyoutAnimationPolicy.SplitEase(show);
            var (_, hideVariant) = TesseraFlyoutAnimationPolicy.SplitEase(hide);
            var (_, selectedVariant) = TesseraFlyoutAnimationPolicy.SplitEase(selected);
            showVariant.Should().BeOneOf("In", "InOut");
            hideVariant.Should().BeOneOf("Out", "InOut");
            if (selectedVariant.Equals("Out", StringComparison.OrdinalIgnoreCase))
            {
                show.Should().Be(TesseraFlyoutAnimationPolicy.ResolveInOutEase(selected));
                show.Should().NotBe(TesseraFlyoutAnimationPolicy.InvertEaseVariant(selected));
                hide.Should().Be(TesseraFlyoutAnimationPolicy.NormalizeEase(selected));
            }
            else if (selectedVariant.Equals("In", StringComparison.OrdinalIgnoreCase))
            {
                show.Should().Be(TesseraFlyoutAnimationPolicy.NormalizeEase(selected));
                hide.Should().Be(TesseraFlyoutAnimationPolicy.ResolveOutEase(selected));
            }
            else
            {
                show.Should().Be(hide);
                show.Should().Be(TesseraFlyoutAnimationPolicy.NormalizeEase(selected));
            }
        }
    }

    [Fact]
    public void Phase2_In_keep_differs_from_phase1_InOut_remap()
    {
        TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase("InCubic", entrance: true)
            .Should().Be(TesseraFlyoutAnimationPolicy.EaseInCubic);
        TesseraFlyoutAnimationPolicy.ResolvePhase1MotionEase("InCubic", entrance: true)
            .Should().Be(TesseraFlyoutAnimationPolicy.EaseInOutCubic);
        TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase("OutQuart", entrance: true)
            .Should().Be(TesseraFlyoutAnimationPolicy.ResolvePhase1MotionEase("OutQuart", entrance: true));
    }

    [Fact]
    public void Phase2_OutQuart_show_spends_time_in_mid_band_while_hide_lingers()
    {
        const int steps = 20;
        var showEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
            TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
        var hideEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
            TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false);

        showEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseInOutQuart);
        hideEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseOutQuart);

        var showEarly = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
            entrance: true, 5, steps, showEase);
        var outQuartEarly = TesseraFlyoutAnimationPolicy.InterpolateStepped(
            0, 1, 5, steps, TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
        showEarly.Should().BeLessThan(0.5, "Out* forward is a start-slam; show remaps to InOut");
        outQuartEarly.Should().BeGreaterThan(0.5);

        var midBand = 0;
        for (var s = 0; s <= steps; s++)
        {
            var p = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
                entrance: true, s, steps, showEase);
            if (p >= TesseraFlyoutAnimationPolicy.Phase2ShowMidBandMin
                && p <= TesseraFlyoutAnimationPolicy.Phase2ShowMidBandMax)
                midBand++;
        }

        (midBand / (double)(steps + 1)).Should().BeGreaterThanOrEqualTo(
            TesseraFlyoutAnimationPolicy.Phase2ShowMidBandMinDurationFraction);

        var hideEarly = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
            entrance: false, 5, steps, hideEase);
        hideEarly.Should().BeGreaterThan(0.9);
    }

    [Fact]
    public void Show_phase2_overlaps_phase1_until_mid_band_and_skips_pause()
    {
        TesseraFlyoutAnimationPolicy.ShowMustSkipFancyPauseBeforePhase2.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.ShowPhase2MustOverlapUntilMidBand.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.ResolveInterPhaseWaitMs(entrance: true).Should().Be(0);
        TesseraFlyoutAnimationPolicy.ResolveInterPhaseWaitMs(entrance: false)
            .Should().Be(TesseraFlyoutAnimationPolicy.FancyPauseMs);

        const int steps = 25;
        var overlap = TesseraFlyoutAnimationPolicy.ResolvePhase2ShowOverlapMs(
            TesseraFlyoutAnimationPolicy.EaseOutQuint, steps);
        var lead = TesseraFlyoutAnimationPolicy.ResolveShowPhase2LeadDelayMs(
            TesseraFlyoutAnimationPolicy.EaseOutQuint, steps);
        var phaseMs = TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(steps);
        var showEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
            TesseraFlyoutAnimationPolicy.EaseOutQuint, entrance: true);
        var overlapSteps = overlap / TesseraFlyoutAnimationPolicy.StepPresentationIntervalMs;
        var p = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
            entrance: true, overlapSteps, steps, showEase);

        overlap.Should().BeGreaterThan(100);
        lead.Should().Be(phaseMs - overlap);
        p.Should().BeGreaterThanOrEqualTo(TesseraFlyoutAnimationPolicy.Phase2ShowMidBandMin);
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
        TesseraFlyoutAnimationPolicy.StackedMediaMustSkipPhase1OnShow.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.StackedShowMustFinishVolumePhase1BeforeMediaPhase2
            .Should().BeTrue();
        TesseraFlyoutAnimationPolicy.StackedHideMustFinishPhase2BeforeAnySlotPhase1
            .Should().BeTrue();
        TesseraFlyoutAnimationPolicy.ShouldRunPhase1(
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
        TesseraFlyoutAnimationPolicy.ShouldRunPhase1(
                ani: 2, "Fluent", showMediaStrip: true, stackedRole: null, entrance: true)
            .Should().BeTrue();
        TesseraFlyoutAnimationPolicy.ShouldRunPhase1(
                ani: 0,
                "Fluent",
                showMediaStrip: true,
                stackedRole: TesseraStackedPanelRole.Media,
                entrance: true)
            .Should().BeTrue();
        TesseraFlyoutAnimationPolicy.ShouldHoldStackedMediaHiddenThroughShowPhase1(
                ani: 2, "Fluent", showMediaStrip: true, TesseraStackedPanelRole.Media)
            .Should().BeTrue();
        TesseraFlyoutAnimationPolicy.ShouldHoldStackedMediaHiddenThroughShowPhase1(
                ani: 2, "Fluent", showMediaStrip: true, TesseraStackedPanelRole.Volume)
            .Should().BeFalse();
        TesseraFlyoutAnimationPolicy.PreparePhase2ShowMustUnhideStackedMedia.Should().BeTrue();
        TesseraFlyoutHwndRegionSpec.ShouldZeroWindowOpacityForCollapsedRegion(
                stackedMedia: true, hideChrome: true, showMotionActive: true)
            .Should().BeFalse();
    }

    [Fact]
    public void Phase2_OutQuart_hide_lingers_on_the_reverse_clock()
    {
        const int steps = 20;
        var showEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
            TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
        var hideEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
            TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false);
        showEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseInOutQuart);
        hideEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseOutQuart);
        TesseraFlyoutAnimationPolicy.Phase2MustSyncRevealRegionBeforeTweenBothWays.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.Phase2HideMustStartFromRestReveal.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.Phase2HideMustNotSnapMissingHostsToRest.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.Phase2ShowMustNotSnapMissingHostsToRest.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.Phase2MustPumpEachAniStepOnDispatcher.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.PreparePhase2ShowMustYieldForRender.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.ShouldSnapMissingPhase2HostsToRest(entrance: true).Should().BeFalse();
        TesseraFlyoutAnimationPolicy.ShouldSnapMissingPhase2HostsToRest(entrance: false).Should().BeFalse();

        var distinctHideTicks = 0;
        var previous = double.NaN;
        for (var s = 0; s <= steps; s++)
        {
            var hide = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
                entrance: false, s, steps, hideEase);
            if (double.IsNaN(previous) || Math.Abs(hide - previous) > 0.01)
                distinctHideTicks++;
            previous = hide;
        }

        var hideEarly = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(
            entrance: false, 5, steps, hideEase);
        hideEarly.Should().BeGreaterThan(0.9);
        distinctHideTicks.Should().BeGreaterThan(10, "skipping to collapsed is not a wipe");
    }

    [Fact]
    public void Stacked_phase2_still_runs_on_volume_for_gnome_fill()
    {
        TesseraFlyoutAnimationPolicy.StackedPhase2MustNotBeMediaSlotOnly.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.ShouldRunPhase2Reveal(
                2, "Gnome", showMediaStrip: true, TesseraStackedPanelRole.Volume)
            .Should().BeTrue();
        TesseraFlyoutAnimationPolicy.ShouldRunPhase2Reveal(
                2, "Gnome", showMediaStrip: true, TesseraStackedPanelRole.Media)
            .Should().BeTrue();
    }
}
