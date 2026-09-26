using FluentAssertions;
using MosaicShell.Core.HostPlatform;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The Host re-queues a fraction apply while DesktopScaling disagrees with the resolved screen's
    /// Scaling. On a window straddling mixed-DPI monitors that disagreement can be permanent, and the
    /// uncapped re-queue spun the dispatcher forever (and filled host-size.log). The retry must end.
    /// </summary>
    public class HostWindowFractionPolicyTests
    {
        [Fact]
        public void Waits_for_scale_settle_only_on_first_pass()
        {
            _ = HostWindowFractionPolicy.ShouldWaitForScaleSettle(scalingSettled: false, pass: 0, settleRetries: 0).Should().BeTrue();
            _ = HostWindowFractionPolicy.ShouldWaitForScaleSettle(scalingSettled: false, pass: 1, settleRetries: 0).Should().BeFalse();
            _ = HostWindowFractionPolicy.ShouldWaitForScaleSettle(scalingSettled: true, pass: 0, settleRetries: 0).Should().BeFalse();
        }

        [Fact]
        public void Never_settling_scale_stops_retrying_after_the_cap()
        {
            int retries = 0;
            while (HostWindowFractionPolicy.ShouldWaitForScaleSettle(scalingSettled: false, pass: 0, retries))
            {
                retries++;
                _ = retries.Should().BeLessThanOrEqualTo(HostWindowFractionPolicy.MaxScaleSettleRetries);
            }

            _ = retries.Should().Be(HostWindowFractionPolicy.MaxScaleSettleRetries);
            _ = HostWindowFractionPolicy.MaxScaleSettleRetries.Should().BeInRange(1, 10);
        }
    }
}
