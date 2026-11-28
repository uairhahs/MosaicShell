using FluentAssertions;
using MosaicShell.Core.HubShell;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The tray's quick panel opens by the pointer that clicked the icon, kept inside the work area: above a bottom
    /// taskbar, below a top one, beside a side one. Physical pixels.
    /// </summary>
    public sealed class TrayPanelPlacementTests
    {
        private static readonly PixelBox Work = new(0, 0, 2560, 1392); // a 1440 screen with a 48 px taskbar at the bottom
        private const int W = 300;
        private const int H = 260;

        [Fact]
        public void Over_a_bottom_taskbar_it_sits_just_above_it_centred_on_the_pointer()
        {
            (int x, int y) = TrayPanelPlacement.TopLeft(1800, 1420, Work, W, H);

            _ = x.Should().Be(1800 - (W / 2));
            _ = y.Should().Be(1392 - H - TrayPanelPlacement.MarginPx);
        }

        [Fact]
        public void Near_the_screen_corner_it_stays_inside_the_work_area()
        {
            (int x, int y) = TrayPanelPlacement.TopLeft(2550, 1420, Work, W, H);

            _ = x.Should().Be(2560 - W - TrayPanelPlacement.MarginPx);
            _ = y.Should().Be(1392 - H - TrayPanelPlacement.MarginPx);
        }

        [Fact]
        public void Under_a_top_taskbar_it_sits_just_below_it()
        {
            PixelBox work = new(0, 48, 2560, 1392);

            (_, int y) = TrayPanelPlacement.TopLeft(1800, 20, work, W, H);

            _ = y.Should().Be(48 + TrayPanelPlacement.MarginPx);
        }

        [Fact]
        public void Beside_a_right_taskbar_it_sits_to_its_left()
        {
            PixelBox work = new(0, 0, 2500, 1440);

            (int x, int y) = TrayPanelPlacement.TopLeft(2530, 1300, work, W, H);

            _ = x.Should().Be(2500 - W - TrayPanelPlacement.MarginPx);
            _ = y.Should().Be(1300 - H - TrayPanelPlacement.MarginPx, "above the pointer, as over any taskbar");
        }

        [Fact]
        public void Without_a_pointer_position_it_takes_the_work_areas_bottom_right()
        {
            (int x, int y) = TrayPanelPlacement.TopLeft(null, null, Work, W, H);

            _ = x.Should().Be(2560 - W - TrayPanelPlacement.MarginPx);
            _ = y.Should().Be(1392 - H - TrayPanelPlacement.MarginPx);
        }
    }
}
