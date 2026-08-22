using FluentAssertions;
using MosaicShell.Core.Scale;

namespace MosaicShell.Core.Tests;

public class ScaleContractTests
{
    [Fact]
    public void UiScale_is_DpiScale_times_UserScale()
    {
        var c = new ScaleContract();
        c.SetDpiScale(1.5);
        c.SetUserScale(1.0);
        c.UiScale.Should().Be(1.5);

        c.SetUserScale(0.8);
        c.UiScale.Should().Be(1.2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetDpiScale_rejects_non_positive(double bad)
    {
        var c = new ScaleContract();
        var act = () => c.SetDpiScale(bad);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0.74)]
    [InlineData(2.01)]
    public void SetUserScale_rejects_out_of_range(double bad)
    {
        var c = new ScaleContract();
        var act = () => c.SetUserScale(bad);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ResetUserScale_returns_to_one()
    {
        var c = new ScaleContract();
        c.SetUserScale(1.25);
        c.ResetUserScale();
        c.UserScale.Should().Be(1.0);
    }

    [Fact]
    public void Roundtrip_settings_preserves_values()
    {
        var c = new ScaleContract();
        c.SetDpiScale(1.25);
        c.SetUserScale(1.1);
        var again = ScaleContract.FromSettings(c.ToSettings());
        again.DpiScale.Should().Be(1.25);
        again.UserScale.Should().Be(1.1);
        again.UiScale.Should().Be(Math.Round(1.25 * 1.1, 4));
    }
}
