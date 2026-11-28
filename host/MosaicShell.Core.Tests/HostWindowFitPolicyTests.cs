using FluentAssertions;
using MosaicShell.Core.HostPlatform;

namespace MosaicShell.Core.Tests
{
    public class HostWindowFitPolicyTests
    {
        [Fact]
        public void A_window_that_fits_keeps_its_size_on_a_smaller_monitor()
        {
            // The user's case: 1100x750 moved from a 2560x1370 work area to 1920x1130 fits, so nothing changes.
            _ = HostWindowFitPolicy.Fit(1100, 750, 1920, 1130, 780, 560).Should().Be((1100d, 750d));
            _ = HostWindowFitPolicy.NeedsFit(1100, 750, 1920, 1130, 780, 560).Should().BeFalse();
        }

        [Fact]
        public void A_window_is_never_grown_on_a_bigger_monitor()
        {
            _ = HostWindowFitPolicy.Fit(825, 619, 2560, 1370, 780, 560).Should().Be((825d, 619d));
        }

        [Fact]
        public void A_window_too_big_for_the_work_area_shrinks_to_it_side_by_side()
        {
            (double w, double h) = HostWindowFitPolicy.Fit(1800, 750, 1366, 728, 780, 560);
            _ = w.Should().Be(1366 - (2 * HostWindowFitPolicy.EdgeMarginDip));
            _ = h.Should().Be(728 - (2 * HostWindowFitPolicy.EdgeMarginDip));
            _ = HostWindowFitPolicy.NeedsFit(1800, 750, 1366, 728, 780, 560).Should().BeTrue();
        }

        [Fact]
        public void The_minimum_size_gives_way_only_to_a_work_area_smaller_than_it()
        {
            (double w, double h) = HostWindowFitPolicy.Fit(1100, 750, 640, 480, 780, 560);
            _ = w.Should().Be(640 - (2 * HostWindowFitPolicy.EdgeMarginDip));
            _ = h.Should().Be(480 - (2 * HostWindowFitPolicy.EdgeMarginDip));
        }

        [Fact]
        public void A_move_settles_before_the_fit_is_checked()
        {
            _ = HostWindowFitPolicy.SettleMs.Should().BeInRange(150, 1000, "long enough to be after a drag, short enough to feel immediate");
        }
    }
}
