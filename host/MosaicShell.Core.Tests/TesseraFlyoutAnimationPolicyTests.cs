using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutAnimationPolicyTests
    {
        /// <summary>ActionTimer Repeat delay in Ani1/Ani2.inc (below a 60 Hz frame).</summary>
        private const int EncodedActionTimerIntervalMs = 2;

        /// <summary>Below this, In vs Out and Bounce vs Linear are not readable on a 60 Hz display.</summary>
        private const int MinPerceptiblePhaseDurationMs = 200;

        private const double Phase2ShowMidBandMax = 0.85;

        /// <summary>Minimum share of 0..AniSteps samples that must sit in the mid band.</summary>
        private const double Phase2ShowMidBandMinDurationFraction = 0.18;

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
        }

        [Fact]
        public void An_os_acrylic_window_slides_as_a_whole_so_its_blur_moves_with_the_card()
        {
            // Modern Flyouts, 2026-10-04 (motion frame trace, run ff725208): the content slid 30 dip
            // by RenderTransform inside an acrylic pane that stayed put, so the card's border travelled
            // across a still blurred pane (the "shake while sliding"). YourFlyouts moves the skin window.
            _ = TesseraFlyoutAnimationPolicy.ResolvePhase1SlideTarget(osAcrylicBackdrop: true)
                .Should().Be(TesseraSlideTarget.WindowPosition);
        }

        [Fact]
        public void A_sliding_acrylic_window_starts_its_entrance_at_the_slide_offset()
        {
            // Modern Flyouts, 2026-10-04 (run c585c57c): the volume pane was shown at rest, then the
            // slide began 30 px to the left and travelled back, so the visible pane jumped left and
            // returned (the jiggle). The start pose has to be in place before the pane is shown.
            (int dx, int dy) = TesseraFlyoutAnimationPolicy.ResolveSlideOffsetPx("left", 30);

            _ = TesseraFlyoutAnimationPolicy.ResolveWindowEntranceStartOffsetPx(ani: 2, "left", 30, osAcrylicBackdrop: true)
                .Should().Be((dx, dy));
            _ = (dx, dy).Should().NotBe((0, 0), "premise: the left slide has an offset");
        }

        [Theory]
        [InlineData(2, false)]
        [InlineData(0, true)]
        public void No_window_start_offset_when_the_window_does_not_slide(int ani, bool osAcrylic)
        {
            _ = TesseraFlyoutAnimationPolicy.ResolveWindowEntranceStartOffsetPx(ani, "left", 30, osAcrylic)
                .Should().Be((0, 0));
        }

        [Fact]
        public void A_window_that_paints_its_own_background_slides_its_content()
        {
            _ = TesseraFlyoutAnimationPolicy.ResolvePhase1SlideTarget(osAcrylicBackdrop: false)
                .Should().Be(TesseraSlideTarget.RenderTransform);
        }

        [Fact]
        public void Stepped_sample_matches_the_keyframes_and_interpolates_between_them()
        {
            const int steps = 4;
            string ease = TesseraFlyoutAnimationPolicy.EaseOutQuart;
            for (int k = 0; k <= steps; k++)
            {
                double key = TesseraFlyoutAnimationPolicy.InterpolateStepped(30, 0, k, steps, ease, entrance: true);
                _ = TesseraFlyoutAnimationPolicy.SampleSteppedAt(30, 0, k / (double)steps, steps, ease, entrance: true)
                    .Should().BeApproximately(key, 1e-9);
            }

            double a = TesseraFlyoutAnimationPolicy.InterpolateStepped(30, 0, 1, steps, ease, entrance: true);
            double b = TesseraFlyoutAnimationPolicy.InterpolateStepped(30, 0, 2, steps, ease, entrance: true);
            _ = TesseraFlyoutAnimationPolicy.SampleSteppedAt(30, 0, 1.5 / steps, steps, ease, entrance: true)
                .Should().BeApproximately((a + b) / 2, 1e-9);
        }

        [Theory]
        [InlineData(-0.5, 30)]
        [InlineData(1.5, 0)]
        public void Stepped_sample_clamps_outside_the_run(double linearT, double expected)
        {
            _ = TesseraFlyoutAnimationPolicy.SampleSteppedAt(30, 0, linearT, 4, TesseraFlyoutAnimationPolicy.EaseOutQuart, entrance: true)
                .Should().BeApproximately(expected, 1e-9);
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
        public void Default_phase_is_long_enough_to_tell_easing_families_apart()
        {
            int encoded = TesseraFlyoutAnimationPolicy.DefaultAniSteps
                          * EncodedActionTimerIntervalMs;
            _ = encoded.Should().Be(40);
            _ = encoded.Should().BeLessThan(MinPerceptiblePhaseDurationMs);
            _ = TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(TesseraFlyoutAnimationPolicy.DefaultAniSteps)
                .Should().BeGreaterThanOrEqualTo(MinPerceptiblePhaseDurationMs);
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

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(true, true, true)]
        [InlineData(false, true, false)]
        public void Relayout_deferred_for_entire_motion_including_phase2(bool motion, bool phase2, bool defer)
        {
            _ = TesseraFlyoutAnimationPolicy.ShouldDeferRelayoutDuringMotion(motion, phase2)
                .Should().Be(defer);
        }

        [Fact]
        public void Cancelled_entrance_must_snap_to_rest_and_clear_motion_on_supersede()
        {
            _ = TesseraFlyoutAnimationPolicy.CancelledEntranceMustSnapToRest.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.MotionAnimatingMustClearOnSupersede.Should().BeTrue();
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
                TesseraFlyoutAnimationPolicy.EaseInOutQuint, steps);
            int lead = TesseraFlyoutAnimationPolicy.ResolveShowPhase2LeadDelayMs(
                TesseraFlyoutAnimationPolicy.EaseInOutQuint, steps);
            int phaseMs = TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(steps);
            string showEase = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
                TesseraFlyoutAnimationPolicy.EaseInOutQuint, entrance: true);
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
        }

        [Fact]
        public void Stacked_phase2_still_runs_on_volume_for_gnome_fill()
        {
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

        // ---- Phase 2 is driven by rendered frames, so it needs a value at any time, not only at whole steps ----

        [Theory]
        [InlineData(true, 10, "OutQuart")]
        [InlineData(false, 10, "OutQuart")]
        [InlineData(true, 20, "InOutBack")]
        [InlineData(false, 20, "InOutBack")]
        [InlineData(true, 40, "Linear")]
        [InlineData(false, 40, "OutBounce")]
        public void Reveal_progress_at_a_step_boundary_equals_the_stepped_value(bool entrance, int steps, string ease)
        {
            for (int step = 0; step <= steps; step++)
            {
                _ = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgressAt(entrance, step / (double)steps, steps, ease)
                    .Should().BeApproximately(TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(entrance, step, steps, ease), 1e-9);
            }
        }

        [Theory]
        [InlineData(true, "OutQuart")]
        [InlineData(false, "InOutQuart")]
        [InlineData(true, "InOutBack")]
        public void Reveal_progress_between_steps_is_a_straight_line_between_the_two_neighbours(bool entrance, string ease)
        {
            const int steps = 20;
            for (int step = 0; step < steps; step++)
            {
                double a = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(entrance, step, steps, ease);
                double b = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgress(entrance, step + 1, steps, ease);
                double mid = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgressAt(entrance, (step + 0.5) / steps, steps, ease);

                _ = mid.Should().BeApproximately((a + b) / 2, 1e-9);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Reveal_progress_clamps_time_outside_the_run(bool entrance)
        {
            double start = entrance ? 0 : 1;
            double end = entrance ? 1 : 0;

            _ = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgressAt(entrance, -0.5, 20, "OutQuart").Should().BeApproximately(start, 1e-9);
            _ = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgressAt(entrance, 0, 20, "OutQuart").Should().BeApproximately(start, 1e-9);
            _ = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgressAt(entrance, 1, 20, "OutQuart").Should().BeApproximately(end, 1e-9);
            _ = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgressAt(entrance, 3, 20, "OutQuart").Should().BeApproximately(end, 1e-9);
        }

        [Fact]
        public void Reveal_progress_moves_in_small_equal_amounts_at_display_rate()
        {
            // At 144 Hz a 20-step run (320 ms) is sampled about 46 times; the stepped value moves in 20 lumps.
            const int steps = 20;
            double previous = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgressAt(true, 0, steps, "Linear");
            double largest = 0;
            for (int frame = 1; frame <= 46; frame++)
            {
                double now = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgressAt(true, frame / 46.0, steps, "Linear");
                largest = Math.Max(largest, Math.Abs(now - previous));
                previous = now;
            }

            _ = largest.Should().BeLessThan(0.03, "46 frames across a straight 0..1 run move about 0.022 each");
        }

        // ---- What you pick is what plays: no remapping of the chosen curve (the Hub preview shows the same curve) ----

        [Fact]
        public void Every_curve_plays_exactly_as_chosen_in_both_phases_and_both_directions()
        {
            foreach (string id in TesseraFlyoutAnimationPolicy.AllEaseTypes)
            {
                foreach (bool entrance in new[] { true, false })
                {
                    _ = TesseraFlyoutAnimationPolicy.ResolvePhase1MotionEase(id, entrance).Should().Be(id);
                    _ = TesseraFlyoutAnimationPolicy.ResolvePhase1OpacityEase(id, entrance).Should().Be(id);
                    _ = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(id, entrance).Should().Be(id);
                }
            }
        }

        [Theory]
        [InlineData("OutQuart", 0.60, 1.00)]
        [InlineData("OutCubic", 0.50, 0.95)]
        [InlineData("InQuart", 0.00, 0.10)]
        [InlineData("InOutSine", 0.10, 0.40)]
        [InlineData("Linear", 0.24, 0.26)]
        public void An_entrance_covers_the_distance_early_or_late_as_the_curve_says(string ease, double min, double max)
        {
            // A quarter of the way through the run, an Out curve is well on its way and an In curve has barely left.
            double quarter = TesseraFlyoutAnimationPolicy.SampleSteppedAt(0, 1, 0.25, 20, ease, entrance: true);

            _ = quarter.Should().BeInRange(min, max);
        }

        [Theory]
        [InlineData("OutBack")]
        [InlineData("OutElastic")]
        public void An_overshooting_curve_goes_past_rest_before_settling(string ease)
        {
            double peak = 0;
            for (int i = 0; i <= 200; i++)
            {
                peak = Math.Max(peak, TesseraFlyoutAnimationPolicy.SampleSteppedAt(0, 1, i / 200.0, 30, ease, entrance: true));
            }

            _ = peak.Should().BeGreaterThan(1.03, "the overshoot is what makes this curve look different");
            _ = TesseraFlyoutAnimationPolicy.SampleSteppedAt(0, 1, 1, 30, ease, entrance: true).Should().BeApproximately(1, 1e-9);
        }
    }
}
