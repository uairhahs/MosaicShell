using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutTweenEngineTests
{
    [Fact]
    public void ResolveEasingFunction_maps_in_and_out_to_different_delegates()
    {
        var easeIn = TesseraFlyoutTweenEngine.ResolveEasingFunction(TesseraFlyoutAnimationPolicy.EaseInQuart);
        var easeOut = TesseraFlyoutTweenEngine.ResolveEasingFunction(TesseraFlyoutAnimationPolicy.EaseOutQuart);
        easeIn(10, 0, 100, 20).Should().BeApproximately(6.25, 0.01);
        easeOut(10, 0, 100, 20).Should().BeApproximately(93.75, 0.01);
    }

    [Theory]
    [InlineData(TesseraFlyoutAnimationPolicy.EaseInQuad, 25)]
    [InlineData(TesseraFlyoutAnimationPolicy.EaseInCubic, 12.5)]
    [InlineData(TesseraFlyoutAnimationPolicy.EaseInQuart, 6.25)]
    public void In_families_are_distinguishable_at_half_steps(string ease, double expected)
    {
        var tween = new TesseraFlyoutStepTween(20, 0, 100, ease);
        for (var i = 0; i < 10; i++)
            tween.Advance(1);
        tween.Value.Should().BeApproximately(expected, 0.05);
    }
}
