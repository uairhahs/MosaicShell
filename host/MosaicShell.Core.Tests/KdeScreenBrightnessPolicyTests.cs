using FluentAssertions;
using MosaicShell.Core.Services.Kde;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Brightness through PowerDevil's org.kde.ScreenBrightness, following the brightness keys: the value shown is the
    /// built-in panel's, and a change moves every display by the same step (AdjustBrightnessRatio), as the keys do.
    /// </summary>
    public sealed class KdeScreenBrightnessPolicyTests
    {
        private static readonly KdeDisplay Odyssey = new("display0", IsInternal: false, Brightness: 8500, MaxBrightness: 10000);
        private static readonly KdeDisplay Panel = new("display1", IsInternal: true, Brightness: 3500, MaxBrightness: 10000);

        [Fact]
        public void The_built_in_panel_is_the_reference_wherever_it_is_listed()
        {
            _ = KdeScreenBrightnessPolicy.Reference([Odyssey, Panel]).Should().Be(Panel);
            _ = KdeScreenBrightnessPolicy.Reference([Panel, Odyssey]).Should().Be(Panel);
        }

        [Fact]
        public void Without_a_built_in_panel_the_first_display_is_the_reference()
        {
            KdeDisplay second = Odyssey with { DbusName = "display2" };

            _ = KdeScreenBrightnessPolicy.Reference([Odyssey, second]).Should().Be(Odyssey);
            _ = KdeScreenBrightnessPolicy.Reference([]).Should().BeNull();
        }

        [Theory]
        [InlineData(3500, 10000, 0.35)]
        [InlineData(0, 10000, 0.0)]
        [InlineData(12000, 10000, 1.0)]
        [InlineData(50, 0, 0.0)]
        public void The_ratio_is_brightness_over_its_maximum(int brightness, int max, double expected)
        {
            _ = KdeScreenBrightnessPolicy.Ratio(Panel with { Brightness = brightness, MaxBrightness = max }).Should().Be(expected);
        }

        [Theory]
        [InlineData(0.35, 0.5, 0.15)]
        [InlineData(0.35, 0.1, -0.25)]
        [InlineData(0.35, 1.4, 0.65)]
        [InlineData(0.35, -1, -0.35)]
        public void A_target_becomes_the_step_every_display_moves_by(double current, double target, double expected)
        {
            _ = KdeScreenBrightnessPolicy.DeltaFor(current, target).Should().BeApproximately(expected, 1e-9);
        }

        [Fact]
        public void Each_display_has_its_own_object_path()
        {
            _ = KdeScreenBrightnessPolicy.DisplayPath("display1").Should().Be("/org/kde/ScreenBrightness/display1");
        }
    }
}
