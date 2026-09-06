using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutTweenEngineTests
    {
        [Fact]
        public void ResolveEasingFunction_maps_in_and_out_to_different_delegates()
        {
            Func<double, double, double, double, double> easeIn = TesseraFlyoutTweenEngine.ResolveEasingFunction(TesseraFlyoutAnimationPolicy.EaseInQuart);
            Func<double, double, double, double, double> easeOut = TesseraFlyoutTweenEngine.ResolveEasingFunction(TesseraFlyoutAnimationPolicy.EaseOutQuart);
            _ = easeIn(10, 0, 100, 20).Should().BeApproximately(6.25, 0.01);
            _ = easeOut(10, 0, 100, 20).Should().BeApproximately(93.75, 0.01);
        }

        [Theory]
        [InlineData(TesseraFlyoutAnimationPolicy.EaseInQuad, 25)]
        [InlineData(TesseraFlyoutAnimationPolicy.EaseInCubic, 12.5)]
        [InlineData(TesseraFlyoutAnimationPolicy.EaseInQuart, 6.25)]
        public void In_families_are_distinguishable_at_half_steps(string ease, double expected)
        {
            TesseraFlyoutStepTween tween = new(20, 0, 100, ease);
            for (int i = 0; i < 10; i++)
            {
                _ = tween.Advance(1);
            }

            _ = tween.Value.Should().BeApproximately(expected, 0.05);
        }
    }
}
