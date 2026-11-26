using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutDismissSpecTests
    {
        [Fact]
        public void It_reads_Dismiss()
        {
            // The user's call (2026-11-26), as Fluent's toasts word it; a bare cross read as "close the app".
            _ = TesseraFlyoutDismissSpec.Label.Should().Be("Dismiss");
        }

        [Theory]
        [InlineData(FlyoutKinds.Volume, true)]
        [InlineData(FlyoutKinds.Brightness, true)]
        [InlineData(FlyoutKinds.Media, true)]
        [InlineData(FlyoutKinds.Locks, false)]
        [InlineData(FlyoutKinds.Flight, false)]
        public void Status_chips_get_none(string kind, bool expected)
        {
            // A lock-key or airplane chip is a one-line notice that leaves on its own.
            _ = TesseraFlyoutDismissSpec.ShowsOn(kind).Should().Be(expected);
        }

        [Theory]
        [InlineData(true, TesseraFlyoutPhase.Shown, true)]
        [InlineData(true, TesseraFlyoutPhase.Entering, true)]
        [InlineData(true, TesseraFlyoutPhase.Exiting, false)]
        [InlineData(true, TesseraFlyoutPhase.Hidden, false)]
        [InlineData(false, TesseraFlyoutPhase.Shown, false)]
        public void It_shows_only_while_the_pointer_is_over_a_flyout_that_is_up(bool hovered, TesseraFlyoutPhase phase, bool expected)
        {
            // The styles copy OS pop-ups that have nothing like it, so at rest the look is untouched.
            _ = TesseraFlyoutDismissSpec.IsVisible(hovered, phase).Should().Be(expected);
        }

        [Fact]
        public void A_brief_loss_of_hover_does_not_hide_it()
        {
            // Windows, stacked Modern Flyouts, pointer held still: the flyout reported the pointer leaving and coming
            // back 220 to 310 ms later (2026-11-26). The grace also carries the pointer across the gap to the link.
            _ = TesseraFlyoutDismissSpec.HideGraceMs.Should().BeGreaterThan(310);
            _ = TesseraFlyoutDismissSpec.HideGraceMs.Should().BeLessThan(TesseraFlyoutDismissPolicy.PointerLeaveDismissMs);
        }

        [Fact]
        public void It_is_tall_enough_to_aim_at_and_sits_close_to_the_flyout()
        {
            _ = TesseraFlyoutDismissSpec.HeightDip.Should().BeGreaterThanOrEqualTo(24);
            _ = TesseraFlyoutDismissSpec.GapDip.Should().BeInRange(4, 10);
        }

        [Fact]
        public void It_acts_on_release_so_sliding_off_cancels()
        {
            _ = TesseraFlyoutDismissSpec.ActsOnRelease.Should().BeTrue();
        }

        [Fact]
        public void The_pill_reads_on_any_wallpaper()
        {
            // It sits outside the card, on the desktop: a dark translucent pill under white text, without reading the
            // backdrop.
            _ = TesseraFlyoutDismissSpec.PillAlpha.Should().BeInRange(0x80, 0xC0);
            _ = TesseraFlyoutDismissSpec.PillHoverAlpha.Should().BeGreaterThan(TesseraFlyoutDismissSpec.PillAlpha);
            _ = TesseraFlyoutDismissSpec.PillPressedAlpha.Should().BeGreaterThan(TesseraFlyoutDismissSpec.PillHoverAlpha);
        }

        [Fact]
        public void The_cluster_is_the_union_of_its_panels()
        {
            // Modern Flyouts, stacked: volume above media, the volume panel offset left during its slide.
            TesseraFlyoutDismissSpec.PanelBounds[] panels =
            [
                new(2090, 526, 400, 65),
                new(2140, 606, 400, 238),
            ];

            _ = TesseraFlyoutDismissSpec.ResolveClusterBounds(panels).Should().Be(new TesseraFlyoutDismissSpec.PanelBounds(2090, 526, 450, 318));
            _ = TesseraFlyoutDismissSpec.ResolveClusterBounds([]).Should().BeNull();
        }

        [Fact]
        public void It_sits_under_the_clusters_bottom_right_edge()
        {
            TesseraFlyoutDismissSpec.PanelBounds cluster = new(2140, 526, 400, 318);

            (int x, int y) = TesseraFlyoutDismissSpec.ResolvePlacement(cluster, linkWidthPx: 80, linkHeightPx: 24, gapPx: 6, workAreaTopPx: 0, workAreaBottomPx: 1400);

            _ = x.Should().Be(2540 - 80);
            _ = y.Should().Be(526 + 318 + 6);
        }

        [Fact]
        public void Near_the_bottom_of_the_screen_it_goes_above_instead()
        {
            // Bottom-right placement, the usual spot for a volume pop-up: there is no room below.
            TesseraFlyoutDismissSpec.PanelBounds cluster = new(2140, 1100, 400, 318);

            (int x, int y) = TesseraFlyoutDismissSpec.ResolvePlacement(cluster, linkWidthPx: 80, linkHeightPx: 24, gapPx: 6, workAreaTopPx: 0, workAreaBottomPx: 1440);

            _ = x.Should().Be(2540 - 80);
            _ = y.Should().Be(1100 - 6 - 24);
        }
    }
}
