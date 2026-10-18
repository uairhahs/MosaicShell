using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraEasePreviewSpecTests
    {
        [Fact]
        public void Preview_duration_matches_live_phase()
        {
            _ = TesseraEasePreviewSpec.ResolveDurationMs(20)
                .Should().Be(TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(20));
        }

        [Fact]
        public void Preview_track_leaves_room_for_overshoot()
        {
            _ = TesseraEasePreviewSpec.TravelDip.Should().BeGreaterThan(80);
            _ = TesseraEasePreviewSpec.OvershootPadDip.Should().BeGreaterThan(0);
            _ = TesseraEasePreviewSpec.TrackWidthDip.Should().BeGreaterThan(TesseraEasePreviewSpec.TravelDip);
        }
    }
}
