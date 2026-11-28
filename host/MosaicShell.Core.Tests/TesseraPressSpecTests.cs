using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraPressSpecTests
    {
        [Theory]
        [InlineData(22, 22)]
        [InlineData(28, 28)]
        [InlineData(36, 36)]
        [InlineData(48, 40)]
        [InlineData(400, 120)]
        public void A_press_moves_each_edge_in_between_1_and_1_5_dip_or_stops_at_the_clamp(double w, double h)
        {
            double s = TesseraPressSpec.SinkScale(w, h);
            double inset = (1 - s) * Math.Min(w, h) / 2;
            bool clamped = s is TesseraPressSpec.MinScale or TesseraPressSpec.MaxScale;
            if (!clamped)
            {
                _ = inset.Should().BeInRange(1.0, 1.5);
            }

            _ = s.Should().BeInRange(TesseraPressSpec.MinScale, TesseraPressSpec.MaxScale);
        }

        [Fact]
        public void A_tiny_or_empty_control_never_sinks_past_the_clamp()
        {
            _ = TesseraPressSpec.SinkScale(10, 10).Should().Be(TesseraPressSpec.MinScale);
            _ = TesseraPressSpec.SinkScale(0, 0).Should().Be(TesseraPressSpec.MaxScale);
        }

        [Fact]
        public void The_sink_leans_toward_the_press_but_stays_near_the_centre()
        {
            _ = TesseraPressSpec.Anchor(14, 14, 28, 28).Should().Be((0.5, 0.5));
            (double x, double y) = TesseraPressSpec.Anchor(0, 28, 28, 28);
            _ = x.Should().BeApproximately(0.35, 1e-9);
            _ = y.Should().BeApproximately(0.65, 1e-9);
            _ = TesseraPressSpec.Anchor(5, 5, 0, 0).Should().Be((0.5, 0.5));
        }

        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(28, 28, true)]
        [InlineData(14, 14, true)]
        [InlineData(-1, 14, false)]
        [InlineData(14, 29, false)]
        public void Only_a_release_inside_the_button_acts(double x, double y, bool acts)
        {
            _ = TesseraPressSpec.ReleasedInside(x, y, 28, 28).Should().Be(acts);
        }

        [Fact]
        public void A_near_invisible_hover_on_an_opaque_fill_is_raised()
        {
            // Core UI's play button: white to #F5F5FA did not read.
            _ = TesseraPressSpec.VisibleHover(0xFFFFFFFF, 0xFFF5F5FA).Should().Be(0xFFF1F1F1);
            // A dark fill lightens instead.
            _ = TesseraPressSpec.VisibleHover(0xFF202020, 0xFF242424).Should().Be(0xFF2E2E2E);
        }

        [Fact]
        public void A_hover_that_already_reads_or_is_translucent_is_kept()
        {
            _ = TesseraPressSpec.VisibleHover(0xFFFFFFFF, 0xFFE0E0E0).Should().Be(0xFFE0E0E0);
            _ = TesseraPressSpec.VisibleHover(0x00FFFFFF, 0x1AFFFFFF).Should().Be(0x1AFFFFFF);
        }

        [Fact]
        public void Pressed_goes_past_hover_away_from_normal()
        {
            _ = TesseraPressSpec.PressedFill(0xFFFFFFFF, 0xFFF1F1F1).Should().Be(0xFFE6E6E6);
        }

        [Fact]
        public void Pressed_on_a_transparent_button_only_strengthens_the_hover_alpha()
        {
            // Transparent is #00FFFFFF; its white must not pull a dark hover toward black.
            _ = TesseraPressSpec.PressedFill(0x00FFFFFF, 0x28303030).Should().Be(0x48303030);
        }
    }

    public class OptimisticToggleTests
    {
        [Fact]
        public void A_press_shows_the_new_state_at_once()
        {
            OptimisticToggle toggle = new();
            _ = toggle.Press(shownOn: false, nowMs: 0).Should().BeTrue();
        }

        [Fact]
        public void A_stale_report_inside_the_hold_does_not_flip_the_icon_back()
        {
            OptimisticToggle toggle = new();
            _ = toggle.Press(shownOn: false, nowMs: 0);
            _ = toggle.Resolve(reportedOn: false, nowMs: 300).Should().BeTrue();
        }

        [Fact]
        public void The_expected_report_or_the_end_of_the_hold_hands_the_icon_back_to_the_player()
        {
            OptimisticToggle toggle = new();
            _ = toggle.Press(shownOn: false, nowMs: 0);
            _ = toggle.Resolve(reportedOn: true, nowMs: 200).Should().BeTrue();
            _ = toggle.Resolve(reportedOn: false, nowMs: 400).Should().BeFalse("the player paused again on its own");

            OptimisticToggle ignored = new();
            _ = ignored.Press(shownOn: false, nowMs: 0);
            _ = ignored.Resolve(reportedOn: false, nowMs: OptimisticToggle.HoldMs).Should().BeFalse("the press did not take");
        }

        [Fact]
        public void Without_a_press_the_report_is_shown_as_is()
        {
            _ = new OptimisticToggle().Resolve(reportedOn: true, nowMs: 0).Should().BeTrue();
        }
    }
}
