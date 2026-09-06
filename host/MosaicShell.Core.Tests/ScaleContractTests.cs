using FluentAssertions;
using MosaicShell.Core.Scale;

namespace MosaicShell.Core.Tests
{
    public class ScaleContractTests
    {
        [Fact]
        public void UiScale_is_DpiScale_times_UserScale()
        {
            ScaleContract c = new();
            c.SetDpiScale(1.5);
            c.SetUserScale(1.0);
            _ = c.UiScale.Should().Be(1.5);

            c.SetUserScale(0.8);
            _ = c.UiScale.Should().Be(1.2);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void SetDpiScale_rejects_non_positive(double bad)
        {
            ScaleContract c = new();
            Action act = () => c.SetDpiScale(bad);
            _ = act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(0.74)]
        [InlineData(2.01)]
        public void SetUserScale_rejects_out_of_range(double bad)
        {
            ScaleContract c = new();
            Action act = () => c.SetUserScale(bad);
            _ = act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ResetUserScale_returns_to_one()
        {
            ScaleContract c = new();
            c.SetUserScale(1.25);
            c.ResetUserScale();
            _ = c.UserScale.Should().Be(1.0);
        }

        [Fact]
        public void Roundtrip_settings_preserves_values()
        {
            ScaleContract c = new();
            c.SetDpiScale(1.25);
            c.SetUserScale(1.1);
            ScaleContract again = ScaleContract.FromSettings(c.ToSettings());
            _ = again.DpiScale.Should().Be(1.25);
            _ = again.UserScale.Should().Be(1.1);
            _ = again.UiScale.Should().Be(Math.Round(1.25 * 1.1, 4));
        }
    }
}
