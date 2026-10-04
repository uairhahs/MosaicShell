using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// F01: after a track skip the title went blank because the marquee measured the viewport while
    /// it still held the previous title. SMTC clears the title between tracks, so the viewport had
    /// shrunk to about the width of a space; a 300 dip title then scrolled 296 dip, almost entirely
    /// out of view. The distance must depend only on the new text width and the width the layout
    /// grants, never on the previous content.
    /// </summary>
    public class TesseraMarqueeSpecTests
    {
        [Fact]
        public void Distance_after_a_skip_through_an_empty_title_uses_the_granted_width()
        {
            // Previous content was a single space (about 4 dip wide). That value is not an input.
            double available = TesseraMarqueeSpec.AvailableWidth(maxWidth: 220, grantedWidth: 220);

            _ = TesseraMarqueeSpec.ScrollDistance(textWidth: 300, availableWidth: available).Should().Be(80);
        }

        [Fact]
        public void A_title_that_fits_does_not_scroll()
        {
            double available = TesseraMarqueeSpec.AvailableWidth(maxWidth: 220, grantedWidth: 220);

            _ = TesseraMarqueeSpec.ScrollDistance(textWidth: 150, availableWidth: available).Should().Be(0);
        }

        [Fact]
        public void A_narrower_layout_than_the_cap_wins()
        {
            // CoreUI's media column grants about 150 dip although the caller caps at 220.
            _ = TesseraMarqueeSpec.AvailableWidth(maxWidth: 220, grantedWidth: 150).Should().Be(150);
        }

        [Theory]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NaN)]
        [InlineData(0)]
        [InlineData(-5)]
        public void An_unknown_or_unbounded_grant_falls_back_to_the_cap(double granted)
        {
            _ = TesseraMarqueeSpec.AvailableWidth(maxWidth: 220, grantedWidth: granted).Should().Be(220);
        }

        [Fact]
        public void A_width_change_after_start_re_derives_the_distance()
        {
            _ = TesseraMarqueeSpec.NeedsRederive(previousAvailable: 220, currentAvailable: 150).Should().BeTrue();
            _ = TesseraMarqueeSpec.NeedsRederive(previousAvailable: 220, currentAvailable: 220.2).Should().BeFalse();
            _ = TesseraMarqueeSpec.NeedsRederive(previousAvailable: double.NaN, currentAvailable: 220).Should().BeTrue();
        }

        // Timing values below are what the Host computed before the spec existed, for the same
        // inputs; moving the math to Core must not change speed or dwell.

        [Fact]
        public void Relaxed_window_timing_is_unchanged()
        {
            TesseraMarqueeSpec.Timing t = TesseraMarqueeSpec.ResolveTiming(autoDismissMs: 0, fontSize: 15, distance: 80);

            _ = t.StartDwellTicks.Should().Be(40);
            _ = t.EndDwellTicks.Should().Be(27);
            _ = t.PixelsPerTick.Should().BeApproximately(0.99, 1e-9);
        }

        [Fact]
        public void Shortest_window_timing_is_unchanged()
        {
            TesseraMarqueeSpec.Timing t = TesseraMarqueeSpec.ResolveTiming(autoDismissMs: 500, fontSize: 15, distance: 80);

            _ = t.StartDwellTicks.Should().Be(10);
            _ = t.EndDwellTicks.Should().Be(8);
            _ = t.PixelsPerTick.Should().BeApproximately(3.465, 1e-9);
        }

        [Fact]
        public void A_long_overflow_speeds_up_to_the_length_driven_cap()
        {
            TesseraMarqueeSpec.Timing t = TesseraMarqueeSpec.ResolveTiming(autoDismissMs: 0, fontSize: 15, distance: 1000);

            // 1000 dip in 6 s would need 166.7 dip/s; the cap is 20 cps x 8.25 dip = 165 dip/s.
            _ = t.PixelsPerTick.Should().BeApproximately(4.95, 1e-9);
        }

        [Fact]
        public void A_moderate_overflow_speeds_up_just_enough_to_fit_the_budget()
        {
            TesseraMarqueeSpec.Timing t = TesseraMarqueeSpec.ResolveTiming(autoDismissMs: 0, fontSize: 15, distance: 600);

            // 600 dip at 33 dip/s takes 18 s, over the 6 s budget, so 100 dip/s (under the cap).
            _ = t.PixelsPerTick.Should().BeApproximately(3.0, 1e-9);
        }

        [Fact]
        public void Host_marquee_reads_the_spec_and_never_the_previous_content_width()
        {
            string file = SourceTree.EnumerateSources("MosaicShell.Host")
                .Single(static f => Path.GetFileName(f) == "TesseraMarqueeText.cs");
            string[] code = [.. File.ReadAllLines(file).Where(static l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal))];

            _ = code.Should().NotContain(static l => l.Contains("Bounds.Width", StringComparison.Ordinal),
                "the viewport's arranged width follows the previous title (F01)");
            _ = code.Should().NotContain(static l => l.Contains("LayoutUpdated", StringComparison.Ordinal),
                "LayoutUpdated restarts fed back into layout and looped");
            _ = code.Should().NotContain(static l => l.Contains(" const ", StringComparison.Ordinal),
                "speed, dwell and tick literals live in TesseraMarqueeSpec");
            _ = code.Should().Contain(static l => l.Contains("TesseraMarqueeSpec.ScrollDistance", StringComparison.Ordinal));
        }
    }
}
