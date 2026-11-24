using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutOutsideClickPolicyTests
    {
        [Fact]
        public void The_watcher_arms_as_the_flyout_presents()
        {
            // It armed 1.5 s after the present, so the first click outside a fresh flyout was ignored (Windows 11,
            // 2026-11-24). The X11 layer takes clicks from the moment it shows; Windows now matches it.
            _ = TesseraFlyoutOutsideClickPolicy.ArmDelayMs.Should().Be(0);
        }

        [Theory]
        [InlineData(50, 50, false)]
        [InlineData(10, 10, false)]
        [InlineData(99, 99, false)]
        [InlineData(100, 50, true)]
        [InlineData(9, 50, true)]
        [InlineData(250, 250, false)]
        [InlineData(500, 500, true)]
        public void A_press_is_outside_when_no_flyout_rect_holds_it(int x, int y, bool outside)
        {
            (int, int, int, int)[] rects = [(10, 10, 100, 100), (200, 200, 300, 300)];

            _ = TesseraFlyoutOutsideClickPolicy.IsOutside(x, y, rects).Should().Be(outside);
        }

        [Fact]
        public void With_no_flyout_rect_read_nothing_counts_as_outside()
        {
            // Every window gone or unreadable: never dismiss on a guess.
            _ = TesseraFlyoutOutsideClickPolicy.IsOutside(5, 5, []).Should().BeFalse();
        }
    }
}
