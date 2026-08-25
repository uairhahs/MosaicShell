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
        TesseraFlyoutAnimationPolicy.ResolveFancyEntranceDurationMs(20).Should().Be(740);
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
    [InlineData("OutQuart")]
    [InlineData("InCubic")]
    [InlineData("Linear")]
    public void Phase2_keeps_selected_ease_both_ways(string selected)
    {
        TesseraFlyoutAnimationPolicy.Phase2MustUseSelectedEaseBothWays.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.Phase2ShowMustNotInvertToInEase.Should().BeTrue();
        var expected = TesseraFlyoutAnimationPolicy.NormalizeEase(selected);
        TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(selected, entrance: true)
            .Should().Be(expected);
        TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(selected, entrance: false)
            .Should().Be(expected);
    }

    [Theory]
    [InlineData("OutQuart")]
    [InlineData("InCubic")]
    public void Phase2_does_not_reuse_phase1_inout_remap(string selected)
    {
        TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(selected, entrance: true)
            .Should().NotBe(
                TesseraFlyoutAnimationPolicy.ResolvePhase1MotionEase(selected, entrance: true));
    }

    [Fact]
    public void Phase2_OutQuart_show_wipes_in_while_hide_still_lingers_open()
    {
        const int steps = 20;
        var showEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
            TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
        var hideEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
            TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false);

        var showEarly = TesseraFlyoutAnimationPolicy.InterpolateStepped(
            0, 1, 5, steps, showEase, entrance: true);
        var showLate = TesseraFlyoutAnimationPolicy.InterpolateStepped(
            0, 1, 15, steps, showEase, entrance: true);
        var hideEarly = TesseraFlyoutAnimationPolicy.InterpolateStepped(
            1, 0, 5, steps, hideEase, entrance: false);

        showEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseOutQuart);
        showEarly.Should().BeGreaterThan(0.5, "forward OutQuart must already be wiping, not InQuart-collapsed");
        showLate.Should().BeGreaterThan(0.9, "last quarter settles; InQuart would still be slamming");
        hideEarly.Should().BeGreaterThan(0.9);
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
    }

    [Fact]
    public void Phase2_OutQuart_hide_is_the_time_reverse_of_show()
    {
        const int steps = 20;
        var ease = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
            TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true);
        TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
                TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: false)
            .Should().Be(ease);
        TesseraFlyoutAnimationPolicy.Phase2MustSyncRevealRegionBeforeTweenBothWays.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.Phase2HideMustStartFromRestReveal.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.Phase2HideMustNotSnapMissingHostsToRest.Should().BeTrue();

        for (var s = 0; s <= steps; s++)
        {
            var show = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                0, 1, s, steps, ease, entrance: true);
            var hide = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                1, 0, steps - s, steps, ease, entrance: false);
            hide.Should().BeApproximately(show, 0.02, "hide at {0} must match show at {1}", steps - s, s);
        }
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
