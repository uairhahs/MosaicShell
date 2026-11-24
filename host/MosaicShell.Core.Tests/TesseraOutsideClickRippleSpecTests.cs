using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraOutsideClickRippleSpecTests
    {
        [Theory]
        [InlineData(FocusDimRoute.X11ArgbInputShape, true)]
        [InlineData(FocusDimRoute.Win32LayeredAlpha, true)]
        [InlineData(FocusDimRoute.None, false)]
        public void The_ripple_plays_wherever_a_translucent_layer_can_be_drawn(FocusDimRoute route, bool expected)
        {
            // On X11 the outside click is swallowed, so the ripple is what says "this click dismissed it". Windows
            // passes the click on and plays it too, so both platforms answer the same click the same way.
            _ = TesseraOutsideClickRippleSpec.ShouldPlay(route).Should().Be(expected);
        }

        [Theory]
        [InlineData(FocusDimRoute.Win32LayeredAlpha, true)]
        [InlineData(FocusDimRoute.X11ArgbInputShape, false)]
        [InlineData(FocusDimRoute.None, false)]
        public void On_windows_the_glint_window_is_warmed_while_the_click_watcher_is_armed(FocusDimRoute route, bool expected)
        {
            // A cold-shown Transparent HWND paints black until composition settles (HideUntilCompositionReady), and
            // the glint lives only DurationMs: shown at the click it was a late black box (kongo, 2026-11-24).
            _ = TesseraOutsideClickRippleSpec.WarmBeforeTheClick(route).Should().Be(expected);
        }

        [Theory]
        [InlineData(null, 0.0, 0.0)]
        [InlineData(null, 200.0, 0.0)]
        [InlineData(null, TesseraOutsideClickRippleSpec.FirstFrameWaitMs + TesseraOutsideClickRippleSpec.DurationMs, 1.0)]
        [InlineData(100.0, 100.0, 0.0)]
        [InlineData(100.0, 360.0, 0.5)]
        [InlineData(100.0, 2000.0, 1.0)]
        public void Progress_counts_from_the_first_frame_on_screen_not_from_show(
            double? firstFrameMs, double nowMs, double expected)
        {
            // A new composited window reaches the screen a frame or more after Show; a clock started at Show had
            // already run part of the glint by its first visible frame, so it looked late and cut short.
            _ = TesseraOutsideClickRippleSpec.ProgressSinceFirstFrame(firstFrameMs, nowMs)
                .Should().BeApproximately(expected, 1e-9);
        }

        [Fact]
        public void The_ripple_is_brief()
        {
            _ = TesseraOutsideClickRippleSpec.DurationMs.Should().BeInRange(250, 600);
        }

        [Theory]
        [InlineData(-10, 0)]
        [InlineData(0, 0)]
        [InlineData(TesseraOutsideClickRippleSpec.DurationMs / 2.0, 0.5)]
        [InlineData(TesseraOutsideClickRippleSpec.DurationMs, 1)]
        [InlineData(TesseraOutsideClickRippleSpec.DurationMs * 3.0, 1)]
        public void Progress_is_elapsed_time_over_the_duration_clamped(double elapsedMs, double expected)
        {
            _ = TesseraOutsideClickRippleSpec.ProgressAt(elapsedMs).Should().BeApproximately(expected, 1e-9);
        }

        [Fact]
        public void The_ring_contracts_from_the_start_to_the_end_diameter_and_never_grows()
        {
            // The ring closes onto the click point rather than spreading from it.
            _ = TesseraOutsideClickRippleSpec.StartDiameterDip.Should().BeGreaterThan(TesseraOutsideClickRippleSpec.EndDiameterDip);
            _ = TesseraOutsideClickRippleSpec.DiameterAt(0).Should().Be(TesseraOutsideClickRippleSpec.StartDiameterDip);
            _ = TesseraOutsideClickRippleSpec.DiameterAt(1).Should().BeApproximately(TesseraOutsideClickRippleSpec.EndDiameterDip, 1e-9);
            double previous = double.MaxValue;
            for (int i = 0; i <= 20; i++)
            {
                double diameter = TesseraOutsideClickRippleSpec.DiameterAt(i / 20.0);
                _ = diameter.Should().BeLessThanOrEqualTo(previous);
                previous = diameter;
            }
        }

        [Fact]
        public void The_ring_contracts_fast_then_settles()
        {
            // Ease out: more than half the contraction lands in the first third of the time.
            double start = TesseraOutsideClickRippleSpec.StartDiameterDip;
            double shrunk = start - TesseraOutsideClickRippleSpec.DiameterAt(1 / 3.0);
            _ = shrunk.Should().BeGreaterThan((start - TesseraOutsideClickRippleSpec.EndDiameterDip) / 2);
        }

        [Fact]
        public void The_ring_starts_at_peak_opacity_and_fades_to_nothing()
        {
            _ = TesseraOutsideClickRippleSpec.OpacityAt(0).Should().Be(TesseraOutsideClickRippleSpec.PeakOpacity);
            _ = TesseraOutsideClickRippleSpec.OpacityAt(1).Should().Be(0);
            double previous = double.MaxValue;
            for (int i = 0; i <= 20; i++)
            {
                double opacity = TesseraOutsideClickRippleSpec.OpacityAt(i / 20.0);
                _ = opacity.Should().BeLessThanOrEqualTo(previous);
                previous = opacity;
            }
        }

        [Fact]
        public void The_window_holds_the_widest_ring_its_stroke_and_its_blur()
        {
            double reach = (TesseraOutsideClickRippleSpec.StartDiameterDip / 2)
                + (TesseraOutsideClickRippleSpec.StrokeAt(0) / 2)
                + (2 * TesseraOutsideClickRippleSpec.BlurRadiusDip);
            _ = (TesseraOutsideClickRippleSpec.WindowSizeDip / 2).Should().BeGreaterThanOrEqualTo(reach);
        }

        [Fact]
        public void The_ring_is_soft_a_glimmer_not_a_hard_line()
        {
            _ = TesseraOutsideClickRippleSpec.BlurRadiusDip.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.3)]
        [InlineData(0.5)]
        [InlineData(0.8)]
        public void The_colour_changes_gradually_round_the_ring_with_no_segments(double progress)
        {
            // Separate tiles read as a harshly segmented ring; the colour must flow round it instead.
            const double step = 5;
            (byte r0, byte g0, byte b0) = TesseraOutsideClickRippleSpec.RingColorAt(0, progress);
            for (double angle = step; angle <= 360; angle += step)
            {
                (byte r, byte g, byte b) = TesseraOutsideClickRippleSpec.RingColorAt(angle, progress);
                _ = Math.Abs(r - r0).Should().BeLessThanOrEqualTo(24);
                _ = Math.Abs(g - g0).Should().BeLessThanOrEqualTo(24);
                _ = Math.Abs(b - b0).Should().BeLessThanOrEqualTo(24);
                (r0, g0, b0) = (r, g, b);
            }
        }

        [Fact]
        public void At_the_start_each_logo_colour_sits_where_its_tile_sits_in_the_logo()
        {
            for (int k = 0; k < TesseraOutsideClickRippleSpec.Palette.Count; k++)
            {
                _ = TesseraOutsideClickRippleSpec.BaseColorAt(TesseraOutsideClickRippleSpec.AnchorAngleDeg(k), 0)
                    .Should().Be(TesseraOutsideClickRippleSpec.Pastel(TesseraOutsideClickRippleSpec.Palette[k]));
            }
        }

        [Fact]
        public void The_colours_are_lightened_toward_pastel_for_a_holographic_look()
        {
            (byte r, byte g, byte b) violet = TesseraOutsideClickRippleSpec.Palette[0];
            (byte r, byte g, byte b) = TesseraOutsideClickRippleSpec.Pastel(violet);
            _ = r.Should().BeGreaterThan(violet.r);
            _ = g.Should().BeGreaterThan(violet.g);
            _ = b.Should().BeGreaterThanOrEqualTo(violet.b);
        }

        [Fact]
        public void The_colours_flow_round_the_ring_as_it_closes()
        {
            // Iridescence: the hue at a fixed point shifts as the light angle changes.
            double angle = TesseraOutsideClickRippleSpec.AnchorAngleDeg(0);
            _ = TesseraOutsideClickRippleSpec.BaseColorAt(angle, 1).Should().NotBe(TesseraOutsideClickRippleSpec.BaseColorAt(angle, 0));
            _ = TesseraOutsideClickRippleSpec.BaseColorAt(angle + TesseraOutsideClickRippleSpec.IridescenceDeg, 1)
                .Should().Be(TesseraOutsideClickRippleSpec.BaseColorAt(angle, 0));
        }

        [Fact]
        public void Every_click_shows_the_same_sheen()
        {
            _ = TesseraOutsideClickRippleSpec.RingColorAt(123, 0.4).Should().Be(TesseraOutsideClickRippleSpec.RingColorAt(123, 0.4));
        }

        [Fact]
        public void The_sheen_sweeps_one_way_round_the_ring()
        {
            double previous = double.MinValue;
            for (int i = 0; i <= 20; i++)
            {
                double angle = TesseraOutsideClickRippleSpec.GlintAngleDeg(i / 20.0);
                _ = angle.Should().BeGreaterThanOrEqualTo(previous);
                previous = angle;
            }

            _ = (TesseraOutsideClickRippleSpec.GlintAngleDeg(1) - TesseraOutsideClickRippleSpec.GlintAngleDeg(0))
                .Should().BeGreaterThan(180);
        }

        [Fact]
        public void The_ring_shines_brightest_under_the_sheen_and_stays_dull_opposite()
        {
            const double progress = 0.5;
            double glint = TesseraOutsideClickRippleSpec.GlintAngleDeg(progress);
            _ = TesseraOutsideClickRippleSpec.SheenAt(glint, progress).Should().BeGreaterThan(0.9);
            _ = TesseraOutsideClickRippleSpec.SheenAt(glint + 180, progress).Should().BeLessThan(0.3);
            for (int angle = 0; angle < 360; angle += 15)
            {
                _ = TesseraOutsideClickRippleSpec.SheenAt(angle, progress).Should().BeInRange(0, 1);
            }
        }

        [Fact]
        public void Under_full_sheen_the_ring_turns_the_highlight_white()
        {
            _ = TesseraOutsideClickRippleSpec.MixToHighlight(TesseraOutsideClickRippleSpec.BaseColorAt(40, 0), 1)
                .Should().Be(TesseraOutsideClickRippleSpec.Highlight);
            _ = TesseraOutsideClickRippleSpec.MixToHighlight(TesseraOutsideClickRippleSpec.BaseColorAt(40, 0), 0)
                .Should().Be(TesseraOutsideClickRippleSpec.BaseColorAt(40, 0));
        }

        [Fact]
        public void The_stroke_thins_a_little_as_the_ring_closes()
        {
            _ = TesseraOutsideClickRippleSpec.StrokeAt(1).Should().BeLessThan(TesseraOutsideClickRippleSpec.StrokeAt(0));
            _ = TesseraOutsideClickRippleSpec.StrokeAt(1).Should().BeGreaterThan(0);
        }

        [Fact]
        public void The_gradient_has_enough_stops_to_look_continuous()
        {
            _ = TesseraOutsideClickRippleSpec.GradientStops.Should().BeGreaterThanOrEqualTo(24);
        }

        [Fact]
        public void The_glow_fades_with_the_ring()
        {
            _ = TesseraOutsideClickRippleSpec.GlowOpacityAt(0).Should().BeGreaterThan(0);
            _ = TesseraOutsideClickRippleSpec.GlowOpacityAt(1).Should().Be(0);
            _ = TesseraOutsideClickRippleSpec.GlowOpacityAt(0).Should().BeLessThanOrEqualTo(TesseraOutsideClickRippleSpec.OpacityAt(0));
        }

        [Theory]
        [InlineData(1000, 500, 1.0)]
        [InlineData(1000, 500, 2.0)]
        [InlineData(3, 7, 1.25)]
        public void The_window_is_centered_on_the_click(int clickX, int clickY, double scaling)
        {
            int sizePx = TesseraOutsideClickRippleSpec.WindowSizePx(scaling);
            (int x, int y) = TesseraOutsideClickRippleSpec.WindowOrigin(clickX, clickY, scaling);

            _ = sizePx.Should().BeGreaterThanOrEqualTo((int)(TesseraOutsideClickRippleSpec.WindowSizeDip * scaling));
            _ = (x + (sizePx / 2)).Should().Be(clickX);
            _ = (y + (sizePx / 2)).Should().Be(clickY);
        }
    }
}
