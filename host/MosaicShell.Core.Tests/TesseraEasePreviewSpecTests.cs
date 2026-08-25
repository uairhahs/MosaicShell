using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraEasePreviewSpecTests
{
    [Fact]
    public void Preview_duration_matches_live_phase()
    {
        TesseraEasePreviewSpec.MustReplayWhenEaseOrStepsChange.Should().BeTrue();
        TesseraEasePreviewSpec.ResolveDurationMs(20)
            .Should().Be(TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(20));
    }

    [Fact]
    public void Preview_track_leaves_room_for_overshoot()
    {
        TesseraEasePreviewSpec.TravelDip.Should().BeGreaterThan(80);
        TesseraEasePreviewSpec.OvershootPadDip.Should().BeGreaterThan(0);
        TesseraEasePreviewSpec.TrackWidthDip.Should().BeGreaterThan(TesseraEasePreviewSpec.TravelDip);
    }

    [Fact]
    public void OutBounce_and_linear_differ_at_mid_preview_sample()
    {
        var bounce = TesseraFlyoutAnimationPolicy.SampleEaseContinuous(
            0.5, TesseraFlyoutAnimationPolicy.EaseOutBounce);
        var linear = TesseraFlyoutAnimationPolicy.SampleEaseContinuous(
            0.5, TesseraFlyoutAnimationPolicy.EaseLinear);
        Math.Abs(bounce - linear).Should().BeGreaterThan(0.15);
    }
}
